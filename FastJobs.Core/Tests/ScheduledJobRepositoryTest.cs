using System.Data;
using System.Threading.Tasks;

namespace FastJobs;
using FastJobs.SqlServer;
internal class ScheduledJobRepositoryTest
{
    private IScheduledJobRepository TestObject;
    internal List<Tuple<bool, string>> TestResults = new List<Tuple<bool, string>>();

    public ScheduledJobRepositoryTest(IScheduledJobRepository repo)
    {
        TestObject = repo;
    }

    internal async Task<ScheduledJobInfo?> InsertAsync(ScheduledJobInfo scheduledJob)
    {
        var InsertedId = await TestObject.InsertAsync(scheduledJob);
        var InsertedScheduledJob = await TestObject.GetByIdAsync(InsertedId);
        return InsertedScheduledJob;
    }

    internal async Task<bool> GetByIdAsync(long id)
    {
        if (await TestObject.GetByIdAsync(id) == null)
        {
            return false;
        }

        return true;
    }

    internal async Task<bool> UpdateRecord(long id, ScheduledJobInfo scheduledJob)
    {
        scheduledJob.Id = id;
        var Result = await TestObject.UpdateByIdAsync(scheduledJob);

        if (Result == 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    internal async Task<bool> DeleteRecord(long id)
    {
        var Result = await TestObject.DeleteByIdAsync(id);

        if (Result == 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    internal async Task<bool> GetReadyJobs()
    {
        var Result = await TestObject.GetReadyJobsAsync();
        return Result.Any();
    }

    internal async Task<bool> DeleteMultipleJobs(IEnumerable<long> ids)
    {
        var Result = await TestObject.DeleteMultipleAsync(ids);
        return Result > 0;
    }
}
