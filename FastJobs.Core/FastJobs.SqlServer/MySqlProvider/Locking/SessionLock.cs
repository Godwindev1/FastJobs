

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
        if (_lockReleased) return;
        _lockReleased = true;

        MySqlConnection connection = (MySqlConnection)this._connection;
        var Command = connection.CreateCommand();

        Command.CommandText = "SELECT RELEASE_LOCK(@ResourceName)";
        Command.Parameters.AddWithValue("@ResourceName", this._LockResourceName);
        
        Command.ExecuteScalar();
    }
    public override async Task ReleaseLockAsync()
    {
        if (_lockReleased) return;
        _lockReleased = true;

        MySqlConnection connection = (MySqlConnection)this._connection;
        var Command = connection.CreateCommand();

        Command.CommandText = "SELECT RELEASE_LOCK(@ResourceName)";
        Command.Parameters.AddWithValue("@ResourceName", this._LockResourceName);
        
        await Command.ExecuteScalarAsync();
    }

    public override void Dispose()
    {
        if (!_lockReleased)
        {
            ReleaseLock();
        }
        base.Dispose();
    }
}
