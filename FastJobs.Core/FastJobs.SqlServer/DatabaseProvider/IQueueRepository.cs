namespace FastJobs;
public interface IQueueRepository
{
    Task<long> EnqueueAsync(Queue jobEntry, CancellationToken cancellationToken = default);
    Task<Queue?> GetQueueEntry(long id, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(long id, CancellationToken cancellationToken = default);
    Task<Queue?> Dequeue(string  Queuename, CancellationToken cancellationToken = default);
    public Task<int> Update(Queue queueEntry, CancellationToken cancellationToken = default);

}