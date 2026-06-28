using System.Data;
using Dapper;
namespace FastJobs.Persistence;

public class MariaDBScheduledJobTableInitializer : ISchemaInitializer
{
        int ISchemaInitializer.Order => 2 ;

    private const string CreateTableSql = @"
    CREATE TABLE IF NOT EXISTS ScheduledJobs
    (
        Id BIGINT AUTO_INCREMENT PRIMARY KEY,
        JobId BIGINT NOT NULL,
        ScheduledTo DATETIME(6) NOT NULL,
        
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

    public async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexScheduledToSql);
        await connection.ExecuteAsync(CreateIndexJobIdSql);
    }
}
