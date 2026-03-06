using System.Data;
using Dapper;


namespace FastJobs.SqlServer;
internal sealed class StateHistoryRepository : IStateHistoryRepository, IDisposable
{
    
    private readonly IDbConnection _connection;

    internal  StateHistoryRepository(DbConnectionFactory connectionFactory)
    {
        _connection = connectionFactory.CreateConnection();
    }

    public async Task<long> InsertAsync(State job)
    {
        const string sql = @"
        INSERT INTO State
        (StateName, data, Reason, JobId, CreatedAt)
        VALUES
        (@StateName, @data, @Reason, @JobId, @CreatedAt);

        SELECT LAST_INSERT_ID()";
        
        var result = await _connection.ExecuteScalarAsync<long>( new CommandDefinition (sql, job) );
        return result;   
    }

    public async Task InsertAsync(IEnumerable<State> states)
    {
        const string sql = @"
            INSERT INTO State
            (StateName, Data, Reason, JobId, CreatedAt)
            VALUES
            (@StateName, @Data, @Reason, @JobId, @CreatedAt);";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, states)
        );
    }
    public async Task<State?> GetByIdAsync(int id)
    {
        string sql  = $@"
            SELECT * FROM State WHERE Id = {id}
        ";

        return await _connection.QuerySingleAsync(sql);
    }

    public void Dispose()
    {
       _connection.Dispose(); 
    }
}