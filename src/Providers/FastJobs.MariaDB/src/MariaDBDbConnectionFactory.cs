using System.Data.Common;
using MySqlConnector;

namespace FastJobs.Persistence;


internal class MySqlDbConnectionFactory : DbConnectionFactory
{
    public MySqlDbConnectionFactory(FastJobsSqlStorageOptions jobsOptions)
        :base(jobsOptions)
    {
    }

    protected override Task OpenConnectionAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        return connection.OpenAsync(cancellationToken);
    }


    protected override DbConnection GetConnection()
    {
        return new MySqlConnection(_jobsOptions.ConnectionString);
    }
}