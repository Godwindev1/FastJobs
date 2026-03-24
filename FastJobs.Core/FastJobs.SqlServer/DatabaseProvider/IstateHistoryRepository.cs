
namespace FastJobs;
public interface IStateHistoryRepository
{
    Task<long> InsertAsync(State job, CancellationToken cancellationToken = default);
    Task<State?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task InsertAsync(IEnumerable<State> states, CancellationToken cancellationToken = default);

}