using System.Data;

namespace FastJobs.SqlServer;

internal abstract class SessionDatabaseLock : IDisposable
{
    protected readonly IDbConnection _connection;
    protected readonly string _LockResourceName;
    protected bool _disposed;

    protected TimeSpan TTL  = TimeSpan.FromMinutes(1);

    public SessionDatabaseLock(IDbConnection connection, string resource, TimeSpan ttl)
    {
        _connection = connection;
        _LockResourceName = resource;
        TTL = ttl;
    }

    public void Dispose()
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
    /// Child Implementation Should Include a Call To Dispose() after Releasing
    /// </summary>
    public abstract void ReleaseLock();
    /// <summary>
    /// Child Implementation Should Include a Call To Dispose() after Releasing
    /// </summary>
    public abstract Task ReleaseLockAsync();
}
