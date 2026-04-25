using System.Data;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Dapper;

namespace FastJobs.SqlServer;

public class FastJobMysqlDependencies : IDatabaseProviderDependencies
{
    private readonly FastJobsSqlStorageOptions _options;

    public FastJobMysqlDependencies(Action<FastJobsSqlStorageOptions> configure)
    {
        _options = new FastJobsSqlStorageOptions();
        configure(_options);
    }

    public void RegisterDependencies(IServiceCollection services)
    {
        services.AddSingleton(_options); // MySqlDbConnectionFactory injects this
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();
        services.AddScoped<IScheduledJobRepository, ScheduledJobRepository>();
        services.AddScoped<IRecurringJobRepository, RecurringJobRepository>();
        services.AddScoped<IStateHistoryRepository, StateHistoryRepository>();
        services.AddScoped<DbConnectionFactory, MySqlDbConnectionFactory>();
        services.AddScoped<LockProvider, MySqlLockProvider>();
    }

    public void SetupDatabase()  // no parameter needed anymore
    {
        var ConnectionString = _options.ConnectionString;
        DbConnectionStringBuilder stringBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = ConnectionString
        };

        string? DBName = stringBuilder["Database"].ToString();
        Console.WriteLine($"DBName { DBName } ");

        if(DBName == null)
        {
            throw new Exception("Database name Is Required In Connection string");     
        }

        try {
            Console.WriteLine($"Testing DB Connection");
            using IDbConnection db = new MySqlConnection(ConnectionString);
            db.Open();
        }
        catch(Exception ex)
        {
            Console.WriteLine("Connection Failed Attemting to Create Database");

            if(ex is MySqlException)
            {
                Console.WriteLine(ex.Message);

                stringBuilder.Remove("Database");
                using IDbConnection db = new MySqlConnection(stringBuilder.ConnectionString);
                db.Execute($"CREATE DATABASE IF NOT EXISTS {DBName};");   
            }   
            else
            {
                Console.WriteLine(ex.Message);
            } 
        }
        finally
        {
            Console.WriteLine("Connecting To Database");
        }

    }
}
