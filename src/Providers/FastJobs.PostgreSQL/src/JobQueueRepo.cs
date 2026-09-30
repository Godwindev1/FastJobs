using Dapper;
using Npgsql;

namespace FastJobs.Persistence;

internal sealed class QueueRepository : IQueueRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public QueueRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> EnqueueAsync(Queue jobEntry, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        INSERT INTO Queue
        (QueueName, JobId, Priority, DequeuedAt, isDequeued, IsMisfireRecovery)
        VALUES
        (@QueueName, @JobId, @Priority, @DequeuedAt, @isDequeued, @IsMisfireRecovery)
        RETURNING Id;";

        var command = new CommandDefinition(sql, new
        {
            jobEntry.QueueName,
            jobEntry.JobId,
            jobEntry.Priority,
            jobEntry.DequeuedAt,
            jobEntry.isDequeued,
            jobEntry.IsMisfireRecovery
        }, cancellationToken: cancellationToken);

        return await _connection.ExecuteScalarAsync<long>(command);
    }

    public async Task<Queue?> GetQueueEntry(long id, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Queue WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var value = await _connection.QueryAsync<Queue>(command);
        return value.FirstOrDefault();
    }

    public async Task<List<Queue>> GetAllQueueEntries(CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Queue;";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var value = await _connection.QueryAsync<Queue>(command);
        return value.ToList();
    }

    public async Task<bool> ExistsAny(CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT EXISTS (SELECT 1 FROM Queue)";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return await _connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<bool> RemoveAsync(long id, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "DELETE FROM Queue WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var rows = await _connection.ExecuteAsync(command);
        return rows > 0;
    }

    public async Task<int> Update(Queue queueEntry, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = @"
        UPDATE Queue
        SET
            QueueName = @QueueName,
            isDequeued = @isDequeued,
            DequeuedAt = @DequeuedAt,
            Priority = @Priority,
            JobId = @JobId,
            IsMisfireRecovery = @IsMisfireRecovery
        WHERE Id = @Id;";

        var command = new CommandDefinition(sql, new
        {
            Id = queueEntry.Id,
            queueEntry.QueueName,
            queueEntry.isDequeued,
            queueEntry.DequeuedAt,
            queueEntry.Priority,
            queueEntry.JobId,
            queueEntry.IsMisfireRecovery
        }, cancellationToken: cancellationToken);

        return await _connection.ExecuteAsync(command);
    }

    public async Task<Queue?> Dequeue(string queueName, CancellationToken cancellationToken)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        using var transaction = _connection.BeginTransaction();

        try
        {
            // SKIP LOCKED lets concurrent workers each claim a different row instead of blocking on the same one.
            const string sqlSelect = @"
                SELECT *
                FROM Queue
                WHERE QueueName = @_queuename
                AND isDequeued = FALSE
                ORDER BY Priority DESC, Id ASC
                LIMIT 1
                FOR UPDATE SKIP LOCKED";

            var selectCommand = new CommandDefinition(sqlSelect, new { _queuename = queueName }, transaction: transaction, cancellationToken: cancellationToken);
            var job = await _connection.QueryFirstOrDefaultAsync<Queue>(selectCommand);

            if (job != null)
            {
                const string sqlUpdate = "UPDATE Queue SET isDequeued = TRUE WHERE Id = @Id";
                var updateCommand = new CommandDefinition(sqlUpdate, new { Id = job.Id }, transaction: transaction, cancellationToken: cancellationToken);
                await _connection.ExecuteAsync(updateCommand);
            }

            transaction.Commit();
            return job;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Queue?> GetByJob(long id, CancellationToken cancellationToken = default)
    {
        using NpgsqlConnection _connection = (NpgsqlConnection)_connectionFactory.CreateConnection();

        const string sql = "SELECT * FROM Queue WHERE JobId = @JobId;";

        var command = new CommandDefinition(sql, new { JobId = id }, cancellationToken: cancellationToken);
        var value = await _connection.QueryAsync<Queue>(command);
        return value.FirstOrDefault();
    }
}
