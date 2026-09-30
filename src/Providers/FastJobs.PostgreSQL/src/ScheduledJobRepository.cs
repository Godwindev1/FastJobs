using Dapper;
using Npgsql;

namespace FastJobs.Persistence;

internal sealed class ScheduledJobRepository : IScheduledJobRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public ScheduledJobRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Inserts a new scheduled job into the ScheduledJobs table
    /// </summary>
    public async Task<long> InsertAsync(ScheduledJobInfo scheduledJob, CancellationToken cancellationToken)
    {
        using NpgsqlConnection connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO ScheduledJobs
        (JobId, ScheduledTo)
        VALUES
        (@JobId, @ScheduledTo)
        RETURNING Id;";

        var command = new CommandDefinition(sql, new
        {
            scheduledJob.JobId,
            scheduledJob.ScheduledTo
        }, cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<long>(command);
    }

    public async Task<ScheduledJobInfo?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        using NpgsqlConnection connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM ScheduledJobs WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ScheduledJobInfo>(command);
    }

    public async Task<List<ScheduledJobInfo>> GetAllAsync(CancellationToken cancellationToken)
    {
        using NpgsqlConnection connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM ScheduledJobs;";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var result = await connection.QueryAsync<ScheduledJobInfo>(command);
        return result.ToList();
    }

    public async Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken)
    {
        using NpgsqlConnection connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM ScheduledJobs WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    /// <summary>
    /// Updates a complete scheduled job record
    /// </summary>
    public async Task<int> UpdateByIdAsync(ScheduledJobInfo scheduledJob, CancellationToken cancellationToken)
    {
        using NpgsqlConnection connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        UPDATE ScheduledJobs
        SET
            JobId = @JobId,
            ScheduledTo = @ScheduledTo
        WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new
        {
            scheduledJob.Id,
            scheduledJob.JobId,
            scheduledJob.ScheduledTo
        }, cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command);
    }

    /// <summary>
    /// Retrieves all scheduled jobs that are ready to be executed (ScheduledTo &lt;= current UTC time)
    /// </summary>
    public async Task<IEnumerable<ScheduledJobInfo>> GetReadyJobsAsync(CancellationToken cancellationToken)
    {
        using NpgsqlConnection connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        SELECT * FROM ScheduledJobs
        WHERE ScheduledTo <= @CurrentTime
        ORDER BY ScheduledTo ASC;";

        var command = new CommandDefinition(sql,
            new { CurrentTime = DateTimeOffset.UtcNow },
            cancellationToken: cancellationToken);

        return await connection.QueryAsync<ScheduledJobInfo>(command);
    }

    /// <summary>
    /// Removes multiple scheduled jobs by their IDs
    /// </summary>
    public async Task<int> DeleteMultipleAsync(IEnumerable<long> ids, CancellationToken cancellationToken)
    {
        var idArray = ids.ToArray();
        if (idArray.Length == 0)
            return 0;

        using NpgsqlConnection connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM ScheduledJobs WHERE Id = ANY(@Ids);";

        var command = new CommandDefinition(sql,
            new { Ids = idArray },
            cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command);
    }

    public async Task<ScheduledJobInfo?> GetNextScheduledJob(CancellationToken ct)
    {
        using NpgsqlConnection connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT * FROM ScheduledJobs
            WHERE ScheduledTo > @CurrentTime
            ORDER BY ScheduledTo ASC
            LIMIT 1;";

        return await connection.QueryFirstOrDefaultAsync<ScheduledJobInfo>(
            new CommandDefinition(sql,
                new { CurrentTime = DateTimeOffset.UtcNow },
                cancellationToken: ct));
    }
}
