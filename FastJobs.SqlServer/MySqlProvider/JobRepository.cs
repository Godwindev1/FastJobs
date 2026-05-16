using System.Data;
using System.Linq.Expressions;
using FastJobs;
using Dapper;
using System.Security.Cryptography;
using MySqlConnector;

namespace FastJobs.SqlServer;
internal sealed class JobRepository : IJobRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    //TODO Use Connection String Instead To Allow Multiple Threads Use 
    public JobRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }


    /// <summary>
    /// Inserts New Job To Jobs persistence Store
    /// </summary>
    /// <param name="job"></param>
    /// <returns>Returns Id of inserted Job</returns>
    public async Task<long> InsertAsync(Job job, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO Jobs
        (AfterActionId, TypeName, JobType, MethodName, MethodDeclaringTypeName, StateID, ParameterTypeNamesJson, ArgumentsJson,
        Queue, StateName, RetryCount, MaxRetries, CreatedAt)
        VALUES
        (@AfterActionId, @TypeName,@JobType, @MethodName, @MethodDeclaringTypeName,  @StateID, @ParameterTypeNamesJson, @ArgumentsJson,
        @Queue, @StateName, @RetryCount, @MaxRetries, @CreatedAt);

        SELECT LAST_INSERT_ID();
        ";

        var id = await _connection.ExecuteScalarAsync<long>(new CommandDefinition (sql, job, cancellationToken: cancellationToken));
        return id;
        

    }


    public async Task<List<Job>> GetAllAsync(CancellationToken cancellationToken)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Jobs ORDER BY CreatedAt DESC;"; //WHERE DeletedAt IS NULL

        var result = await _connection.QueryAsync<Job>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return result.ToList();
    }
    public async Task<Job?> GetByIdAsync(long id, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Jobs WHERE Id = @Id";

        return await _connection.QuerySingleOrDefaultAsync<Job>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<int> DeleteByIdAsync(long id, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        string sql  = $@"
            DELETE FROM Jobs WHERE Id = {id} 
        ";

        return await _connection.ExecuteAsync(new CommandDefinition (sql, cancellationToken: cancellationToken) );
    }

    /// <summary>
    /// Updates the Complete Job record  
    /// </summary>
    /// <param name="id"></param>
    /// <param name="job"></param>
    /// <returns> returns affected rows</returns>
    public async Task<int> UpdateByIdAsync(Job job, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

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
            CreatedAt = @CreatedAt
        WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new
        {
            Id = job.Id,
            job.AfterActionId,
            job.TypeName,
            job.JobType,
            job.MethodName,
            job.MethodDeclaringTypeName,
            job.stateID,
            job.ParameterTypeNamesJson,
            job.ArgumentsJson,
            job.Queue,
            job.StateName,
            job.RetryCount,
            job.MaxRetries,
            job.CreatedAt, 
        }, cancellationToken: cancellationToken);

        return await _connection.ExecuteAsync(command);
    }

    /// <summary>
    /// Updates Select Fields Within a Job Record
    /// </summary>
    /// <param name="id">id of Job </param>
    /// <param name="SqlValues"> Values formatted By @value <dapper format> </param>
    /// <param name="job"></param>
    /// <returns></returns>
    public async Task<int> UpdateByIdAsync(long id, string SqlValues, Job job, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        string sql = $@"
        UPDATE Jobs
        SET 
          {SqlValues}
        WHERE Id = {id}";

        return await _connection.ExecuteAsync(new CommandDefinition(sql, job, cancellationToken: cancellationToken));

    }

    public async Task<int> CountByStateAsync(string stateName, CancellationToken cancellationToken = default)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM Jobs 
            WHERE StateName = @StateName 
           "; // AND DeletedAt IS NULL

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = stateName }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountRetryingAsync(CancellationToken cancellationToken = default)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM Jobs 
            WHERE RetryCount > 1 
            AND StateName = @StateName
           "; // AND DeletedAt IS NULL

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = QueueStateTypes.Processing }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountCompletedSinceAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM Jobs
            WHERE StateName = @StateName
            AND CreatedAt >= @Since
           ";// AND DeletedAt IS NULL

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = QueueStateTypes.Completed, Since = since }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountFailedSinceAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM Jobs
            WHERE StateName = @StateName
            AND CreatedAt >= @Since
            ";// AND DeletedAt IS NULL

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = QueueStateTypes.Failed, Since = since }, cancellationToken: cancellationToken));
    }

    public async Task<int> CountStateBetween( string statename, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            SELECT COUNT(*) FROM Jobs
            WHERE StateName = @StateName
            AND CreatedAt >= @From
            AND CreatedAt <= @To
           "; //AND DeletedAt IS NULL

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { StateName = statename, From = from, To = to }, cancellationToken: cancellationToken));
    }

}