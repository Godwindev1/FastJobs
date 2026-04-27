using System.Data;
using Dapper;

public static class WorkerTableInitializer
{
    private const string CreateTableSql = @"
CREATE TABLE IF NOT EXISTS Workers
(
    Id             BIGINT AUTO_INCREMENT PRIMARY KEY,
    WorkerName     VARCHAR(500) NOT NULL,
    ThreadName     VARCHAR(500) NOT NULL,
    StartedAt      DATETIME(6)  NOT NULL,
    LastHeartbeat  DATETIME(6)  NOT NULL,
    isSleeping     BIT          NOT NULL DEFAULT 0,
    isCrashed BIT          NOT NULL DEFAULT 0
) ENGINE=InnoDB;";

    private const string CreateIndexSleepingSql = @"
CREATE INDEX IF NOT EXISTS IX_Workers_isSleeping
ON Workers (isSleeping);";

    private const string CreateIndexCrashedSql = @"
CREATE INDEX IF NOT EXISTS IX_Workers_isCrashed
ON Workers (isCrashed);";

    public static async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexSleepingSql);
        await connection.ExecuteAsync(CreateIndexCrashedSql);
    }
}