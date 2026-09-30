using System.Data;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace FastJobs.Persistence;

internal class PostgresLockProvider : LockProvider
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly DbConnectionFactory dbConnectionFactory;

    public PostgresLockProvider(DbConnectionFactory factory)
    {
        dbConnectionFactory = factory;
    }

    /// <summary>
    /// Postgres advisory locks are keyed by a 64-bit integer, so the resource name is hashed to one.
    /// </summary>
    internal static long ToLockKey(string resourceName)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(resourceName));
        return BitConverter.ToInt64(hash, 0);
    }

    public override async Task<SessionDatabaseLock?> AcquireLock(string LockResourceName, TimeSpan Timeout, CancellationToken cancellationToken)
    {
        NpgsqlConnection dbConnection = (NpgsqlConnection)dbConnectionFactory.CreateConnection();

        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }

        long key = ToLockKey(LockResourceName);
        var deadline = DateTime.UtcNow + Timeout;

        try
        {
            while (true)
            {
                await using var cmd = dbConnection.CreateCommand();
                cmd.CommandText = "SELECT pg_try_advisory_lock(@key)";
                cmd.Parameters.AddWithValue("key", key);

                var scalarResult = await cmd.ExecuteScalarAsync(cancellationToken);

                if (scalarResult is bool acquired && acquired)
                {
                    return new PostgresSessionDBLock(dbConnection, LockResourceName, Timeout);
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                await Task.Delay(remaining < PollInterval ? remaining : PollInterval, cancellationToken);
            }
        }
        catch
        {
            await dbConnection.DisposeAsync();
            throw;
        }

        await dbConnection.DisposeAsync();
        return null;
    }
}
