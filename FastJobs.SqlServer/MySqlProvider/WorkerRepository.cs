using Dapper;
using MySqlConnector;

namespace FastJobs.SqlServer;

internal sealed class WorkerRepository : IWorkerRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public WorkerRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> InsertAsync(FSTJBS_Worker worker, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO Workers (WorkerName, ThreadName, StartedAt, isSleeping, isCrashed, LastHeartbeat)
            VALUES (@WorkerName, @ThreadName, @StartedAt, @isSleeping, @isCrashed, @LastHeartbeat);

            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, worker, cancellationToken: cancellationToken));
    }

    public async Task<int> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM Workers WHERE Id = @Id";

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<int> UpdateAsync(FSTJBS_Worker worker, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            UPDATE Workers
            SET WorkerName    = @WorkerName,
                ThreadName    = @ThreadName,
                StartedAt     = @StartedAt,
                isSleeping    = @isSleeping,
                isCrashed = @isCrashed,
                LastHeartbeat = @LastHeartbeat
            WHERE Id = @Id";

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, worker, cancellationToken: cancellationToken));
    }

    public async Task<List<FSTJBS_Worker>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Workers";

        var result = await connection.QueryAsync<FSTJBS_Worker>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return result.ToList();
    }

    public async Task<List<FSTJBS_Worker>> GetSleepingAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Workers WHERE isSleeping = 1 AND isCrashed = 0";

        var result = await connection.QueryAsync<FSTJBS_Worker>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return result.ToList();
    }

    public async Task<List<FSTJBS_Worker>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Workers WHERE isSleeping = 0 AND isCrashed = 0";

        var result = await connection.QueryAsync<FSTJBS_Worker>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return result.ToList();
    }

    public async Task<List<FSTJBS_Worker>> GetDeadWorkersAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Workers WHERE isCrashed = 1";

        var result = await connection.QueryAsync<FSTJBS_Worker>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return result.ToList();
    }

    public async Task<FSTJBS_Worker?> GetByID(long id, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Workers WHERE Id = @Id";

        return await connection.QuerySingleOrDefaultAsync<FSTJBS_Worker>(
            new CommandDefinition(sql, new { id },  cancellationToken: cancellationToken));
    }

    public async Task TruncateAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "TRUNCATE TABLE Workers";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

}