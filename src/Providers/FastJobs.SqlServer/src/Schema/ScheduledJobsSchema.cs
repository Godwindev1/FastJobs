using System.Data;
using Dapper;
namespace FastJobs.Persistence;

public class MSSQLScheduledJobTableInitializer : ISchemaInitializer
{
        int ISchemaInitializer.Order => 2 ;

    private const string CreateTableSql = @"
IF OBJECT_ID('dbo.ScheduledJobs', 'U') IS NULL
CREATE TABLE dbo.ScheduledJobs
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobId BIGINT NOT NULL,
    ScheduledTo DATETIME2(6) NOT NULL,
    CONSTRAINT FK_ScheduledJobs_Jobs FOREIGN KEY (JobId)
        REFERENCES dbo.Jobs(Id)
        ON DELETE CASCADE
);";

    private const string CreateIndexScheduledToSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ScheduledJobs_ScheduledTo' AND object_id = OBJECT_ID('dbo.ScheduledJobs'))
CREATE INDEX IX_ScheduledJobs_ScheduledTo
ON dbo.ScheduledJobs (ScheduledTo);";

    private const string CreateIndexJobIdSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ScheduledJobs_JobId' AND object_id = OBJECT_ID('dbo.ScheduledJobs'))
CREATE INDEX IX_ScheduledJobs_JobId
ON dbo.ScheduledJobs (JobId);";

    public async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexScheduledToSql);
        await connection.ExecuteAsync(CreateIndexJobIdSql);
    }
}
