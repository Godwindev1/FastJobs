using System.Data;

namespace FastJobs.SqlServer;

// ============================================================
// SESSION-SCOPED DATABASE LOCK
// Lock survives transaction commits — must be manually released
// ============================================================

internal abstract class SessionDatabaseLock : IDisposable, IAsyncDisposable
{

    protected static readonly IDictionary<int, string> LockErrorMessages
    = new Dictionary<int, string>
    {
        { -1, "The lock request timed out" },
        { -2, "The lock request was canceled" },
        { -3, "The lock request was chosen as a deadlock victim" },
        { -999, "Indicates a parameter validation or other call error" }
    };

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
