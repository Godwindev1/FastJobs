using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using FastJobs;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Xml;

string connectionString  = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;";

// 1 - Create the service collection
var services = new ServiceCollection();
services.AddJobService<ConsoleTestJob>();
services.FastJobs( Option =>  Option.ConnectionString = connectionString , new FastJobs.SqlServer.FastJobMysqlDependincies() );

var Provider = services.BuildServiceProvider();
Provider.UseFastJobs();

//await FastJobs.FastJobServer.EnqueueJob(() => JobsHelp.Job());
await FastJobs.FastJobServer.EnqueueJob<ConsoleTestJob>().WithDelay(TimeSpan.FromSeconds(10)).Start();

public class ConsoleTestJob : IBackGroundJob
{
    public Task ExecuteAsync(CancellationToken ck)
    {
        for(int i = 100; i > 0; i--)
        {
            Console.WriteLine($"Counting Down: {i}");
            Thread.Sleep(200);
        }
        
        Console.WriteLine("Completed Console Job");
        return Task.CompletedTask;
    }   
}

