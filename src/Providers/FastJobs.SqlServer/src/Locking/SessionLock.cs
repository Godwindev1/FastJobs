
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace FastJobs.Persistence;

internal class MSSQLSessionDBLock : SessionDatabaseLock
{
    public MSSQLSessionDBLock(DbConnection connection, string resource, TimeSpan ttl)
        : base(connection, resource, ttl)
    {
    }

    public override void ReleaseLock()
    {
        if (_lockReleased) return;
        _lockReleased = true;

        SqlConnection connection = (SqlConnection)this._connection;
        using var command = connection.CreateCommand();

        command.CommandText = "EXEC sp_releaseapplock @Resource, @LockOwner";
        command.Parameters.AddWithValue("@Resource", this._LockResourceName);
        command.Parameters.AddWithValue("@LockOwner", "Session");

        command.ExecuteScalar();
    }

    public override async Task ReleaseLockAsync()
    {
        if (_lockReleased) return;
        _lockReleased = true;

        SqlConnection connection = (SqlConnection)this._connection;
        using var command = connection.CreateCommand();

        command.CommandText = "EXEC sp_releaseapplock @Resource, @LockOwner";
        command.Parameters.AddWithValue("@Resource", this._LockResourceName);
        command.Parameters.AddWithValue("@LockOwner", "Session");

        await command.ExecuteScalarAsync();
    }

    public override void Dispose()
    {
        if (!_lockReleased)
        {
            ReleaseLock();
        }

        base.Dispose();
    }
}
