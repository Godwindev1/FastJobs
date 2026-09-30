using System.Data;
using Npgsql;

namespace FastJobs.Persistence;

internal class PostgresDbConnectionFactory : DbConnectionFactory
{
    public PostgresDbConnectionFactory(FastJobsSqlStorageOptions jobsOptions)
        : base(jobsOptions)
    {
    }

    protected override void OpenConnection(IDbConnection connection)
    {
        connection.Open();
    }

    protected override IDbConnection GetConnection()
    {
        return new NpgsqlConnection(_jobsOptions.ConnectionString);
    }
}
