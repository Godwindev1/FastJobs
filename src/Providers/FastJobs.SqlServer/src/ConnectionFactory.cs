using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

namespace FastJobs.Persistence;


internal class SqlServerDbCinnectionFactory : DbConnectionFactory
{
    public SqlServerDbCinnectionFactory(FastJobsSqlStorageOptions jobsOptions)
        :base(jobsOptions)
    {
    }

    protected override void OpenConnection(IDbConnection connection)
    {
        connection.Open();
    }


    protected override IDbConnection GetConnection()
    {
        return new SqlConnection(_jobsOptions.ConnectionString);
    }
}