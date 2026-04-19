namespace FastJobs;

public interface IScheduledJobRepository
{
    Task<long> InsertAsync(ScheduledJobInfo scheduledJob, CancellationToken cancellationToken = default);

    Task<ScheduledJobInfo?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<int> UpdateByIdAsync(ScheduledJobInfo scheduledJob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all scheduled jobs that are ready to be executed (ScheduledTo <= current UTC time)
    /// </summary>
    Task<IEnumerable<ScheduledJobInfo>> GetReadyJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple scheduled jobs by their IDs
    /// </summary>
    Task<int> DeleteMultipleAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);

    Task<ScheduledJobInfo?> GetNextScheduledJob(CancellationToken cancellationToken = default);
}
