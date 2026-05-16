using FastJobs.SqlServer;

namespace FastJobs;

public interface IAfterActionRepository
{
    // -------------------------------------------------------------------------
    // CRUD
    // -------------------------------------------------------------------------
    Task<long> InsertAsync(AfterActionModel action, CancellationToken cancellationToken = default);

    Task<AfterActionModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<List<AfterActionModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<AfterActionModel>> GetByJobIdAsync(long jobId, CancellationToken cancellationToken = default);

    Task<int> UpdateByIdAsync(AfterActionModel action, CancellationToken cancellationToken = default);

    Task<int> UpdateByIdAsync(long id, string sqlValues, AfterActionModel action, CancellationToken cancellationToken = default);

    Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<int> DeleteByJobIdAsync(long jobId, CancellationToken cancellationToken = default);

    // -------------------------------------------------------------------------
    // Observability
    // -------------------------------------------------------------------------
    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountRetryingAsync(CancellationToken cancellationToken = default);

    Task<int> CountExhaustedAsync(CancellationToken cancellationToken = default);

    Task<int> CountSucceededFirstAttemptAsync(CancellationToken cancellationToken = default);

    Task<int> CountByJobIdAsync(long jobId, CancellationToken cancellationToken = default);

    Task<double> AverageActionsPerJobAsync(CancellationToken cancellationToken = default);
}