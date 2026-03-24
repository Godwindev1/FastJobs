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

    public async Task<long> EnqueueAsync(Queue jobEntry, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO Queue
        (QueueName, JobId, Priority, ScheduledAt, IsScheduled)
        VALUES
        (@QueueName, @JobId, @priority, @ScheduledAt, @IsScheduled);

        SELECT LAST_INSERT_ID()";

        var command = new CommandDefinition(sql, new
        {
            QueueName = jobEntry.QueueName,
            JobId = jobEntry.JobId,
            Priority = jobEntry.Priority,
            ScheduledAt = jobEntry.ScheduledAt,
            IsScheduled = jobEntry.IsScheduled
        }, cancellationToken: cancellationToken);

        return await _connection.ExecuteScalarAsync<long>(command);
    }

    public async Task<Queue?> GetQueueEntry(long id, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @" 
            SELECT * FROM Queue WHERE Id = @Id;
        ";  

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var value  = await _connection.QueryAsync<Queue>(command);
        return value.FirstOrDefault();
    }
    public async Task<bool> RemoveAsync(long id, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        DELETE FROM Queue
        WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var rows = await _connection.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<int> Update(Queue queueEntry, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        UPDATE Queue
        SET 
            QueueName = @QueueName,
            IsScheduled = @IsScheduled,
            ScheduledAt = @ScheduledAt, 
            Priority = @Priority,
            JobId = @JobId
        WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new
        {
            Id = queueEntry.Id,
            queueEntry.QueueName,
            queueEntry.IsScheduled,
            queueEntry.ScheduledAt,
            queueEntry.Priority,
            queueEntry.JobId,
        }, cancellationToken: cancellationToken);

        return await _connection.ExecuteAsync(command);

    }

    public async Task<Queue?> Dequeue(string queueName, CancellationToken cancellationToken )
    {
        using MySqlConnection _connection = (MySqlConnection)_connectionFactory.CreateConnection();

        using var transaction = _connection.BeginTransaction();

        try
        {
            string sql = @"
                SELECT *
                FROM Queue
                WHERE QueueName = @QueueName 
                AND IsScheduled = false 
                ORDER BY Priority DESC, Id ASC
                LIMIT 1
            ";

            CommandDefinition command = new CommandDefinition(sql, new { QueueName = queueName }, transaction: transaction, cancellationToken: cancellationToken);

            var job = await _connection.QueryFirstOrDefaultAsync<Queue>(
             command
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