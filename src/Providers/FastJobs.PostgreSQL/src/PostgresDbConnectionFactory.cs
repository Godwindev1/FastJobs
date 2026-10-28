using System.Data.Common;
using Npgsql;

namespace FastJobs.Persistence;

internal class PostgresDbConnectionFactory : DbConnectionFactory
{
    public PostgresDbConnectionFactory(FastJobsSqlStorageOptions jobsOptions)
        : base(jobsOptions)
    {
    }

    protected override Task OpenConnectionAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        return connection.OpenAsync(cancellationToken);
    }

    protected override DbConnection GetConnection()
    {
        return new NpgsqlConnection(_jobsOptions.ConnectionString);
    }
}
