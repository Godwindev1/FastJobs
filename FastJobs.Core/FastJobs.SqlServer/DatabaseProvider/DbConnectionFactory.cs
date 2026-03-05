using System.Data;
using Microsoft.Extensions.Options;

namespace FastJobs.SqlServer;


internal abstract class DbConnectionFactory
{
    protected FastJobsOptions _jobsOptions;

    public DbConnectionFactory(FastJobsOptions jobsOptions)
    {
        _jobsOptions = jobsOptions;
    }

    public IDbConnection CreateConnection()
    {
        var connection = GetConnection();
        OpenConnection(connection);

        return connection;
    }

    protected abstract void OpenConnection(IDbConnection connection);
    protected abstract IDbConnection GetConnection();
}