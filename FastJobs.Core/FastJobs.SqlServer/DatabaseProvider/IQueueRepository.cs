namespace FastJobs;
public interface IQueueRepository
{
    Task<long> EnqueueAsync(Queue jobEntry);
    Task<Queue?> GetQueueEntry(long id);
    Task<bool> RemoveAsync(long id);
    Task<Queue?> Dequeue(string  Queuename);


}