using System.Data;
using Dapper;

namespace FastJobs.Persistence;

public class MSSQLJobTableInitializer : ISchemaInitializer
{
    int ISchemaInitializer.Order => 0 ;
    private const string CreateTableSql = @"
IF OBJECT_ID('dbo.Jobs', 'U') IS NULL
CREATE TABLE dbo.Jobs
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    AfterActionId BIGINT NULL,
    JobType VARCHAR(50) NOT NULL DEFAULT '',
    TypeName VARCHAR(500) NOT NULL,
    MethodName VARCHAR(200) NOT NULL,
    MethodDeclaringTypeName VARCHAR(200) NULL,
    ParameterTypeNamesJson VARCHAR(MAX) NOT NULL,
    ArgumentsJson VARCHAR(MAX) NOT NULL,
    Queue VARCHAR(100) NOT NULL,
    StateName VARCHAR(50) NOT NULL,
    RetryCount INT NOT NULL DEFAULT 0,
    MaxRetries INT NOT NULL DEFAULT 3,
    Priority INT NOT NULL DEFAULT 0,
    misfirePolicy INT NOT NULL DEFAULT 0,
    ScheduledRunAt DATETIME2(6) NULL,
    CreatedAt DATETIME2(6) NOT NULL,
    ExpiresAt DATETIME2(6) NULL,
    StateId BIGINT NOT NULL
);";


private const string CreateIndexQueueSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Jobs_Queue_State_Priority_CreatedAt' AND object_id = OBJECT_ID('dbo.Jobs'))
CREATE INDEX IX_Jobs_Queue_State_Priority_CreatedAt
ON dbo.Jobs (Queue, StateName, Priority DESC, CreatedAt);";
private const string CreateIndexStateSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Jobs_StateName_StateID' AND object_id = OBJECT_ID('dbo.Jobs'))
CREATE INDEX IX_Jobs_StateName_StateID
ON dbo.Jobs (StateName, StateId);";
private const string CreateIndexExpiresSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Jobs_JobType' AND object_id = OBJECT_ID('dbo.Jobs'))
CREATE INDEX IX_Jobs_JobType
ON dbo.Jobs (JobType);";
    
    public async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexQueueSql);
        await connection.ExecuteAsync(CreateIndexStateSql);
        await connection.ExecuteAsync(CreateIndexExpiresSql);
    }
}