using System.Data;
using Dapper;

namespace FastJobs;

public static class JobTableInitializer
{
    private const string CreateTableSql = @"
    CREATE TABLE IF NOT EXISTS Jobs
    (
        Id BIGINT AUTO_INCREMENT PRIMARY KEY,

        TypeName VARCHAR(500) NOT NULL,
        MethodName VARCHAR(200) NOT NULL,

        MethodDeclaringTypeName VARCHAR(200),

        ParameterTypeNamesJson LONGTEXT NOT NULL,
        ArgumentsJson LONGTEXT NOT NULL,

        Queue VARCHAR(100) NOT NULL,

        StateId INT NOT NULL,
        StateName VARCHAR(50) NOT NULL,

        RetryCount INT NOT NULL DEFAULT 0,
        MaxRetries INT NOT NULL DEFAULT 3,

        Priority INT NOT NULL DEFAULT 0,

        CreatedAt DATETIME(6) NOT NULL,
        ExpiresAt DATETIME(6) NULL
    ) ENGINE=InnoDB;";

private const string CreateIndexQueueSql = @"
    CREATE INDEX IF NOT EXISTS IX_Jobs_Queue_State_Priority_CreatedAt
    ON Jobs (Queue, StateName, Priority DESC, CreatedAt);";
private const string CreateIndexStateSql = @"
    CREATE INDEX IF NOT EXISTS IX_Jobs_StateName
    ON Jobs (StateName);";
private const string CreateIndexExpiresSql = @"
    CREATE INDEX IF NOT EXISTS IX_Jobs_ExpiresAt
    ON Jobs (ExpiresAt);";
    
    public static async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexQueueSql);
        await connection.ExecuteAsync(CreateIndexStateSql);
    }
}