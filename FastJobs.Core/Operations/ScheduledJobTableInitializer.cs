using System.Data;
using Dapper;

public static class ScheduledJobTableInitializer
{
    private const string CreateTableSql = @"
CREATE TABLE IF NOT EXISTS ScheduledJobs
(
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    JobId BIGINT NOT NULL,
    ScheduledTo DATETIME(6) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    
    CONSTRAINT FK_ScheduledJobs_Jobs
    FOREIGN KEY (JobId)
    REFERENCES Jobs(Id)
    ON DELETE CASCADE
) ENGINE=InnoDB;";

    private const string CreateIndexScheduledToSql = @"
    CREATE INDEX IF NOT EXISTS IX_ScheduledJobs_ScheduledTo
    ON ScheduledJobs (ScheduledTo ASC);";

    private const string CreateIndexJobIdSql = @"
    CREATE INDEX IF NOT EXISTS IX_ScheduledJobs_JobId
    ON ScheduledJobs (JobId);";

    public static async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexScheduledToSql);
        await connection.ExecuteAsync(CreateIndexJobIdSql);
    }
}
