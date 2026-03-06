
namespace FastJobs;
public interface IStateHistoryRepository
{
    Task<long> InsertAsync(State job);
    Task<State?> GetByIdAsync(int id);

    Task InsertAsync(IEnumerable<State> states);

}