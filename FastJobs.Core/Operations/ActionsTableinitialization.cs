using System.Data;
using Dapper;

namespace FastJobs;

public static class AfterActionTableInitializer
{
    private const string CreateTableSql = @"
    CREATE TABLE IF NOT EXISTS AfterActions
    (
        Id          BIGINT AUTO_INCREMENT PRIMARY KEY,

        TypeName    VARCHAR(500) NOT NULL,

        Retries     INT NOT NULL DEFAULT 0,
        MaxRetries  INT NOT NULL DEFAULT 3,

        JobId       BIGINT NOT NULL,

        NextActionId BIGINT NOT NULL DEFAULT 0,
        LastActionId BIGINT NOT NULL DEFAULT 0,

        ChainNo     BIGINT NOT NULL DEFAULT 0
        
    ) ENGINE=InnoDB;";

    private const string CreateIndexJobIdSql = @"
    CREATE INDEX IF NOT EXISTS IX_AfterActions_JobId
    ON AfterActions (JobId);";

    private const string CreateIndexChainSql = @"
    CREATE INDEX IF NOT EXISTS IX_AfterActions_JobId_ChainNo
    ON AfterActions (JobId, ChainNo);";

    private const string CreateIndexNextLastSql = @"
    CREATE INDEX IF NOT EXISTS IX_AfterActions_NextActionId_LastActionId
    ON AfterActions (NextActionId, LastActionId);";

    public static async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexJobIdSql);
        await connection.ExecuteAsync(CreateIndexChainSql);
        await connection.ExecuteAsync(CreateIndexNextLastSql);
    }
}