
using System.Data;
using Dapper;

namespace FastJobs.Persistence;
public class MSSQLQueueTableInitializer : ISchemaInitializer
{

    int ISchemaInitializer.Order => 1 ;

   private const string CreateTableSql = @"
IF OBJECT_ID('dbo.Queue', 'U') IS NULL
CREATE TABLE dbo.Queue
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    QueueName VARCHAR(500) NOT NULL,
    DequeuedAt DATETIME2(6) NOT NULL,
    JobId BIGINT NOT NULL,
    Priority INT NOT NULL DEFAULT 0,
    isDequeued BIT NOT NULL DEFAULT 0,
    IsMisfireRecovery BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Queue_Jobs FOREIGN KEY (JobId)
    REFERENCES dbo.Jobs(Id)
    ON DELETE CASCADE
);";

    private const string CreateIndexQueueSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Queue_QueueName_Priority_Scheduled' AND object_id = OBJECT_ID('dbo.Queue'))
CREATE INDEX IX_Queue_QueueName_Priority_Scheduled
ON dbo.Queue (QueueName, Priority DESC, isDequeued);";
    private const string CreateIndexJobIDSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Queue_JobId' AND object_id = OBJECT_ID('dbo.Queue'))
CREATE INDEX IX_Queue_JobId
ON dbo.Queue (JobId);";

    
    public async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexQueueSql);
        await connection.ExecuteAsync(CreateIndexJobIDSql);
    }
    
}