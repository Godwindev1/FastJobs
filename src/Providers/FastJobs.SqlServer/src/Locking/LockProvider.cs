using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace FastJobs.Persistence;

internal class MSSQLLockProvider : LockProvider
{
    private readonly DbConnectionFactory dbConnectionFactory;

    public MSSQLLockProvider(DbConnectionFactory factory)
    {
        dbConnectionFactory = factory;
    }

    public override async Task<SessionDatabaseLock?> AcquireLock(string LockResourceName, TimeSpan Timeout, CancellationToken cancellationToken)
    {
        SqlConnection dbConnection = (SqlConnection)dbConnectionFactory.CreateConnection();

        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }

        await using var cmd = dbConnection.CreateCommand();
        cmd.CommandText = "EXEC sp_getapplock @Resource, @LockMode, @LockOwner, @LockTimeout";
        cmd.Parameters.AddWithValue("@Resource", LockResourceName);
        cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
        cmd.Parameters.AddWithValue("@LockOwner", "Session");
        cmd.Parameters.AddWithValue("@LockTimeout", (int)Timeout.TotalMilliseconds);

        var scalarResult = await cmd.ExecuteScalarAsync(cancellationToken);
        int result = scalarResult == DBNull.Value || scalarResult == null
            ? 0
            : Convert.ToInt32(scalarResult);

        if (result == 0)
        {
            return new MSSQLSessionDBLock(dbConnection, LockResourceName, Timeout);
        }

        await dbConnection.DisposeAsync();
        return null;
    }

    public new void ReleaseLock(SessionDatabaseLock Lock)
    {
        Lock.ReleaseLock();
    }
}