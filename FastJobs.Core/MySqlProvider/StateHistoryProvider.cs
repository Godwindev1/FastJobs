using System.Data;
using Dapper;
namespace FastJobs;

public class StateHistoryRepository : IStateHistoryRepository
{
    
    private readonly IDbConnection _connection;

    public StateHistoryRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<long> InsertAsync(State job, CancellationToken ct)
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
    public async Task<State?> GetByIdAsync(int id, CancellationToken ct)
    {
        string sql  = $@"
            SELECT * FROM State WHERE Id = {id}
        ";

        return await _connection.QuerySingleAsync(sql);
    }

}