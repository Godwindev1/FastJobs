
namespace FastJobs;

public class ProcessQueue
{
    private readonly IQueueRepository _queueRepo;
    private readonly IJobRepository _JobRepository;

    public ProcessQueue(IQueueRepository queueRepository, IJobRepository jobRepository)
    {
        _queueRepo = queueRepository;
        _JobRepository = jobRepository;
    }

    public void LockQueueItem()
    {
        
    }

    public async Task GetQueueItem(string QueueName)
    {
        var Entry  = await _queueRepo.Dequeue(QueueName);
        //Lock Entry
        //Update Job State
        //Retrieve And Return The Job
    }
}