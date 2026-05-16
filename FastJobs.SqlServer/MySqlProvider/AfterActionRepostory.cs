using Dapper;
using MySqlConnector;

namespace FastJobs.SqlServer;

internal sealed class AfterActionRepository : IAfterActionRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public AfterActionRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // -------------------------------------------------------------------------
    // CRUD
    // -------------------------------------------------------------------------

    /// <summary>
    /// Inserts a new AfterAction and returns its generated Id.
    /// </summary>
    public async Task<long> InsertAsync(AfterActionModel action, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO AfterActions
                (TypeName, Retries, MaxRetries, JobId, NextActionId, LastActionId, ChainNo)
            VALUES
                (@TypeName, @Retries, @MaxRetries, @JobId, @NextActionId, @LastActionId, @ChainNo);

            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, action, cancellationToken: cancellationToken));
    }

    public async Task<AfterActionModel?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM AfterActions WHERE Id = @Id;";

        return await connection.QuerySingleOrDefaultAsync<AfterActionModel>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<List<AfterActionModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM AfterActions ORDER BY JobId, ChainNo;";

        var result = await connection.QueryAsync<AfterActionModel>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return result.ToList();
    }

    /// <summary>
    /// Returns all AfterActions belonging to a job, ordered by chain position.
    /// </summary>
    public async Task<List<AfterActionModel>> GetByJobIdAsync(long jobId, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT * FROM AfterActions
            WHERE JobId = @JobId
            ORDER BY ChainNo;";

        var result = await connection.QueryAsync<AfterActionModel>(
            new CommandDefinition(sql, new { JobId = jobId }, cancellationToken: cancellationToken));

        return result.ToList();
    }

    /// <summary>
    /// Full record update for a given AfterAction.
    /// </summary>
    public async Task<int> UpdateByIdAsync(AfterActionModel action, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            UPDATE AfterActions
            SET
                TypeName     = @TypeName,
                Retries      = @Retries,
                MaxRetries   = @MaxRetries,
                JobId        = @JobId,
                NextActionId = @NextActionId,
                LastActionId = @LastActionId,
                ChainNo      = @ChainNo
            WHERE Id = @Id;";

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, action, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Partial update — pass only the SET clause fragment, e.g. "Retries = @Retries".
    /// </summary>
    public async Task<int> UpdateByIdAsync(
        long id,
        string sqlValues,
        AfterActionModel action,
        CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        string sql = $@"
            UPDATE AfterActions
            SET {sqlValues}
            WHERE Id = {id};";

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, action, cancellationToken: cancellationToken));
    }

    public async Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM AfterActions WHERE Id = @Id;";

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Deletes all AfterActions tied to a job — useful on job cleanup/expiry.
    /// </summary>
    public async Task<int> DeleteByJobIdAsync(long jobId, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM AfterActions WHERE JobId = @JobId;";

        return await connection.ExecuteAsync(
            new CommandDefinition(sql, new { JobId = jobId }, cancellationToken: cancellationToken));
    }

    // -------------------------------------------------------------------------
    // Observability
    // -------------------------------------------------------------------------

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT COUNT(*) FROM AfterActions;";

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<int> CountRetryingAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM AfterActions
            WHERE Retries > 0
              AND Retries < MaxRetries;";

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<int> CountExhaustedAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM AfterActions
            WHERE Retries >= MaxRetries;";

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<int> CountSucceededFirstAttemptAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM AfterActions
            WHERE Retries = 0;";

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<int> CountByJobIdAsync(long jobId, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM AfterActions
            WHERE JobId = @JobId;";

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { JobId = jobId }, cancellationToken: cancellationToken));
    }

    public async Task<double> AverageActionsPerJobAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT AVG(action_count) FROM (
                SELECT COUNT(*) AS action_count
                FROM AfterActions
                GROUP BY JobId
            ) AS per_job;";

        return await connection.ExecuteScalarAsync<double>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}