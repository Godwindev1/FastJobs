using Dapper;
using FastJobs;
using MySqlConnector;

namespace FastJobs.SqlServer;

internal sealed class RecurringJobRepository : IRecurringJobRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public RecurringJobRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> InsertAsync(RecurringJob recurringJob, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO RecurringJobs
            (JobId, NextScheduledID, CronExpression, StartTime, IntervalTicks, NextScheduledTime, IsConcurrent, isCron, ExecutingInstances, ExecutedInstances)
        VALUES
            (@JobId, @NextScheduledID, @CronExpression, @StartTime, @IntervalTicks, @NextScheduledTime, @IsConcurrent, @isCron, @ExecutingInstances, @ExecutedInstances);

        SELECT LAST_INSERT_ID();";

        var command = new CommandDefinition(sql, new
        {
            recurringJob.JobId,
            recurringJob.NextScheduledID,
            recurringJob.CronExpression,
            recurringJob.StartTime,
            IntervalTicks         = recurringJob.IntervalTicks,   // TimeSpan → BIGINT
            recurringJob.NextScheduledTime,
            recurringJob.IsConcurrent,
            recurringJob.IsCron,
            recurringJob.ExecutingInstances,
            recurringJob.ExecutedInstances
        }, cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<long>(command);
    }

    public async Task<RecurringJob?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM RecurringJobs WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);

        // Dapper cannot auto-map BIGINT → TimeSpan, so we use a raw row and convert manually.
        var row = await connection.QuerySingleOrDefaultAsync<RecurringJobRow>(command);
        return row is null ? null : MapToDomain(row);
    }

    public async Task<IEnumerable<RecurringJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM RecurringJobs ORDER BY Id ASC;";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<RecurringJobRow>(command);
        return rows.Select(MapToDomain);
    }

    public async Task<IEnumerable<RecurringJob>> GetOrphanedRecurringJobsAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        SELECT r.* FROM RecurringJobs r
        LEFT JOIN ScheduledJobs s ON r.NextScheduledID = s.Id
        LEFT JOIN Jobs j ON r.JobId = j.Id
        WHERE (j.ExpiresAt IS NULL OR j.ExpiresAt > UTC_TIMESTAMP())
        AND (r.NextScheduledID IS NULL OR s.Id IS NULL)
        AND r.NextScheduledTime < UTC_TIMESTAMP();";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<RecurringJobRow>(command);
        return rows.Select(MapToDomain);
    }

    public async Task<IEnumerable<RecurringJob>> GetDueAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT * FROM RecurringJobs
            WHERE NextScheduledTime <= @CurrentTime
            ORDER BY NextScheduledTime ASC;";

        var command = new CommandDefinition(sql,
            new { CurrentTime = DateTime.UtcNow },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<RecurringJobRow>(command);
        return rows.Select(MapToDomain);
    }

    public async Task<RecurringJob?> GetNextScheduledRecurringJob(CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT * FROM RecurringJobs
            WHERE NextScheduledTime > @CurrentTime
            ORDER BY NextScheduledTime ASC
            LIMIT 1;";

        var command = new CommandDefinition(sql,
            new { CurrentTime = DateTime.UtcNow },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<RecurringJobRow>(command);
        return row is null ? null : MapToDomain(row);
    }

    public async Task<int> UpdateByIdAsync(RecurringJob recurringJob, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        UPDATE RecurringJobs
        SET
            JobId             = @JobId,
            NextScheduledID   = @NextScheduledID,
            CronExpression    = @CronExpression,
            StartTime         = @StartTime,
            IntervalTicks          = @IntervalTicks,
            NextScheduledTime = @NextScheduledTime,
            IsConcurrent      = @IsConcurrent,
            isCron            = @IsCron,
            ExecutingInstances= @ExecutingInstances,
            ExecutedInstances = @ExecutedInstances
        WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new
        {
            recurringJob.id,
            recurringJob.JobId,
            recurringJob.NextScheduledID,
            recurringJob.CronExpression,
            recurringJob.StartTime,
            IntervalTicks         = recurringJob.IntervalTicks,   // TimeSpan → BIGINT
            recurringJob.NextScheduledTime,
            recurringJob.IsConcurrent,
            IsCron = recurringJob.IsCron,
            recurringJob.ExecutingInstances,
            recurringJob.ExecutedInstances
        }, cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command);
    }

    
    public async Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        using MySqlConnection connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM RecurringJobs WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Flat projection that Dapper can auto-map directly from the DB row.
    /// IntervalVMs arrives as a raw long (ticks) which we then convert to TimeSpan.
    /// </summary>
    private sealed class RecurringJobRow
    {
        public long     Id                { get; init; }
        public long     JobId             { get; init; }
        public long?    NextScheduledID   { get; init; }
        public string   CronExpression    { get; init; } = string.Empty;
        public DateTime StartTime         { get; init; }
        public long?    IntervalTicks     { get; init; }  // ticks
        public DateTime NextScheduledTime { get; init; }
        public bool     IsConcurrent      { get; init; }
        public int ExecutedInstances {get; set; } = 0;
        public int ExecutingInstances {get; set; } = 0; 

        public bool IsCron {get; set; } = false;
    }

    private static RecurringJob MapToDomain(RecurringJobRow row) => new()
    {
        id = row.Id,
        JobId             = row.JobId,
        NextScheduledID   = row.NextScheduledID,
        CronExpression    = row.CronExpression,
        StartTime         = row.StartTime,
        IntervalTicks     = row.IntervalTicks,  // BIGINT → long?
        NextScheduledTime = row.NextScheduledTime,
        IsConcurrent      = row.IsConcurrent,
        IsCron = row.IsCron,
        ExecutedInstances = row.ExecutedInstances,
        ExecutingInstances = row.ExecutingInstances
    };
}