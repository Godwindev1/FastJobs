
using System.Data;
using Dapper;

namespace FastJobs.Persistence;

public class MariaDBStateHistoryTableInitialization : ISchemaInitializer
{
        int ISchemaInitializer.Order => 3 ;

     private const string CreateTableSql = @"
    CREATE TABLE IF NOT EXISTS State
    (
        Id BIGINT AUTO_INCREMENT PRIMARY KEY,
                
        StateName VARCHAR(500) NOT NULL,
        Reason VARCHAR(500) NOT NULL,
        data VARCHAR(500) NOT NULL,
        
        CreatedAt DATETIME(6) NOT NULL,
        DeletedAt DATETIME(6) NULL, 

        JobId BIGINT NOT NULL,
        FOREIGN KEY (JobId) REFERENCES Jobs(Id)
        ON DELETE CASCADE
        
    ) ENGINE=InnoDB;";

    private const string CreateIndexQueueSql = @"
    CREATE INDEX IF NOT EXISTS IX_State_StateName
    ON State (StateName);";
    private const string CreateIndexJobIDSql = @"
        CREATE INDEX IF NOT EXISTS IX_State_JobId
    ON State (JobId);";

    public async Task EnsureCreatedAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync(CreateTableSql);
        await connection.ExecuteAsync(CreateIndexQueueSql);
        await connection.ExecuteAsync(CreateIndexJobIDSql);
    }
       
}