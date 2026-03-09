using System.Data;

namespace FastJobs.SqlServer;

internal abstract class SessionDatabaseLock : IDisposable, IAsyncDisposable
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

        // Must explicitly release — session locks do NOT release on transaction end
        ReleaseLock();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await ReleaseLockAsync();
        _connection.Dispose();
    }

    public string GetResoureName()
    {
        return _LockResourceName;
    }
    public abstract void ReleaseLock();
    public abstract Task ReleaseLockAsync();
}
