using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;
//PENDING MODIFICATIONS
public class Worker
{
    private readonly int _workerId;
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly CancellationToken _shutdownToken;
    private readonly IServiceScopeFactory serviceScopeFactory; 

    private readonly QueueProcessor _QueueProcessor;
    public Worker(int workerId, IServiceScopeFactory serviceScope, CancellationToken shutdownToken)
    {
        using var Scopemanager = new ScopeManager(serviceScope);
        
        _QueueProcessor = new QueueProcessor(
            Scopemanager.Resolve<IQueueRepository>(),
            Scopemanager.Resolve<IJobRepository>(),
            Scopemanager.Resolve<IStateHistoryRepository>(),
            Scopemanager.Resolve<LockProvider>()
        );

        _workerId = workerId;
        serviceScopeFactory = serviceScope;
        _shutdownToken = shutdownToken;
    }

    public async Task Run()
    {
        while (!_shutdownToken.IsCancellationRequested)
        {
            Tuple<Queue, SessionDatabaseLock>? JobDetails = null;
            if (await _QueueProcessor.AllQueuesEmpty(_shutdownToken))
            {
                await Task.Delay(200, _shutdownToken);
                continue;
            }

            // WHen work is likely,  lock and re-check
            await _semaphore.WaitAsync(_shutdownToken);
            try
            {
                // Must re-check — another worker may have dequeued
                // the last job between our outer check and acquiring the lock
                if (await _QueueProcessor.AllQueuesEmpty(_shutdownToken))
                {
                    continue;
                }

                JobDetails = await _QueueProcessor.Dequeue(_shutdownToken);
            }
            finally
            {
                _semaphore.Release();
            }

            using ( var Scope = new ScopeManager(serviceScopeFactory) )
            {
                
                if (JobDetails == null)
                {
                    continue;
                }

                IJobRepository JobRepo = Scope.Resolve<IJobRepository>();
                Job job = await JobRepo.GetByIdAsync(JobDetails.Item1.JobId);

                var ResolvedJob = JobResolver.ResolveJob(job, Scope);
                
                if (ResolvedJob == null)
                {
                    await Task.Delay(500, _shutdownToken);
                    continue;
                }

                var jobCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);
                bool jobSucceeded = false;

                // Set the job context so jobs like ExpressionFireAndForgetJob can access the job metadata
                // Since This is a ScopedService Resolved Within A Scope Inside the Worker Its Thread safe And Does not Interfare Other Workers Setting Contexts for use Asewell
                var jobContext = Scope.Resolve<IJobContext>() as JobContext;
                jobContext.SetJob( job );

                // Check if this is a recurring job and increment ExecutingInstances
                IRecurringJobRepository? recurringRepo = null;
                RecurringJob? recurringJob = null;
                if (job.JobType == JobTypes.Recurring)
                {
                    recurringRepo = Scope.Resolve<IRecurringJobRepository>();
                    recurringJob = await recurringRepo.GetByJob(job, jobCts.Token);
                    if (recurringJob != null)
                    {
                        recurringJob.ExecutingInstances++;
                        await recurringRepo.UpdateByIdAsync(recurringJob, jobCts.Token);
                    }
                }

                try
                {
                    StateHelpers StateHelper = new StateHelpers(JobRepo, Scope.Resolve<IStateHistoryRepository>());
                    await StateHelper.UpdateJobStateAsync(job.Id, QueueStateTypes.Processing, "Job is being processed", "", jobCts.Token);

                    await ResolvedJob.ExecuteAsync(jobCts.Token);
                    jobSucceeded = true; 
                }
                catch (OperationCanceledException) when (jobCts.IsCancellationRequested)
                {
                    // expected during shutdown
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await _QueueProcessor.RequeueJobAsync(JobDetails.Item1, JobDetails.Item2, ex.Message);
                }
                finally
                {
                    // Update recurring job execution counters
                    if (recurringRepo != null && recurringJob != null)
                    {
                        recurringJob.ExecutingInstances--;
                        if (jobSucceeded)
                        {
                            recurringJob.ExecutedInstances++;
                        }
                        await recurringRepo.UpdateByIdAsync(recurringJob, jobCts.Token);
                    }
                }

               
                if (jobSucceeded)
                {
                    try
                    {
                        await _QueueProcessor.CompleteJobAsync(JobDetails.Item1, JobDetails.Item2);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't requeue — job work is done, only cleanup failed
                        Console.WriteLine($"CompleteJob failed: {ex.Message}");
                    }

                    // Reschedule recurring jobs
                    if (job.JobType == JobTypes.Recurring)
                    {
                        await RescheduleRecurringJobAsync(job.Id, Scope);
                    }

                    jobContext.SetJob( null );
                }

            } 
        }
    }

    private async Task RescheduleRecurringJobAsync(long jobId, ScopeManager scope)
    {
        var recurringJobRepository = scope.Resolve<IRecurringJobRepository>();
        var scheduledJobRepository = scope.Resolve<IScheduledJobRepository>();
        var processingServer = scope.Resolve<ProcessingServer>();
        var jobRepository = scope.Resolve<IJobRepository>();
        var stateHelper = new StateHelpers(jobRepository, scope.Resolve<IStateHistoryRepository>());

        var recurringJob = await recurringJobRepository.GetByIdAsync(jobId);
        if (recurringJob == null) return;

        // Fetch the job to check expiry from Jobs table
        var job = await jobRepository.GetByIdAsync(recurringJob.JobId);
        if (job == null) return;

        // If the job has expired, do not reschedule
        if (job.ExpiresAt.HasValue && DateTime.UtcNow >= job.ExpiresAt.Value)
            return; 

        // Check concurrency if not concurrent
        if (!recurringJob.IsConcurrent && recurringJob.ExecutingInstances > 0)
            return; // Skip this cycle

        // Compute next run
        var nextRun = recurringJob.ComputeNextRun(DateTime.UtcNow);
        if (nextRun == null) return; // No more occurrences

        // Insert next scheduled job
        var scheduledJob = new ScheduledJobInfo
        {
            JobId = jobId,
            ScheduledTo = nextRun.Value
        };
        var scheduledId = await scheduledJobRepository.InsertAsync(scheduledJob);

        // Update recurring job
        recurringJob.NextScheduledID = scheduledId;
        recurringJob.NextScheduledTime = nextRun.Value;
        await recurringJobRepository.UpdateByIdAsync(recurringJob);

        await stateHelper.UpdateJobStateAsync(jobId, QueueStateTypes.Scheduled, $"Recurring job rescheduled for {nextRun:O}", "", CancellationToken.None);

        processingServer.NotifyScheduledJobAdded();
    }
}