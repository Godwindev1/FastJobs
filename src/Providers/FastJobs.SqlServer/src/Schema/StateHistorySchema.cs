
using System.Data;
using Dapper;

namespace FastJobs.Persistence;

public class MSSQLStateHistoryTableInitialization : ISchemaInitializer
{
        int ISchemaInitializer.Order => 3 ;

     private const string CreateTableSql = @"
IF OBJECT_ID('dbo.State', 'U') IS NULL
CREATE TABLE dbo.State
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    StateName VARCHAR(500) NOT NULL,
    Reason VARCHAR(500) NOT NULL,
    data VARCHAR(500) NOT NULL,
    CreatedAt DATETIME2(6) NOT NULL,
    DeletedAt DATETIME2(6) NULL,
    JobId BIGINT NOT NULL,
    CONSTRAINT FK_State_Jobs FOREIGN KEY (JobId) REFERENCES dbo.Jobs(Id) ON DELETE CASCADE
);";

    private const string CreateIndexQueueSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_State_StateName' AND object_id = OBJECT_ID('dbo.State'))
CREATE INDEX IX_State_StateName
ON dbo.State (StateName);";
    private const string CreateIndexJobIDSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_State_JobId' AND object_id = OBJECT_ID('dbo.State'))
CREATE INDEX IX_State_JobId
ON dbo.State (JobId);";

    public async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexQueueSql);
        await connection.ExecuteAsync(CreateIndexJobIDSql);
    }
       
}