using Dapper;
using FastJobs;
using MySqlConnector;
using System.Data;

namespace FastJobs.SqlServer;
internal sealed class QueueRepository : IQueueRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public QueueRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> EnqueueAsync(Queue jobEntry)
    {
        MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO Queue
        (QueueName, JobId, Priority, ScheduledAt)
        VALUES
        (@QueueName, @JobId, @priority, @ScheduledAt);

        SELECT LAST_INSERT_ID()";

        return await _connection.ExecuteScalarAsync<long>(sql, new
        {
            QueueName = jobEntry.QueueName,
            JobId = jobEntry.JobId,
            Priority = jobEntry.Priority,
            ScheduledAt = jobEntry.ScheduledAt
        });
    }

    public async Task<Queue?> GetQueueEntry(long id)
    {
        MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @" 
            SELECT * FROM Queue WHERE Id = @Id;
        ";  

        var value  = await _connection.QueryAsync<Queue>(sql, new { Id = id });
        return value.FirstOrDefault();
    }
    public async Task<bool> RemoveAsync(long id)
    {
        MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        DELETE FROM Queue
        WHERE Id = @Id;";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

    public async Task<Queue?> Dequeue(string queueName)
    {
        MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        using var transaction = _connection.BeginTransaction();

        try
        {
            string sql = @"
                SELECT *
                FROM Queue
                WHERE QueueName = @QueueName
                ORDER BY Priority DESC, Id ASC
                LIMIT 1
            ";

            var job = await _connection.QueryFirstOrDefaultAsync<Queue>(
                sql,
                new { QueueName = queueName },
                transaction: transaction
            );

            transaction.Commit();
            return job;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }


}