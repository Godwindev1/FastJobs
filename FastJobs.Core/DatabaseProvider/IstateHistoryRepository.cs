
namespace FastJobs;
public interface IStateHistoryRepository
{
    Task<long> InsertAsync(State job, CancellationToken ct);
    Task<State?> GetByIdAsync(int id, CancellationToken ct);

    Task InsertAsync(IEnumerable<State> states, CancellationToken ct);

}