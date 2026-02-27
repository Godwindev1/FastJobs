using Dapper;
using FastJobs;
using System.Data;

public class QueueRepository : IQueueRepository
{
    private readonly IDbConnection _connection;

    public QueueRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<long> EnqueueAsync(Queue job)
    {
        const string sql = @"
        INSERT INTO Queue
        (QueueName, JobId, ScheduledAt)
        VALUES
        (@QueueName, @JobId, @ScheduledAt);

        SELECT LAST_INSERT_ID()";

        return await _connection.ExecuteScalarAsync<long>(sql, new
        {
            job.QueueName,
            job.JobId,
            ScheduledAt = job.ScheduledAt
        });
    }

    public async Task<Queue?> GetQueueEntry(long id)
    {
        const string sql = @" 
            SELECT * FROM Queue WHERE Id = @Id;
        ";  

        var value  = await _connection.QueryAsync<Queue>(sql, new { Id = id });
        return value.FirstOrDefault();
    }
    public async Task<bool> RemoveAsync(long id)
    {
        const string sql = @"
        DELETE FROM Queue
        WHERE Id = @Id;";

        var rows = await _connection.ExecuteAsync(sql, new { Id = id });
        return rows > 0;
    }

}