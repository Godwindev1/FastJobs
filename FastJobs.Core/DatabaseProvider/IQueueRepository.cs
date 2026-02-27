namespace FastJobs;
public interface IQueueRepository
{
    Task<long> EnqueueAsync(Queue job);
    Task<Queue?> GetQueueEntry(long id);
    Task<bool> RemoveAsync(long id);
    

}