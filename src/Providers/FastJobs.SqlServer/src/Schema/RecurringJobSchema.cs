using System.Data;
using Dapper;
 
namespace FastJobs.Persistence;


public class MSSQLRecurringJobTableInitializer : ISchemaInitializer
{

        int ISchemaInitializer.Order => 4 ;

    //   IntervalTicks -- stored as TimeSpan.Ticks
    private const string CreateTableSql = @"
IF OBJECT_ID('dbo.RecurringJobs', 'U') IS NULL
CREATE TABLE dbo.RecurringJobs
(
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    JobId BIGINT NOT NULL,
    NextScheduledID BIGINT NULL,
    CronExpression VARCHAR(120) NULL,
    StartTime DATETIME2(6) NOT NULL,
    IntervalTicks BIGINT NULL,
    NextScheduledTime DATETIME2(6) NOT NULL,
    IsConcurrent TINYINT NOT NULL DEFAULT 1,
    IsCron TINYINT NOT NULL DEFAULT 0,
    ExecutingInstances INT NOT NULL DEFAULT 0,
    ExecutedInstances INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_RecurringJobs_Jobs FOREIGN KEY (JobId)
        REFERENCES dbo.Jobs(Id)
        ON DELETE CASCADE
);";

    private const string CreateIndexJobIdSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecurringJobs_JobId' AND object_id = OBJECT_ID('dbo.RecurringJobs'))
CREATE INDEX IX_RecurringJobs_JobId
ON dbo.RecurringJobs (JobId);";

    private const string CreateIndexNextScheduledTimeSql = @"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecurringJobs_NextScheduledTime' AND object_id = OBJECT_ID('dbo.RecurringJobs'))
CREATE INDEX IX_RecurringJobs_NextScheduledTime
ON dbo.RecurringJobs (NextScheduledTime);";

    public async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexJobIdSql);
        await connection.ExecuteAsync(CreateIndexNextScheduledTimeSql);
    }
}