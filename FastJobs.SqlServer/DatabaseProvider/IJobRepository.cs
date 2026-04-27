namespace FastJobs.SqlServer;
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
    Task<int> CountCompletedSinceAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<int> CountFailedSinceAsync(DateTime since, CancellationToken cancellationToken = default);
    Task<int> CountStateBetween(string statename, DateTime from, DateTime to, CancellationToken cancellationToken = default);


}