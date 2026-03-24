
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

    public QueueProcessor(IQueueRepository queueRepository, IJobRepository jobRepository, IStateHistoryRepository stateRepo, LockProvider lockProvider)
    {
        _queueRepo = queueRepository;
        _JobRepository = jobRepository;
        _locprovider = lockProvider;
        stateHistoryRepository = stateRepo;
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
            
            var StateId = await stateHistoryRepository.InsertAsync(new State { 
                CreatedAt = DateTime.UtcNow,
                StateName = QueueStateTypes.Scheduled,
                JobID = Job.Id,
                Reason = "Schedule Job For Processing",
                data = "",
            }, cancellationToken);


            Job.StateName = QueueStateTypes.Scheduled;
            Job.stateID = StateId;
            await _JobRepository.UpdateByIdAsync(Job, cancellationToken);

            
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
        //TODO: fix Possible Issues With Atomicity since StateID is Not Guranteed To Always Succeed 
        var stateID = await stateHistoryRepository.InsertAsync(new State { 
                CreatedAt = DateTime.Now,
                StateName = QueueStateTypes.Completed,
                JobID = JobQueueEntry.JobId,
                Reason = "Job Has Been Completed",
                data = "Completed",
            });

        var Job = await _JobRepository.GetByIdAsync(JobQueueEntry.JobId);
        Job.stateID = stateID;
        Job.StateName =  QueueStateTypes.Completed;
        await _JobRepository.UpdateByIdAsync(Job);

        await _queueRepo.RemoveAsync(JobQueueEntry.Id);
        await QueueLock.ReleaseLockAsync();
    }

    private async Task FailJobAsync(Job job, string ExceptionMessage)
    {
        //NOTE: Intentionally no CancellationToken — compensating operation must complete
        //TODO: fix Possible Issues With Atomicity since StateID is Not Guranteed To Always Succeed 
        var stateID = await stateHistoryRepository.InsertAsync(new State { 
                CreatedAt = DateTime.UtcNow,
                StateName = QueueStateTypes.Failed,
                JobID = job.Id,
                Reason = $"Job Has Failed As Many As {job.MaxRetries} Times",
                data = ExceptionMessage,
        });

        job.stateID = stateID;
        job.StateName =  QueueStateTypes.Failed;

        await _JobRepository.UpdateByIdAsync(job);       
    }

    public async Task RequeueJobAsync(Queue JobQueueEntry, SessionDatabaseLock QueueLock, string ExceptionMessage = "")
    {  
        //NOTE: Intentionally no CancellationToken — compensating operation must complete to maintain job state consistency
        try {
            var Job = await _JobRepository.GetByIdAsync(JobQueueEntry.JobId);
            if (Job == null)
            {
                await QueueLock.ReleaseLockAsync();
                return;
            }

            if(Job.RetryCount > Job.MaxRetries)
            {
                await FailJobAsync(Job, ExceptionMessage);
                await _queueRepo.RemoveAsync(JobQueueEntry.Id);   
            }
            else
            {
                //TODO: fix Possible Issues With Atomicity since StateID is Not Guranteed To Always Succeed 
                var stateID = await stateHistoryRepository.InsertAsync(new State { 
                    CreatedAt = DateTime.UtcNow,
                    StateName = QueueStateTypes.Enqueued,
                    JobID = JobQueueEntry.JobId,
                    Reason = "Retrying",
                    data = ExceptionMessage,
                });

                Job.stateID = stateID;
                Job.StateName =  QueueStateTypes.Enqueued;
                Job.RetryCount += 1;
                
                await _JobRepository.UpdateByIdAsync(Job);

                //make Job Visible again
                JobQueueEntry.IsScheduled = false;
                await _queueRepo.Update(JobQueueEntry);
            }
            
        }
        finally {
        
            await QueueLock.ReleaseLockAsync();
        }
       
    }

}