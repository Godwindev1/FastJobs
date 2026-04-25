
namespace FastJobs.SqlServer;
public interface IStateHistoryRepository
{
    Task<long> InsertAsync(State job, CancellationToken cancellationToken = default);
    Task<State?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task InsertAsync(IEnumerable<State> states, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a StateHistory entry by marking it with a DeletedAt timestamp.
    /// The entry remains in the database for audit purposes but is excluded from normal queries.
    /// This is used for rolling back failed state transitions while maintaining the audit trail.
    /// </summary>
    /// <param name="id">The ID of the state history entry to soft delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows affected (1 if successful, 0 if not found)</returns>
    Task<int> SoftDeleteByIdAsync(long id, CancellationToken cancellationToken = default);
}