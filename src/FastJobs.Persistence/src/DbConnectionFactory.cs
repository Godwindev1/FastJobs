using System.Data.Common;

namespace FastJobs.Persistence;

/// <summary>
/// Creates and opens connections. Callers must dispose returned connections.
/// </summary>
public abstract class DbConnectionFactory
{
    protected FastJobsSqlStorageOptions _jobsOptions;

    public DbConnectionFactory(FastJobsSqlStorageOptions jobsOptions)
    {
        _jobsOptions = jobsOptions;
    }

    public async Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            await OpenConnectionAsync(connection, cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    protected abstract Task OpenConnectionAsync(DbConnection connection, CancellationToken cancellationToken);
    protected abstract DbConnection GetConnection();
}