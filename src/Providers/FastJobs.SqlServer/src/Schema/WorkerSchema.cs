using System.Data;
using Dapper;

namespace FastJobs.Persistence;
public  class MSSQLWorkerTableInitializer : ISchemaInitializer
{

        int ISchemaInitializer.Order => 5 ;

    private const string CreateTableSql = @"
IF OBJECT_ID('dbo.Workers', 'U') IS NULL
CREATE TABLE dbo.Workers
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    WorkerName VARCHAR(500) NOT NULL,
    ThreadName VARCHAR(500) NOT NULL,
    StartedAt DATETIME2(6) NOT NULL,
    LastHeartbeat DATETIME2(6) NOT NULL,
    isSleeping BIT NOT NULL DEFAULT 0,
    isCrashed BIT NOT NULL DEFAULT 0
);";

    private const string CreateIndexSleepingSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Workers_isSleeping' AND object_id = OBJECT_ID('dbo.Workers'))
CREATE INDEX IX_Workers_isSleeping
ON dbo.Workers (isSleeping);";

    private const string CreateIndexCrashedSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Workers_isCrashed' AND object_id = OBJECT_ID('dbo.Workers'))
CREATE INDEX IX_Workers_isCrashed
ON dbo.Workers (isCrashed);";

    public async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexSleepingSql);
        await connection.ExecuteAsync(CreateIndexCrashedSql);
    }
}