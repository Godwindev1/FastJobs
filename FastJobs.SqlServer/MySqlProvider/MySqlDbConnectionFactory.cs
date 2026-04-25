using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace FastJobs.SqlServer;


internal class MySqlDbConnectionFactory : DbConnectionFactory
{
    public MySqlDbConnectionFactory(FastJobsSqlStorageOptions jobsOptions)
        :base(jobsOptions)
    {
    }

    protected override void OpenConnection(IDbConnection connection)
    {
        connection.Open();
    }


    protected override IDbConnection GetConnection()
    {
        return new MySqlConnection(_jobsOptions.ConnectionString);
    }
}