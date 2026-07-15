using FastJobs.Persistence;
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
    internal Worker(int workerId, string workerName, IServiceScopeFactory serviceScope, CancellationToken shutdownToken)
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

    internal void SetDBWorkerID(long id)
    {
        _dbWorkerId = (int)id;
    }


    internal async Task Run()
    {

        var workerRecord =  await WorkerObservability();

        try
        {
            while (!_shutdownToken.IsCancellationRequested)
            {
                Tuple<Queue, SessionDatabaseLock>? JobQueueDetails = null;

  
                if (await _QueueProcessor.AllQueuesEmpty(_shutdownToken))
                {
                    await RestWorker(workerRecord);
                    await Task.Delay(300, _shutdownToken); 
                    continue;
                }

                // WHen work is likely,  lock and re-check
                await _semaphore.WaitAsync(_shutdownToken);
                try
                {
                     // Must re-check — another worker may have dequeued
                    // the last job between our outer check and acquiring the lock
                    if (await _QueueProcessor.AllQueuesEmpty(_shutdownToken))
                        continue;

                    JobQueueDetails = await _QueueProcessor.Dequeue(_shutdownToken);
                }
                finally
                {
                    _semaphore.Release();
                }

                using (var Scope = new ScopeManager(serviceScopeFactory))
                {
                    if (JobQueueDetails == null)
                        continue;

                    await WakeWorker(workerRecord);

                    //RESOLVE JOB DETAILS 
                    IJobRepository JobRepo = Scope.Resolve<IJobRepository>();
                    Job job = await JobRepo.GetByIdAsync(JobQueueDetails.Item1.JobId);

                    // If the job has expired by the time we got it, skip processing And Change the Jobs State 
                    if (job.ExpiresAt.HasValue && DateTime.UtcNow >= job.ExpiresAt.Value)
                    {
                        StateHelpers StateHelper = new StateHelpers(JobRepo, Scope.Resolve<IStateHistoryRepository>());
                        await StateHelper.UpdateJobStateAsync(job.Id ?? 0, QueueStateTypes.Expired, $"Job #{job.Id} of Type {job.MethodDeclaringTypeName} is Expired", "", _shutdownToken);

                        continue;
                    }
                        

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
                    catch (TerminateJobException ex)
                    {
                        await _QueueProcessor.FailJobAsync(job, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        //if Job has Exceeded its retry Limit Fail the job
                        if( !(job.RetryCount >= 3))
                        {
                            _logger.LogError(ex, "Job #{JobID} of type {DeclaringTypeName} Beginning Retry {RetryCOunt}  ", job.Id, job.MethodDeclaringTypeName, job.RetryCount + 1);
                            await _QueueProcessor.RequeueJobAsync(JobQueueDetails.Item1, JobQueueDetails.Item2, Scope, ex.Message);
                        }
                        else
                        {
                            await _QueueProcessor.FailJobAsync(job, ex.Message);
                        }

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

                    try
                    {
                        if(jobSucceeded)
                        {
                           await _QueueProcessor.CompleteJobAsync(JobQueueDetails.Item1, JobQueueDetails.Item2);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't requeue — job work is done, only cleanup failed
                        _logger.LogError(ex, "Job #{JobID} of type {DeclaringTypeName} Failed State Update After Completion ",
                            job.Id, 
                            job.MethodDeclaringTypeName                             );
                    }


                    //TODO: Resolve The Order for these Two Functions 
                    //currently this is fine because recurring jobs after actions only run on final completion
                    await RunAfterAction(job, Scope, jobSucceeded);
                    await Reschedule(job, Scope, jobSucceeded);
                    
                    jobContext.SetJob(null);

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

            workerRecord.isCrashed  = true;
            workerRecord.isSleeping = false;
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

    internal async Task Reschedule(Job job, ScopeManager Scope, bool JobSuceeded)
    {
        //Reschedule Recurring Job Types
        if (job.JobType == JobTypes.Recurring )
         {  
            bool RetriesExhausted = job.RetryCount >= job.MaxRetries;
            if(RetriesExhausted || JobSuceeded)
            {
                await RescheduleRecurringJobAsync(job.Id ?? 0, Scope);
            }
         }
    }
    internal async Task RunAfterAction(Job job, ScopeManager Scope, bool JobSuceeded)
    {
        //Execute After Actions
        var JobAfterActionID = job.AfterActionId ?? 0;
        if(job.AfterActionId != null && JobAfterActionID != 0)
        {
            if(job.JobType != JobTypes.Recurring && JobSuceeded)
            {
                await ExecuteAfterActionChainAsync(JobAfterActionID, Scope, _shutdownToken);
            }
            else
            {
                // Run Recurring Jobs After action on final completion if Job has not expired 
                if (job.ExpiresAt.HasValue && DateTime.UtcNow >= job.ExpiresAt.Value)
                {
                   await ExecuteAfterActionChainAsync(JobAfterActionID, Scope, _shutdownToken);                                 
                } 
            }
        }

    }

    internal async Task RestWorker(FSTJBS_Worker WorkerRecord)
    {
        using( var scope = new ScopeManager(serviceScopeFactory))
        {
            IWorkerRepository workerRepo = scope.Resolve<IWorkerRepository>();
            // Mark sleeping only if we weren't already
            if (!WorkerRecord.isSleeping)
            {
                WorkerRecord.isSleeping = true;
                await workerRepo.UpdateAsync(WorkerRecord, _shutdownToken);
            }
        }
        
    }

    internal async Task WakeWorker(FSTJBS_Worker WorkerRecord)
    {
        using( var scope = new ScopeManager(serviceScopeFactory))
        {
            
            IWorkerRepository workerRepo = scope.Resolve<IWorkerRepository>();

            // Wake up There is  Work to do 
            if (WorkerRecord.isSleeping)
            {
                WorkerRecord.isSleeping = false;
                await workerRepo.UpdateAsync(WorkerRecord, _shutdownToken);
            }
        }
        
    }

    /// <summary>
    /// Executes The After Actions Chain For The Just Completed Job 
    /// </summary>
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
        var ActionContext = scope.Resolve<IAfterActionContext>() as AfterActionContext;
        ActionContext.SetAction(actionModel);

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


    private async Task RescheduleRecurringJobAsync(long RecurringjobId, ScopeManager scope)
    {
        var recurringJobRepository = scope.Resolve<IRecurringJobRepository>();
        var scheduledJobRepository = scope.Resolve<IScheduledJobRepository>();
        var processingServer = scope.Resolve<ProcessingServer>();
        var jobRepository = scope.Resolve<IJobRepository>();
        var stateHelper = new StateHelpers(jobRepository, scope.Resolve<IStateHistoryRepository>());

        var recurringJob = await recurringJobRepository.GetByIdAsync(RecurringjobId);
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
            JobId = RecurringjobId,
            ScheduledTo = nextRun.Value
        };
        var scheduledId = await scheduledJobRepository.InsertAsync(scheduledJob);

        // Update recurring job
        recurringJob.NextScheduledID = scheduledId;
        recurringJob.NextScheduledTime = nextRun.Value;
        await recurringJobRepository.UpdateByIdAsync(recurringJob);

        await stateHelper.UpdateJobStateAsync(RecurringjobId, QueueStateTypes.Scheduled, $"Recurring job #{recurringJob.id} rescheduled for {nextRun:O}", "", CancellationToken.None);

        processingServer.NotifyScheduledJobAdded();
    }
}