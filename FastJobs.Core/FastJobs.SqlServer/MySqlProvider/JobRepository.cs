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
    public async Task<long> InsertAsync(Job job)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO Jobs
        (TypeName, MethodName, MethodDeclaringTypeName, StateID, ParameterTypeNamesJson, ArgumentsJson,
        Queue, StateName, RetryCount, MaxRetries, CreatedAt, LeaseExpiresAt, LeaseOwner)
        VALUES
        (@TypeName, @MethodName, @MethodDeclaringTypeName,  @StateID, @ParameterTypeNamesJson, @ArgumentsJson,
        @Queue, @StateName, @RetryCount, @MaxRetries, @CreatedAt, @LeaseExpiresAt, @LeaseOwner);

        SELECT LAST_INSERT_ID();
        ";

        var id = await _connection.ExecuteScalarAsync<long>(sql, job);
        return id;

    }


    public async Task<Job?> GetByIdAsync(long id)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Jobs WHERE Id = @Id";

        return await _connection.QuerySingleOrDefaultAsync<Job>(
            new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<int> DeleteByIdAsync(long id)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        string sql  = $@"
            DELETE FROM Jobs WHERE Id = {id} 
        ";

        return await _connection.ExecuteAsync(sql);
    }

    /// <summary>
    /// Updates the Complete Job record  
    /// </summary>
    /// <param name="id"></param>
    /// <param name="job"></param>
    /// <returns> returns affected rows</returns>
    public async Task<int> UpdateByIdAsync(Job job)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        UPDATE Jobs
        SET 
            TypeName = @TypeName,
            MethodName = @MethodName,
            MethodDeclaringTypeName = @MethodDeclaringTypeName, 
            StateId = @StateID,
            ParameterTypeNamesJson = @ParameterTypeNamesJson,
            ArgumentsJson = @ArgumentsJson,
            Queue = @Queue,
            StateName = @StateName,
            RetryCount = @RetryCount,
            MaxRetries = @MaxRetries,
            CreatedAt = @CreatedAt,
            LeaseExpiresAt = @LeaseExpiresAt,
            LeaseOwner = @LeaseOwner 
        WHERE Id = @Id;";

        return await _connection.ExecuteAsync(sql, new
        {
            Id = job.Id,
            job.TypeName,
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
            job.LeaseExpiresAt,
            job.LeaseOwner
        });
    }

    /// <summary>
    /// Updates Select Fields Within a Job Record
    /// </summary>
    /// <param name="id">id of Job </param>
    /// <param name="SqlValues"> Values formatted By @value <dapper format> </param>
    /// <param name="job"></param>
    /// <returns></returns>
    public async Task<int> UpdateByIdAsync(long id, string SqlValues, Job job)
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        string sql = $@"
        UPDATE Jobs
        SET 
          {SqlValues}
        WHERE Id = {id}";

        return await _connection.ExecuteAsync(new CommandDefinition ( sql, job ));

    }

}