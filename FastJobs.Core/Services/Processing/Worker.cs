using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FastJobs;
//PENDING MODIFICATIONS
public partial class Worker
{
    private readonly int _workerId;
    private int _dbWorkerId; // ID of this worker in the database for observability
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly CancellationToken _shutdownToken;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private CancellationTokenSource? _heartbeatCts; 

    private readonly QueueProcessor _QueueProcessor;
    
    private ILogger<Worker> _logger;

    private string _WorkerName;

    private FastJobsOptions _options;
    public Worker(int workerId, string workerName, IServiceScopeFactory serviceScope, CancellationToken shutdownToken)
    {
        using var Scopemanager = new ScopeManager(serviceScope);
        
        _QueueProcessor = new QueueProcessor(
            Scopemanager.Resolve<IQueueRepository>(),
            Scopemanager.Resolve<IJobRepository>(),
            Scopemanager.Resolve<IStateHistoryRepository>(),
            Scopemanager.Resolve<LockProvider>()
        );

        _workerId = workerId;
        _WorkerName = workerName;
        serviceScopeFactory = serviceScope;
        _shutdownToken = shutdownToken;

        _options = Scopemanager.Resolve<FastJobsOptions>();
        _logger = Scopemanager.Resolve<ILogger<Worker>>();
    }

    public void SetDBWorkerID(long id)
    {
        _dbWorkerId = (int)id;
    }


