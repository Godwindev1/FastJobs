using System.Data;
using Dapper;

namespace FastJobs;

public static class JobTableInitializer
{
    private const string CreateTableSql = @"
    CREATE TABLE IF NOT EXISTS Jobs
    (
        Id BIGINT AUTO_INCREMENT PRIMARY KEY,

        AfterActionId BIGINT NULL,

        JobType VARCHAR(50) NOT NULL DEFAULT '',

        TypeName VARCHAR(500) NOT NULL,
        MethodName VARCHAR(200) NOT NULL,

        MethodDeclaringTypeName VARCHAR(200),

        ParameterTypeNamesJson LONGTEXT NOT NULL,
        ArgumentsJson LONGTEXT NOT NULL,

        Queue VARCHAR(100) NOT NULL,

        StateName VARCHAR(50) NOT NULL,

        RetryCount INT NOT NULL DEFAULT 0,
        MaxRetries INT NOT NULL DEFAULT 3,

        Priority INT NOT NULL DEFAULT 0,

        CreatedAt DATETIME(6) NOT NULL,
        ExpiresAt DATETIME(6) NULL,


        StateId BIGINT NOT NULL

    ) ENGINE=InnoDB;";


private const string CreateIndexQueueSql = @"
    CREATE INDEX IF NOT EXISTS IX_Jobs_Queue_State_Priority_CreatedAt
    ON Jobs (Queue, StateName, Priority DESC, CreatedAt);";
private const string CreateIndexStateSql = @"
    CREATE INDEX IF NOT EXISTS IX_Jobs_StateName_StateID
    ON Jobs (StateName, StateID);";
private const string CreateIndexExpiresSql = @"
    CREATE INDEX IF NOT EXISTS IX_Jobs_JobType
    ON Jobs (JobType);";
    
    public static async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexQueueSql);
        await connection.ExecuteAsync(CreateIndexStateSql);
        await connection.ExecuteAsync(CreateIndexExpiresSql);
    }
}