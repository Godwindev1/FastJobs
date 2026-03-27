
using System.Threading.Tasks;
using FastJobs.SqlServer;

namespace FastJobs;

/// <summary>
/// QueueProcessor is Responsible for Rettrieving A Job From the DB queue And Locking The DB entry And Returning Lock and Entry 
/// </summary>
internal class QueueProcessor
{
    private readonly IQueueRepository _queueRepo;
    private readonly IJobRepository _JobRepository;
    private readonly LockProvider _locprovider;
    private readonly IStateHistoryRepository stateHistoryRepository;
    private readonly StateHelpers _stateHelpers;

    public QueueProcessor(IQueueRepository queueRepository, IJobRepository jobRepository, IStateHistoryRepository stateRepo, LockProvider lockProvider)
    {
        _queueRepo = queueRepository;
        _JobRepository = jobRepository;
        _locprovider = lockProvider;
        stateHistoryRepository = stateRepo;
        _stateHelpers = new StateHelpers(jobRepository, stateRepo);
    }

    private Task<SessionDatabaseLock?> LockQueueItem(string QueueEntryID, string JobID, CancellationToken cancellationToken)
    {
      return  _locprovider.AcquireLock($"FastJobs.{QueueEntryID}.{JobID}", TimeSpan.FromMinutes(5), cancellationToken);   
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
    public async Task<Tuple<Queue, SessionDatabaseLock>?> DequeueAsync(string QueueName, CancellationToken cancellationToken)
    {
        Queue? Entry  = await _queueRepo.Dequeue(QueueName, cancellationToken);

        if(Entry != null)
        {
            SessionDatabaseLock CurrentWorkerHeldLock =  await LockQueueItem(Entry.Id.ToString(), Entry.JobId.ToString(), cancellationToken) ; 
            var Job = await _JobRepository.GetByIdAsync(Entry.JobId, cancellationToken);
            
            // Update job state with atomic state history creation and rollback support
            await _stateHelpers.UpdateJobStateAsync(
                Job.Id,
                QueueStateTypes.Scheduled,
                "Schedule Job For Processing",
                data: "",
                cancellationToken);

            //Set Dequeed Item Visibility Hide To true;
            Entry.IsScheduled = true;
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
                "Job Has Been Completed",
                data: "Completed");

            await _queueRepo.RemoveAsync(JobQueueEntry.Id);
        }
        finally
        {
            await QueueLock.ReleaseLockAsync();
            QueueLock.Dispose();
        }
    }

    private async Task FailJobAsync(Job job, string ExceptionMessage)
    {
        //NOTE: Intentionally no CancellationToken — compensating operation must complete
        // Update job state with atomic state history creation and rollback support
        await _stateHelpers.UpdateJobStateAsync(
            job.Id,
            QueueStateTypes.Failed,
            $"Job Has Failed As Many As {job.MaxRetries} Times",
            data: ExceptionMessage);
    }

    public async Task RequeueJobAsync(Queue JobQueueEntry, SessionDatabaseLock QueueLock, string ExceptionMessage = "")
    {  
        //NOTE: Intentionally no CancellationToken — compensating operation must complete to maintain job state consistency
        try {
            var Job = await _JobRepository.GetByIdAsync(JobQueueEntry.JobId);
            if (Job == null)
            {
                await QueueLock.ReleaseLockAsync();
                QueueLock.Dispose();
                return;
            }

            if(Job.RetryCount > Job.MaxRetries)
            {
                await FailJobAsync(Job, ExceptionMessage);
                await _queueRepo.RemoveAsync(JobQueueEntry.Id);   
            }
            else
            {
                // Update job state with atomic state history creation and rollback support
                await _stateHelpers.UpdateJobStateAsync(
                    JobQueueEntry.JobId,
                    QueueStateTypes.Enqueued,
                    "Retrying",
                    data: ExceptionMessage);

                // Increment retry count separately
                Job.RetryCount += 1;
                await _JobRepository.UpdateByIdAsync(Job);

                //make Job Visible again
                JobQueueEntry.IsScheduled = false;
                await _queueRepo.Update(JobQueueEntry);
            }
            
        }
        finally {
        
            await QueueLock.ReleaseLockAsync();
            QueueLock.Dispose();
        }
       
    }

}
