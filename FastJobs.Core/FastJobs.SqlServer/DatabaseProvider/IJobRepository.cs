
namespace FastJobs;
public interface IJobRepository
{
    Task<long> InsertAsync(Job job);

    Task<Job?> GetByIdAsync(long id);

    Task<int> DeleteByIdAsync(long id);

    Task<int> UpdateByIdAsync(Job job);

    /// <summary>
    /// Flexibility in Setting Updates
    /// </summary>
    /// <param name="id"></param>
    /// <param name="SqlValues"> Format "field1 = value1, field2 = value2 ..." </param>
    /// <returns></returns>
    Task<int> UpdateByIdAsync(long id, string SqlValues,  Job job);
}