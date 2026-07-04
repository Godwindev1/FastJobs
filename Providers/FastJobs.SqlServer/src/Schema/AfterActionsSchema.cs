using System.Data;
using Dapper;
namespace FastJobs.Persistence;

public  class MSSQLAfterActionTableInitializer : ISchemaInitializer
{
        int ISchemaInitializer.Order => 6 ;

    private const string CreateTableSql = @"
IF OBJECT_ID('dbo.AfterActions', 'U') IS NULL
CREATE TABLE dbo.AfterActions
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    TypeName VARCHAR(500) NOT NULL,
    Retries INT NOT NULL DEFAULT 0,
    MaxRetries INT NOT NULL DEFAULT 3,
    JobId BIGINT NOT NULL,
    NextActionId BIGINT NOT NULL DEFAULT 0,
    LastActionId BIGINT NOT NULL DEFAULT 0,
    ChainNo BIGINT NOT NULL DEFAULT 0,
    Payload VARCHAR(MAX) NULL
);";

    private const string CreateIndexJobIdSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AfterActions_JobId' AND object_id = OBJECT_ID('dbo.AfterActions'))
CREATE INDEX IX_AfterActions_JobId
ON dbo.AfterActions (JobId);";

    private const string CreateIndexChainSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AfterActions_JobId_ChainNo' AND object_id = OBJECT_ID('dbo.AfterActions'))
CREATE INDEX IX_AfterActions_JobId_ChainNo
ON dbo.AfterActions (JobId, ChainNo);";

    private const string CreateIndexNextLastSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AfterActions_NextActionId_LastActionId' AND object_id = OBJECT_ID('dbo.AfterActions'))
CREATE INDEX IX_AfterActions_NextActionId_LastActionId
ON dbo.AfterActions (NextActionId, LastActionId);";

    public async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexJobIdSql);
        await connection.ExecuteAsync(CreateIndexChainSql);
        await connection.ExecuteAsync(CreateIndexNextLastSql);
    }
}