    public async Task Run()
    {

        var workerRecord =  await WorkerObservability();

        try
        {
            while (!_shutdownToken.IsCancellationRequested)
            {
                Tuple<Queue, SessionDatabaseLock>? JobDetails = null;

  
                if (await _QueueProcessor.AllQueuesEmpty(_shutdownToken))
                {
                    using( var scope = new ScopeManager(serviceScopeFactory))
                    {
                        IWorkerRepository workerRepo = scope.Resolve<IWorkerRepository>();

                        // Mark sleeping only if we weren't already
                        if (!workerRecord.isSleeping)
                        {
                            workerRecord.isSleeping = true;
                            await workerRepo.UpdateAsync(workerRecord, _shutdownToken);
                        }

                        await Task.Delay(200, _shutdownToken);
                        continue;
                    }
                }

                // WHen work is likely,  lock and re-check
                await _semaphore.WaitAsync(_shutdownToken);
                try
                {
                     // Must re-check — another worker may have dequeued
                    // the last job between our outer check and acquiring the lock
                    if (await _QueueProcessor.AllQueuesEmpty(_shutdownToken))
                        continue;

                    JobDetails = await _QueueProcessor.Dequeue(_shutdownToken);
                }
                finally
                {
                    _semaphore.Release();
                }

                using (var Scope = new ScopeManager(serviceScopeFactory))
                {
                    if (JobDetails == null)
                        continue;

                    IWorkerRepository workerRepo = Scope.Resolve<IWorkerRepository>();

                    // Wake up There is  Work to do 
                    if (workerRecord.isSleeping)
                    {
                        workerRecord.isSleeping = false;
                        await workerRepo.UpdateAsync(workerRecord, _shutdownToken);
                    }

                    IJobRepository JobRepo = Scope.Resolve<IJobRepository>();
                    Job job = await JobRepo.GetByIdAsync(JobDetails.Item1.JobId);

                    // If the job has expired by the time we got it, skip processing
                    if (job.ExpiresAt.HasValue && DateTime.UtcNow >= job.ExpiresAt.Value)
                        return;

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
                    jobContext.SetJob(job);

                    IRecurringJobRepository? recurringRepo = null;
                    RecurringJob? recurringJob = null;

                    // Check if this is a recurring job and increment ExecutingInstances
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
                        await StateHelper.UpdateJobStateAsync(job.Id ?? 0, QueueStateTypes.Processing, $"Job #{job.Id} of Type {job.MethodDeclaringTypeName} Has Begun Processing", "", jobCts.Token);

                        await ResolvedJob.ExecuteAsync(jobCts.Token);
                        jobSucceeded = true;
                    }
                    catch (OperationCanceledException) when (jobCts.IsCancellationRequested)
                    {
                        // expected during shutdown
                    }
                    catch (Exception ex)
                    {
                         _logger.LogError(ex, "Job #{JobID} of type {DeclaringTypeName} Beginning Retry {RetryCOunt}  ", job.Id, job.MethodDeclaringTypeName, job.RetryCount + 1);
                        await _QueueProcessor.RequeueJobAsync(JobDetails.Item1, JobDetails.Item2, ex.Message);
                    }
                    finally
                    {
                        // Update recurring job execution counters
                        if (recurringRepo != null && recurringJob != null)
                        {
                            recurringJob.ExecutingInstances--;
                            if (jobSucceeded)
                                recurringJob.ExecutedInstances++;

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
                            _logger.LogError(ex, "Job #{JobID} of type {DeclaringTypeName} Failed State Update After Completion ",
                                job.Id, 
                                job.MethodDeclaringTypeName                             );
                        }

                        if (job.JobType == JobTypes.Recurring)
                         {  
                             await RescheduleRecurringJobAsync(job.Id ?? 0, Scope);
                         }

                        //Execute After Actions
                        if(job.AfterActionId != null && job.AfterActionId != 0)
                        {
                            await ExecuteAfterActionChainAsync(job.AfterActionId ?? 0, Scope, _shutdownToken);
                        }

                        jobContext.SetJob(null);

                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown — not a crash
        }
        catch (Exception ex)
        {
            using var scope = new ScopeManager(serviceScopeFactory);
            IWorkerRepository workerRepo = scope.Resolve<IWorkerRepository>();

            // Unhandled exception with no fallback — mark as crashed
            _logger.LogError(ex, "Worker: {WorkerName} Has Crashed ", _WorkerName);

            workerRecord.isCrashed = true;
            workerRecord.isSleeping     = false;
            await workerRepo.UpdateAsync(workerRecord, CancellationToken.None);

            throw; // re-throw so the host knows this worker died
        }
        finally
        {
            using var scope = new ScopeManager(serviceScopeFactory);
            IWorkerRepository workerRepo = scope.Resolve<IWorkerRepository>();
            
            // Stop heartbeat regardless of how we exited
            await _heartbeatCts.CancelAsync();

            // Clean up worker record if shutdown was graceful
            if (!workerRecord.isCrashed)
                await workerRepo.DeleteAsync(_dbWorkerId, CancellationToken.None);
        }
    }


    private async Task ExecuteAfterActionChainAsync(
    long startingActionId,
    ScopeManager scope,
    CancellationToken cancellationToken)
{
    IAfterActionRepository afterActionRepo = scope.Resolve<IAfterActionRepository>();

    long? currentActionId = startingActionId;
    const int maxChainDepth = 100; // Guard against infinite loops
    int depth = 0;
    var visitedIds = new HashSet<long>(); // Guard against cycles

    while (currentActionId.HasValue)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Cycle detection
        if (!visitedIds.Add(currentActionId.Value))
        {
            throw new InvalidOperationException(
                $"Cycle detected in after-action chain at ActionId: {currentActionId.Value}");
        }

        // Depth guard
        if (++depth > maxChainDepth)
        {
            throw new InvalidOperationException(
                $"After-action chain exceeded maximum depth of {maxChainDepth}.");
        }

        // Fetch the action model
        var actionModel = await afterActionRepo.GetByIdAsync(currentActionId.Value);
        if (actionModel == null)
        {
            throw new InvalidOperationException(
                $"AfterAction with Id {currentActionId.Value} not found.");
        }

        // Resolve and execute
        var action = AfterActionsResolver.ResolveAction(actionModel, scope);
        await action.ExecuteAsync(cancellationToken);

        // Advance to next in chain
        currentActionId = actionModel.NextActionID;

         // Normalize 0 → null so the loop exits cleanly
        currentActionId = (actionModel.NextActionID == 0) 
            ? null 
            : actionModel.NextActionID;
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

        await stateHelper.UpdateJobStateAsync(jobId, QueueStateTypes.Scheduled, $"Recurring job #{recurringJob.id} rescheduled for {nextRun:O}", "", CancellationToken.None);

        processingServer.NotifyScheduledJobAdded();
    }
}