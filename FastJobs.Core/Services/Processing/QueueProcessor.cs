
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

    private Task<SessionDatabaseLock?> LockQueueItem(string QueueEntryID, string JobID)
    {
      return  _locprovider.AcquireLock($"FastJobs.{QueueEntryID}.{JobID}", TimeSpan.FromMinutes(5));   
    }

    public async Task<bool> IsQueueEmpty(string QueueName)
    {
        var result = await _queueRepo.Dequeue(QueueName);
        if(result == null)
        {
            return true;
        }

        return false;
    }
    public async Task<Tuple<Queue, SessionDatabaseLock>?> DeQueueItem(string QueueName)
    {
        Queue? Entry  = await _queueRepo.Dequeue(QueueName);
        if(Entry != null)
        {
            //Does not Do A Visibilty Hide On the Dequeued Work Yet 
            SessionDatabaseLock CurrentWorkerHeldLock =  await LockQueueItem(Entry.Id.ToString(), Entry.JobId.ToString()) ; 
            var Job = await _JobRepository.GetByIdAsync(Entry.JobId);
            
            Job.StateName = QueueStateTypes.Scheduled;
            await _JobRepository.UpdateByIdAsync(Job);

            await stateHistoryRepository.InsertAsync(new State { 
                CreatedAt = DateTime.Now,
                StateName = QueueStateTypes.Scheduled,
                JobID = Job.Id,
                Reason = "Schedule Job For Processing",
                data = "",
            });
            
            return new Tuple<Queue, SessionDatabaseLock>( Entry, CurrentWorkerHeldLock );
        
        }

        return null;
    }

}