
namespace FastJobs.SqlServer;


internal abstract class LockProvider 
{
    
    protected static readonly IDictionary<int, string> LockErrorMessages
    = new Dictionary<int, string>
    {
        { -999, "Indicates a parameter validation or other call error" }
    }; 

    public abstract Task<SessionDatabaseLock?> AcquireLock(string LockResourceName, TimeSpan Timeout, CancellationToken cancellationToken = default);
    public void ReleaseLock(SessionDatabaseLock Lock)
    {
        Lock.ReleaseLock();
    }

}