using System.ComponentModel;
using System.Data;

namespace FastJobs.Persistence;

//[EditorBrowsable(EditorBrowsableState.Never)] //To stop End users from Seeing this 
public abstract class SessionDatabaseLock : IDisposable
{
    protected readonly IDbConnection _connection;
    protected readonly string _LockResourceName;
    protected bool _disposed;
    protected bool _lockReleased;

    protected TimeSpan TTL  = TimeSpan.FromMinutes(1);

    public SessionDatabaseLock(IDbConnection connection, string resource, TimeSpan ttl)
    {
        _connection = connection;
        _LockResourceName = resource;
        TTL = ttl;
    }

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _connection.Dispose();
        
    }

    public string GetResoureName()
    {
        return _LockResourceName;
    }

    /// <summary>
    /// Child Implementation Should Release the Lock
    /// </summary>
    public abstract void ReleaseLock();
    /// <summary>
    /// Child Implementation Should Release the Lock
    /// </summary>
    public abstract Task ReleaseLockAsync();
}
