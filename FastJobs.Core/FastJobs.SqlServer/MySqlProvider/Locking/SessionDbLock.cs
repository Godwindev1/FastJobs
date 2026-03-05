

using System.Data;
using MySqlConnector;
namespace FastJobs.SqlServer;

internal class MySqlSessionDBLock : SessionDatabaseLock
{
    public MySqlSessionDBLock(IDbConnection connection, string resource, TimeSpan ttl)
    :base(connection, resource, ttl)
    {
    }


    public override void ReleaseLock()
    {
        MySqlConnection connection = (MySqlConnection)this._connection;
        var Command = connection.CreateCommand();

        Command.CommandText = "SELECT RELEASE_LOCK(@ResourceName)";
        Command.Parameters.AddWithValue("@ResourceName", this._LockResourceName);
        
        var result = Convert.ToInt32(Command.ExecuteScalar());
    }
    public override async Task ReleaseLockAsync()
    {
        MySqlConnection connection = (MySqlConnection)this._connection;
        var Command = connection.CreateCommand();

        Command.CommandText = "SELECT RELEASE_LOCK(@ResourceName)";
        Command.Parameters.AddWithValue("@ResourceName", this._LockResourceName);
        
        var result = Convert.ToInt32(await Command.ExecuteScalarAsync());

    }
}
