using System.Data;
using Dapper;
using FastJobs;
using MySqlConnector;

namespace FastJobs.SqlServer;

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
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO ScheduledJobs 
        (JobId, ScheduledTo)
        VALUES 
        (@JobId, @ScheduledTo);

        SELECT LAST_INSERT_ID();";

        var command = new CommandDefinition(sql, new
        {
            scheduledJob.JobId,
            scheduledJob.ScheduledTo
        }, cancellationToken: cancellationToken);

        var id = await connection.ExecuteScalarAsync<long>(command);
        return id;
    }

    /// <summary>
    /// Retrieves a scheduled job by its ID
    /// </summary>
    public async Task<ScheduledJobInfo?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM ScheduledJobs WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ScheduledJobInfo>(command);
    }

    /// <summary>
    /// Deletes a scheduled job by its ID
    /// </summary>
    public async Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM ScheduledJobs WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    /// <summary>
    /// Updates a complete scheduled job record
    /// </summary>
    public async Task<int> UpdateByIdAsync(ScheduledJobInfo scheduledJob, CancellationToken cancellationToken)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

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
    /// Retrieves all scheduled jobs that are ready to be executed (ScheduledTo <= current UTC time)
    /// </summary>
    public async Task<IEnumerable<ScheduledJobInfo>> GetReadyJobsAsync(CancellationToken cancellationToken)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        SELECT * FROM ScheduledJobs 
        WHERE ScheduledTo <= @CurrentTime
        ORDER BY ScheduledTo ASC;";

        var command = new CommandDefinition(sql, 
            new { CurrentTime = DateTime.UtcNow }, 
            cancellationToken: cancellationToken);

        var result = await connection.QueryAsync<ScheduledJobInfo>(command);
        return result;
    }

    /// <summary>
    /// Removes multiple scheduled jobs by their IDs
    /// </summary>
    public async Task<int> DeleteMultipleAsync(IEnumerable<long> ids, CancellationToken cancellationToken)
    {
        if (!ids.Any())
            return 0;

        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM ScheduledJobs WHERE Id IN @Ids;";

        var command = new CommandDefinition(sql, 
            new { Ids = ids }, 
            cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command);
    }


    public async Task<ScheduledJobInfo?> GetNextScheduledJob(CancellationToken ct)
    {
        using var connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT * FROM ScheduledJobs
            WHERE ScheduledTo > @CurrentTime
            ORDER BY ScheduledTo ASC
            LIMIT 1;";

        return await connection.QueryFirstOrDefaultAsync<ScheduledJobInfo>(
            new CommandDefinition(sql, 
                new { CurrentTime = DateTime.UtcNow }, 
                cancellationToken: ct));

    }
}
