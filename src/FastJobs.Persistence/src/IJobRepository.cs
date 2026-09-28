namespace FastJobs.Persistence;
public interface IJobRepository
{
    Task<long> InsertAsync(Job job, CancellationToken cancellationToken = default);

    Task<Job?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<List<Job>?> GetAllAsync( CancellationToken cancellationToken = default);

    Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<int> UpdateByIdAsync(Job job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flexibility in Setting Updates
    /// </summary>
    /// <param name="id"></param>
    /// <param name="SqlValues"> Format "field1 = value1, field2 = value2 ..." </param>
    /// <returns></returns>
    Task<int> UpdateByIdAsync(long id, string SqlValues,  Job job, CancellationToken cancellationToken = default);

    // Interface
    Task<int> CountByStateAsync(string stateName, CancellationToken cancellationToken = default);
    Task<int> CountRetryingAsync(CancellationToken cancellationToken = default);

    // Interface
    Task<int> CountCompletedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default);
    Task<int> CountFailedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default);
    Task<int> CountStateBetween(string statename, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    public Task<List<Job>> GetMisfiredJobsAsync(DateTimeOffset cutoff, CancellationToken ct = default);

    Task<int> PruneCompletedJobs(CancellationToken ct = default);
    Task<int> PruneExpiredJobs(CancellationToken ct = default);
}