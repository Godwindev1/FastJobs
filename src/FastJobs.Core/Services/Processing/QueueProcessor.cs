using FastJobs.Persistence;
using Microsoft.Extensions.Logging;

namespace FastJobs;
public class QueueNames
{
    public const string Default = "Default";
    public const string Critical = "Critical";
    public const string LowPriority = "LowPriority";
}

/// <summary>
/// QueueProcessor is Responsible for Rettrieving A Job From the DB queue And Locking The DB entry And Returning Lock and Entry 
/// </summary>
internal class QueueProcessor
{
    private readonly IQueueRepository _queueRepo;
    private readonly IJobRepository _JobRepository;
    private readonly LockProvider _lockProvider;
    private readonly StateHelpers _stateHelpers;

    public QueueProcessor(IQueueRepository queueRepository, IJobRepository jobRepository, IStateHistoryRepository stateRepo, LockProvider lockProvider)
    {
        _queueRepo = queueRepository;
        _JobRepository = jobRepository;
        _lockProvider = lockProvider;
        _stateHelpers = new StateHelpers(jobRepository, stateRepo);
    }

    private Task<SessionDatabaseLock?> LockQueueItem(string QueueEntryID, string JobID, CancellationToken cancellationToken)
    {
      return  _lockProvider.AcquireLock($"FastJobs.{QueueEntryID}.{JobID}", TimeSpan.FromMinutes(5), cancellationToken);   
    }


    public async Task<bool> IsQueueEmpty(string QueueName, CancellationToken cancellationToken)
    {
        var result = await _queueRepo.Dequeue(QueueName, cancellationToken);
        if(result == null)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> AllQueuesEmpty(CancellationToken cancellationToken)
    {
       bool exists = await _queueRepo.ExistsAny(cancellationToken);
       return !exists;
    }

    public async Task<Tuple<Queue, SessionDatabaseLock>?> Dequeue(CancellationToken cancellationToken)
    {
        Queue<string> QueueNamesToCheck = new Queue<string>();
        QueueNamesToCheck.Enqueue(QueueNames.Critical);
        QueueNamesToCheck.Enqueue(QueueNames.Default);  
        QueueNamesToCheck.Enqueue(QueueNames.LowPriority);

        //Dequeue from each queue in order of priority until we find a job to process or exhaust all queues
        while (QueueNamesToCheck.Count > 0)
        {
            var queueName = QueueNamesToCheck.Dequeue();
            var result = await DequeueAsync(queueName, cancellationToken);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public async Task<Tuple<Queue, SessionDatabaseLock>?> DequeueAsync(string QueueName, CancellationToken cancellationToken)
    {
        Queue? Entry  = await _queueRepo.Dequeue(QueueName, cancellationToken);

        if(Entry != null)
        {
            SessionDatabaseLock CurrentWorkerHeldLock =  await LockQueueItem(Entry.Id.ToString(), Entry.JobId.ToString(), cancellationToken) ; 
            var Job = await _JobRepository.GetByIdAsync(Entry.JobId, cancellationToken);
            
            // Update job state with atomic state history creation and rollback support
            await _stateHelpers.UpdateJobStateAsync(
                Job.Id ?? 0,
                QueueStateTypes.Dequeued,
                $"Job #{Job.Id} of Type {Job.MethodDeclaringTypeName} Has Been  Dequeued",
                data: "",
                cancellationToken);

            //Set Dequeed Item Visibility Hide To true;
            Entry.isDequeued = true;
            await _queueRepo.Update(Entry, cancellationToken);

            return new Tuple<Queue, SessionDatabaseLock>( Entry, CurrentWorkerHeldLock );
        
        }

        return null;
    }


    public async Task CompleteJobAsync(Queue JobQueueEntry, SessionDatabaseLock QueueLock)
    {
        //NOTE: Intentionally no CancellationToken — finalisation operation must complete to avoid orphaned locks
        try
        {
            
            // Update job state with atomic state history creation and rollback support
            await _stateHelpers.UpdateJobStateAsync(
                JobQueueEntry.JobId,
                QueueStateTypes.Completed,
                $"Job #{JobQueueEntry.JobId}  Has Been  Completed",
                data: "");

            await _queueRepo.RemoveAsync(JobQueueEntry.Id);
        }
        finally
        {
            await QueueLock.ReleaseLockAsync();
            QueueLock.Dispose();
        }
    }

    internal async Task FailJobAsync(Job job, string ExceptionMessage)
    {
        //NOTE: Intentionally no CancellationToken — compensating operation must complete
        // Update job state with atomic state history creation and rollback support
        await _stateHelpers.UpdateJobStateAsync(
            job.Id ?? 0,
            QueueStateTypes.Failed,
            $"Job #{job.Id} of Type {job.MethodDeclaringTypeName} Has Failed As Many As {job.MaxRetries} Times Or Was Intentionally Terminated",
            data: ExceptionMessage);
    }

    public async Task RequeueJobAsync(Queue JobQueueEntry, SessionDatabaseLock QueueLock, ScopeManager Scope,  string ExceptionMessage = "")
    {  
        //TODO: REqueue Would Now Take in A TimeSpan And Use Scheduling to Schedule The JOB with Exponential Backopff Properly 
        //NOTE: Intentionally no CancellationToken — compensating operation must complete to maintain job state consistency
        try {
            var Job = await _JobRepository.GetByIdAsync(JobQueueEntry.JobId);
            
            if (Job == null)
            {
                await QueueLock.ReleaseLockAsync();
                QueueLock.Dispose();
                return;
            }

            if(Job.RetryCount >= Job.MaxRetries)
            {
                await FailJobAsync(Job, ExceptionMessage);
                return;
            }

            //Remove it Frome The Queue
            await _queueRepo.RemoveAsync(JobQueueEntry.Id);   

            FastJobsOptions Options = Scope.Resolve<FastJobsOptions>();
            var StateRepo = Scope.Resolve<IStateHistoryRepository>();
            var ScehduledJobRepository = Scope.Resolve<IScheduledJobRepository>();

            var TimeSpan = Math.Min(Options.MaxJobRetryDelay.TotalSeconds, 
                                Math.Pow(2, Job.RetryCount) * Options.JobRetryDelayBase.TotalSeconds)
                                + Random.Shared.NextDouble() * Options.Jitter.TotalSeconds;
            
            Scope.Resolve<ILogger<QueueProcessor>>().LogInformation("TImespan For Backoff {Time}", TimeSpan);

            await JobRetryScheduler.RescheduleAsync(
                job: Job,
                scheduledTime: DateTime.UtcNow.AddSeconds(TimeSpan),
                jobRepository: _JobRepository,
                stateHistoryRepository: StateRepo,
                scheduledJobRepository: ScehduledJobRepository,
                processingServer: Scope.Resolve<ProcessingServer>()
            ); 

             await QueueLock.ReleaseLockAsync();
             QueueLock.Dispose();
            

            
        }
        finally 
        {
            await QueueLock.ReleaseLockAsync();
            QueueLock.Dispose();
        }

       
    }

}
