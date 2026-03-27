using System.Data.SqlTypes;
using FastJobs.SqlServer;

namespace FastJobs;
public class DBResourceLockingTest
{
    internal LockProvider Testobject;
    internal SessionDatabaseLock? lockReference;
    internal List<Tuple<bool, string>> TestResults  = new List<Tuple<bool, string>>();

    internal DBResourceLockingTest(LockProvider lockProvider)
    {
        Testobject = lockProvider;
    }

    public async Task<bool> AcquireLock(string resourceName, TimeSpan timeSpan)
    {
        lockReference = await Testobject.AcquireLock(resourceName, timeSpan);

        if(lockReference == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public bool ReleaseLock()
    {
        if(lockReference != null)
        Testobject.ReleaseLock(lockReference);
        lockReference?.Dispose();

        
        return true;
    }

}
