using System.Data;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using Microsoft.Extensions.Logging;

namespace FastJobs.Persistence;

public class FastJobMSSQLDependencies : IDatabaseProviderDependencies
{
    private readonly FastJobsSqlStorageOptions _options;

    public FastJobMSSQLDependencies(Action<FastJobsSqlStorageOptions> configure)
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
        services.AddScoped<DbConnectionFactory, SqlServerDbCinnectionFactory>();
        services.AddScoped<LockProvider, MSSQLLockProvider>();
        
        RegisterDbBootstrappers(services);
    }

    public void RegisterDbBootstrappers(IServiceCollection services)
    {
        services.AddSingleton<ISchemaInitializer, MSSQLJobTableInitializer>();
        services.AddSingleton<ISchemaInitializer, MSSQLQueueTableInitializer>();
        services.AddSingleton<ISchemaInitializer, MSSQLScheduledJobTableInitializer>();
        services.AddSingleton<ISchemaInitializer, MSSQLRecurringJobTableInitializer>();
        services.AddSingleton<ISchemaInitializer, MSSQLStateHistoryTableInitialization>();
        services.AddSingleton<ISchemaInitializer, MSSQLWorkerTableInitializer>();
        services.AddSingleton<ISchemaInitializer, MSSQLAfterActionTableInitializer>();
    }


    public void SetupDatabase()  // no parameter needed anymore
    {
        var ConnectionString = _options.ConnectionString;
        DbConnectionStringBuilder stringBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = ConnectionString
        };

         var _LoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));


        var _Logger = _LoggerFactory.CreateLogger("Fastjobs.NET");



        string? DBName = stringBuilder["Database"].ToString();

        if(DBName == null)
        {
            throw new Exception("Database name Is Required In Connection string");     
        }

        try {
            _Logger.LogInformation("Connecting To Database {DBName} {DateTime}", DBName, DateTime.UtcNow);

            using IDbConnection db = new SqlConnection(ConnectionString);
            db.Open();
        }
        catch(Exception ex)
        {
            _Logger.LogError(ex, "Connection Failed Attemting to Create Database");

            if(ex is SqlException)
            {
                Console.WriteLine(ex.Message);

                stringBuilder.Remove("Database");
                using IDbConnection db = new SqlConnection(stringBuilder.ConnectionString);
                var safeDbName = DBName.Replace("'", "''");
                db.Execute($@"IF DB_ID(N'{safeDbName}') IS NULL
                BEGIN
                    EXEC('CREATE DATABASE [{safeDbName}]');
                END;"
                );
            }   
            else
            {
                _Logger.LogError(ex, "DB creation Failed");
            } 
        }
        finally
        {
            _Logger.LogInformation("Connection Done");
        }

    }
}
