
using System.Data;
using Dapper;

public static class QueueTableInitializer
{
   private const string CreateTableSql = @"
CREATE TABLE IF NOT EXISTS Queue
(
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    QueueName VARCHAR(500) NOT NULL,
    ScheduledAt DATETIME(6) NOT NULL,
    JobId BIGINT NOT NULL,
    Priority INT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_Queue_Jobs
    FOREIGN KEY (JobId)
    REFERENCES Jobs(Id)
    ON DELETE CASCADE
) ENGINE=InnoDB;";

    private const string CreateIndexQueueSql = @"
    CREATE INDEX IF NOT EXISTS IX_Queue_QueueName_Priority_ScheduledAt
    ON Queue (QueueName, Priority DESC, ScheduledAt);";
    private const string CreateIndexJobIDSql = @"
        CREATE INDEX IF NOT EXISTS IX_Queue_JobId
    ON Queue (JobId);";

    
    public static async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexQueueSql);
        await connection.ExecuteAsync(CreateIndexJobIDSql);
    }
    
}