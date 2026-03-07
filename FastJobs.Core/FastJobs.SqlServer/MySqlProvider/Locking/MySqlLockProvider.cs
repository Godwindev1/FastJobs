using System.Data;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;

namespace FastJobs.SqlServer;

internal class MySqlLockProvider  : LockProvider
{
    private readonly DbConnectionFactory dbConnectionFactory;

    public MySqlLockProvider(DbConnectionFactory factory)
    {
        dbConnectionFactory = factory;        
    }

    public override async Task<SessionDatabaseLock?> AcquireLock(string LockResourceName, TimeSpan Timeout)
    {
        MySqlConnection dbConnection = (MySqlConnection)dbConnectionFactory.CreateConnection();

        if(dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }

        using var cmd = dbConnection.CreateCommand();

        cmd.CommandText = "SELECT GET_LOCK(@resource, @timeout)";
        cmd.Parameters.AddWithValue("@resource", LockResourceName);
        cmd.Parameters.AddWithValue("@timeout", (int)Timeout.TotalSeconds);

        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        if(result == 1)
        {
            return new MySqlSessionDBLock(dbConnection, LockResourceName, Timeout);
        }
        
        await dbConnection.DisposeAsync();
        return null;
    }
    public new void ReleaseLock(SessionDatabaseLock Lock)
    {
        Lock.ReleaseLock();
    }

}