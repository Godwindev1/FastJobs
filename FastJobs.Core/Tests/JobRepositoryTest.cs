namespace FastJobs;
using FastJobs.SqlServer;
public class JobRepoSitoryTest
{
    internal IJobRepository Testobject;
    internal List<Tuple<bool, string>> TestResults  = new List<Tuple<bool, string>>();


    public  JobRepoSitoryTest( IJobRepository repo )
    {
        Testobject = repo;
    }


    internal async Task<Job?> InsertAsync(Job job)
    {
        var InsertedjobID = await Testobject.InsertAsync(job);

        var Insertedjob = await Testobject.GetByIdAsync(InsertedjobID);
        return Insertedjob;
    }

    internal async Task<bool> GetByIdAsync(long id)
    {
        
        if(await Testobject.GetByIdAsync(id) == null)
        {
            return false;
        }

        return true;
    }


    internal async Task<bool> UpdateRecord(long id, string SqlValues, Job job)
    {
        var Result = await Testobject.UpdateByIdAsync(id, SqlValues, job);

        if(Result== 0)
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
        var Result = await Testobject.DeleteByIdAsync(id);

        if(Result == 0)
        {
            return false;
        }
        else
        {
            return true ;
        }

    }


}