using System.Data;
using Dapper;

namespace FastJobs.SqlServer;

public static class RecurringJobTableInitializer
{
    //   IntervalVMs -- stored as TimeSpan.Ticks
    private const string CreateTableSql = @"
    CREATE TABLE IF NOT EXISTS RecurringJobs
    (
        Id               BIGINT AUTO_INCREMENT PRIMARY KEY,
        JobId            BIGINT NOT NULL,
        NextScheduledID  BIGINT NULL,
        CronExpression   VARCHAR(120) NOT NULL,
        StartTime        DATETIME(6) NOT NULL,
        IntervalVMs         BIGINT NOT NULL,     
        NextScheduledTime DATETIME(6) NOT NULL,
        IsConcurrent     TINYINT(1) NOT NULL DEFAULT 1,

        CONSTRAINT FK_RecurringJobs_Jobs
            FOREIGN KEY (JobId)
            REFERENCES Jobs(Id)
            ON DELETE CASCADE       
    ) ENGINE=InnoDB;";

    private const string CreateIndexJobIdSql = @"
    CREATE INDEX IF NOT EXISTS IX_RecurringJobs_JobId
    ON RecurringJobs (JobId);";

    private const string CreateIndexNextScheduledTimeSql = @"
    CREATE INDEX IF NOT EXISTS IX_RecurringJobs_NextScheduledTime
    ON RecurringJobs (NextScheduledTime ASC);";

    public static async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexJobIdSql);
        await connection.ExecuteAsync(CreateIndexNextScheduledTimeSql);
    }
}