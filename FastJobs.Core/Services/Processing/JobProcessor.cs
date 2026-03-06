
using FastJobs.SqlServer;

namespace FastJobs;

/// <summary>
/// JobProcessor is Responsible for Rettrieving A Job From the DB queue And Locking The DB entry And Returning Lock and Entry 
/// </summary>
internal class JobProcessor
{
    private readonly IQueueRepository _queueRepo;
    private readonly IJobRepository _JobRepository;
    private readonly LockProvider _locprovider;
    private readonly IStateHistoryRepository stateHistoryRepository;

    public JobProcessor(IQueueRepository queueRepository, IJobRepository jobRepository, IStateHistoryRepository stateRepo, LockProvider lockProvider)
    {
        _queueRepo = queueRepository;
        _JobRepository = jobRepository;
        _locprovider = lockProvider;
        stateHistoryRepository = stateRepo;
    }

    private Task<SessionDatabaseLock?> LockQueueItem(string QueueEntryID, string JobID)
    {
      return  _locprovider.AcquireLock($"FastJobs.{QueueEntryID}.{JobID}", TimeSpan.FromSeconds(120));   
    }

    public async Task<Tuple<Queue, SessionDatabaseLock>> DeQueueItem(string QueueName)
    {
        var Entry  = await _queueRepo.Dequeue(QueueName);
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

}