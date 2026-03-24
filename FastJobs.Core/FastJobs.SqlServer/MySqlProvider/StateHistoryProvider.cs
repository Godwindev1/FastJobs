using System.Data;
using Dapper;
using MySqlConnector;


namespace FastJobs.SqlServer;
internal sealed class StateHistoryRepository : IStateHistoryRepository
{
    
    private readonly DbConnectionFactory _connectionFactory;

    public StateHistoryRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> InsertAsync(State job, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO State
        (StateName, data, Reason, JobId, CreatedAt)
        VALUES
        (@StateName, @data, @Reason, @JobId, @CreatedAt);

        SELECT LAST_INSERT_ID()";
        
        var result = await _connection.ExecuteScalarAsync<long>(new CommandDefinition(sql, job, cancellationToken: cancellationToken));
        return result;   
    }

    public async Task InsertAsync(IEnumerable<State> states, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO State
            (StateName, Data, Reason, JobId, CreatedAt)
            VALUES
            (@StateName, @Data, @Reason, @JobId, @CreatedAt);";

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, states, cancellationToken: cancellationToken)
        );
    }
    public async Task<State?> GetByIdAsync(int id, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        string sql  = $@"
            SELECT * FROM State WHERE Id = {id}
        ";

        return await _connection.QuerySingleAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

}