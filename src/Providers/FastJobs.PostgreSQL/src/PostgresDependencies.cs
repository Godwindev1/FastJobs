using System.Data;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Dapper;
using Microsoft.Extensions.Logging;

namespace FastJobs.Persistence;

public class FastJobPostgresDependencies : IDatabaseProviderDependencies
{
    private readonly FastJobsSqlStorageOptions _options;

    static FastJobPostgresDependencies()
    {
        SqlMapper.AddTypeHandler(new PostgresDateTimeOffsetTypeHandler());
    }

    public FastJobPostgresDependencies(Action<FastJobsSqlStorageOptions> configure)
    {
        _options = new FastJobsSqlStorageOptions();
        configure(_options);
    }

    public void RegisterDependencies(IServiceCollection services)
    {
        services.AddSingleton(_options);
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();
        services.AddScoped<IScheduledJobRepository, ScheduledJobRepository>();
        services.AddScoped<IRecurringJobRepository, RecurringJobRepository>();
        services.AddScoped<IStateHistoryRepository, StateHistoryRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddScoped<IAfterActionRepository, AfterActionRepository>();
        services.AddScoped<DbConnectionFactory, PostgresDbConnectionFactory>();
        services.AddScoped<LockProvider, PostgresLockProvider>();

        RegisterDbBootstrappers(services);
    }

    public void RegisterDbBootstrappers(IServiceCollection services)
    {
        services.AddSingleton<ISchemaInitializer, PostgresJobTableInitializer>();
        services.AddSingleton<ISchemaInitializer, PostgresQueueTableInitializer>();
        services.AddSingleton<ISchemaInitializer, PostgresScheduledJobTableInitializer>();
        services.AddSingleton<ISchemaInitializer, PostgresRecurringJobTableInitializer>();
        services.AddSingleton<ISchemaInitializer, PostgresStateHistoryTableInitialization>();
        services.AddSingleton<ISchemaInitializer, PostgresWorkerTableInitializer>();
        services.AddSingleton<ISchemaInitializer, PostgresAfterActionTableInitializer>();
    }

    public void SetupDatabase()
    {
        var connectionString = _options.ConnectionString;
        var stringBuilder = new NpgsqlConnectionStringBuilder(connectionString);

        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger("Fastjobs.NET");

        string? dbName = stringBuilder.Database;

        if (string.IsNullOrWhiteSpace(dbName))
        {
            throw new Exception("Database name Is Required In Connection string");
        }

        try
        {
            logger.LogInformation("Connecting To Database {DBName} {DateTime}", dbName, DateTime.UtcNow);

            using IDbConnection db = new NpgsqlConnection(connectionString);
            db.Open();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Connection Failed Attemting to Create Database");

            // 3D000 = invalid_catalog_name (database does not exist)
            if (ex is PostgresException { SqlState: "3D000" })
            {
                Console.WriteLine(ex.Message);

                // Connect to the maintenance database to create the target one.
                stringBuilder.Database = "postgres";
                stringBuilder.Pooling = false;

                using IDbConnection db = new NpgsqlConnection(stringBuilder.ConnectionString);
                var safeDbName = dbName.Replace("\"", "\"\"");
                db.Execute($"CREATE DATABASE \"{safeDbName}\";");
            }
            else
            {
                logger.LogError(ex, "DB creation Failed");
            }
        }
        finally
        {
            logger.LogInformation("Connection Done");
        }
    }
}
