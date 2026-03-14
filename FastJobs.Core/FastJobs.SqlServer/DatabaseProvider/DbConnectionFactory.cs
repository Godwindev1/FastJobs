using System.Data;
using Microsoft.Extensions.Options;

namespace FastJobs.SqlServer;

/// <summary>
/// This Class is an Interface for  Creating And Opens IdbConnections On The Go It Does Not Dispose This Connections 
/// So Implemented Services Using Connections From these Must handle Closing or Disposing IdbConnections To Avoid Starving 
/// The Connection Pool
/// </summary>
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