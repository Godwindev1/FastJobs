using System.Data;
using Npgsql;

namespace FastJobs.Persistence;

internal class PostgresSessionDBLock : SessionDatabaseLock
{
    public PostgresSessionDBLock(IDbConnection connection, string resource, TimeSpan ttl)
        : base(connection, resource, ttl)
    {
    }

    public override void ReleaseLock()
    {
        if (_lockReleased) return;
        _lockReleased = true;

        var connection = (NpgsqlConnection)_connection;
        using var command = connection.CreateCommand();

        command.CommandText = "SELECT pg_advisory_unlock(@key)";
        command.Parameters.AddWithValue("key", PostgresLockProvider.ToLockKey(_LockResourceName));

        command.ExecuteScalar();
    }

    public override async Task ReleaseLockAsync()
    {
        if (_lockReleased) return;
        _lockReleased = true;

        var connection = (NpgsqlConnection)_connection;
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT pg_advisory_unlock(@key)";
        command.Parameters.AddWithValue("key", PostgresLockProvider.ToLockKey(_LockResourceName));

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
