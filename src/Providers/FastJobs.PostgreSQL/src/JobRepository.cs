using System.Data;
using Dapper;
using Npgsql;

namespace FastJobs.Persistence;

internal sealed class JobRepository : IJobRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public JobRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Inserts New Job To Jobs persistence Store
    /// </summary>
    /// <returns>Returns Id of inserted Job</returns>
    public async Task<long> InsertAsync(Job job, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO Jobs
        (AfterActionId, TypeName, JobType, MethodName, MethodDeclaringTypeName, StateID, ParameterTypeNamesJson, ArgumentsJson,
        Queue, StateName, RetryCount, MaxRetries, misfirePolicy, CreatedAt, ScheduledRunAt, ExpiresAt)
        VALUES
        (@AfterActionId, @TypeName, @JobType, @MethodName, @MethodDeclaringTypeName, @StateID, @ParameterTypeNamesJson, @ArgumentsJson,
        @Queue, @StateName, @RetryCount, @MaxRetries, @misfirePolicy, @CreatedAt, @ScheduledRunAt, @ExpiresAt)
        RETURNING Id;";

        return await _connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, job, cancellationToken: cancellationToken));
    }

    public async Task<List<Job>> GetAllAsync(CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Jobs ORDER BY CreatedAt DESC;";

        var result = await _connection.QueryAsync<Job>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return result.ToList();
    }

    public async Task<Job?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Jobs WHERE Id = @Id";

        return await _connection.QuerySingleOrDefaultAsync<Job>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM Jobs WHERE Id = @Id";

        return await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Updates the Complete Job record
    /// </summary>
    /// <returns> returns affected rows</returns>
    public async Task<int> UpdateByIdAsync(Job job, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        UPDATE Jobs
        SET
            AfterActionId = @AfterActionId,
            TypeName = @TypeName,
            JobType = @JobType,
            MethodName = @MethodName,
            MethodDeclaringTypeName = @MethodDeclaringTypeName,
            StateId = @StateID,
            ParameterTypeNamesJson = @ParameterTypeNamesJson,
            ArgumentsJson = @ArgumentsJson,
            Queue = @Queue,
            StateName = @StateName,
            RetryCount = @RetryCount,
            MaxRetries = @MaxRetries,
            misfirePolicy = @misfirePolicy,
            CreatedAt = @CreatedAt,
            ScheduledRunAt = @ScheduledRunAt,
            ExpiresAt = @ExpiresAt
        WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new
        {
            Id = job.Id,
            job.AfterActionId,
            job.TypeName,
            job.JobType,
            job.MethodName,
            job.MethodDeclaringTypeName,
            StateID = job.stateID,
            job.ParameterTypeNamesJson,
            job.ArgumentsJson,
            job.Queue,
            job.StateName,
            job.RetryCount,
            job.MaxRetries,
            job.misfirePolicy,
            job.CreatedAt,
            job.ScheduledRunAt,
            job.ExpiresAt
        }, cancellationToken: cancellationToken);

        return await _connection.ExecuteAsync(command);
    }

    /// <summary>
    /// Updates Select Fields Within a Job Record
    /// </summary>
    /// <param name="id">id of Job </param>
    /// <param name="SqlValues"> Values formatted By @value (dapper format) </param>
    public async Task<int> UpdateByIdAsync(long id, string SqlValues, Job job, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        string sql = $@"
        UPDATE Jobs
        SET
          {SqlValues}
        WHERE Id = {id}";

        return await _connection.ExecuteAsync(new CommandDefinition(sql, job, cancellationToken: cancellationToken));
    }

    public async Task<int> CountByStateAsync(string stateName, CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT COUNT(*) FROM Jobs WHERE StateName = @StateName";

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = stateName }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountRetryingAsync(CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM Jobs
            WHERE RetryCount > 1
            AND StateName = @StateName";

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = QueueStateTypes.Processing }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountCompletedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM Jobs
            WHERE StateName = @StateName
            AND CreatedAt >= @Since";

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = QueueStateTypes.Completed, Since = since }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountFailedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM Jobs
            WHERE StateName = @StateName
            AND CreatedAt >= @Since";

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = QueueStateTypes.Failed, Since = since }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountStateBetween(string statename, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM Jobs
            WHERE StateName = @StateName
            AND CreatedAt >= @From
            AND CreatedAt <= @To";

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = statename, From = from, To = to }, cancellationToken: cancellationToken));
    }

    public async Task<List<Job>> GetMisfiredJobsAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        //Misfired Job Cannot be Completed or Failed
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT * FROM Jobs
            WHERE ScheduledRunAt <= @Cutoff
            AND JobType = @JobType
            AND StateName IN (@ProcessingState, @DequeuedState)";

        var result = await _connection.QueryAsync<Job>(
            new CommandDefinition(sql, new { Cutoff = cutoff, JobType = JobTypes.Recurring, ProcessingState = QueueStateTypes.Processing, DequeuedState = QueueStateTypes.Dequeued }, cancellationToken: ct));

        return result.ToList();
    }

    //Return Count of jobs deleted
    public async Task<int> PruneCompletedJobs(CancellationToken ct = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM Jobs WHERE StateName = @CompletedState";

        return await _connection.ExecuteAsync(
            new CommandDefinition(sql, new { CompletedState = QueueStateTypes.Completed }, cancellationToken: ct));
    }

    //Return Count of jobs deleted
    public async Task<int> PruneExpiredJobs(CancellationToken ct = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM Jobs WHERE NOW() > ExpiresAt;";

        return await _connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
    }
}
