using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace FastJobs.Persistence;


internal class SqlServerDbCinnectionFactory : DbConnectionFactory
{
    public SqlServerDbCinnectionFactory(FastJobsSqlStorageOptions jobsOptions)
        :base(jobsOptions)
    {
    }

    protected override Task OpenConnectionAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        return connection.OpenAsync(cancellationToken);
    }


    protected override DbConnection GetConnection()
    {
        return new SqlConnection(_jobsOptions.ConnectionString);
    }
}