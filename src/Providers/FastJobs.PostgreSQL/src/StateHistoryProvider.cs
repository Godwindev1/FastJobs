using Dapper;
using Npgsql;

namespace FastJobs.Persistence;

internal sealed class StateHistoryRepository : IStateHistoryRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public StateHistoryRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> InsertAsync(State job, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO State
        (StateName, data, Reason, JobId, CreatedAt)
        VALUES
        (@StateName, @data, @Reason, @JobId, @CreatedAt)
        RETURNING Id;";

        return await _connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, job, cancellationToken: cancellationToken));
    }

    public async Task InsertAsync(IEnumerable<State> states, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO State
            (StateName, Data, Reason, JobId, CreatedAt)
            VALUES
            (@StateName, @Data, @Reason, @JobId, @CreatedAt);";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, states, cancellationToken: cancellationToken));
    }

    public async Task<State?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM State WHERE Id = @Id AND DeletedAt IS NULL";

        return await _connection.QuerySingleOrDefaultAsync<State>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<int> SoftDeleteByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            UPDATE State
            SET DeletedAt = @DeletedAt
            WHERE Id = @Id AND DeletedAt IS NULL";

        return await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { DeletedAt = DateTimeOffset.UtcNow, Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<JobTimestamps?> GetTimestampsByJobIdAsync(long jobId, CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                s.EnqueuedAt,
                s.StartedAt,
                (SELECT MIN(c.CreatedAt)
                 FROM State c
                 WHERE c.JobId = @JobId
                   AND c.DeletedAt IS NULL
                   AND c.StateName IN ('Completed', 'Failed')
                   AND c.CreatedAt >= s.StartedAt) AS CompletedAt
            FROM (
                SELECT
                    MAX(CASE WHEN StateName = 'Enqueued'   THEN CreatedAt END) AS EnqueuedAt,
                    MAX(CASE WHEN StateName = 'Processing' THEN CreatedAt END) AS StartedAt
                FROM State
                WHERE JobId = @JobId
                AND DeletedAt IS NULL
            ) s";

        return await _connection.QuerySingleOrDefaultAsync<JobTimestamps>(
            new CommandDefinition(sql, new { JobId = jobId }, cancellationToken: cancellationToken));
    }
}
