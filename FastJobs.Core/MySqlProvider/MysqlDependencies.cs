using System.Data;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Dapper;

namespace FastJobs;

public class FastJobMysqlDependincies : IDatabaseProviderDependencies
{
    public void RegisterDependencies(IServiceCollection services)
    {
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();
    }

    public void SetupDatabase(IServiceCollection Services, string ConnectionString)
    {
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

            Services.AddScoped<IDbConnection>(serviceProvider =>{
                    var connection = new MySqlConnection(ConnectionString);
                    connection.Open();
                    return connection;
                }
            );
        }


    }

    

}