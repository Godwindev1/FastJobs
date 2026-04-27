
namespace FastJobs.SqlServer;
public interface IWorkerRepository
{
    Task<long> InsertAsync(FSTJBS_Worker worker, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<int> UpdateAsync(FSTJBS_Worker worker, CancellationToken cancellationToken = default);
    Task<FSTJBS_Worker> GetByID(long id, CancellationToken cancellationToken = default);
    Task<List<FSTJBS_Worker>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<FSTJBS_Worker>> GetSleepingAsync(CancellationToken cancellationToken = default);
    Task<List<FSTJBS_Worker>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<List<FSTJBS_Worker>> GetDeadWorkersAsync(CancellationToken cancellationToken = default);
}