namespace FastJobs.SqlServer;

//InternalsVisibleTo("FastJobs.core")] 
internal abstract class LockProvider 
{
    private readonly string LockOwner = "Session";
    private readonly string LockType = "Exclusive";

    public abstract Task<SessionDatabaseLock?> AcquireLock(string LockResourceName, TimeSpan Timeout);
    public void ReleaseLock(SessionDatabaseLock Lock)
    {
        Lock.ReleaseLock();
    }

}