using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using FastJobs;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

string connectionString  = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;";

// 1 - Create the service collection
var services = new ServiceCollection();
services.FastJobs( Option =>  Option.ConnectionString = connectionString , new FastJobs.FastJobMysqlDependincies() );

var Provider = services.BuildServiceProvider();
Provider.UseFastJobs();

//FastJobs.FastJobRepoTests Test = new FastJobRepoTests(Provider.GetRequiredService<IJobRepository>(), Provider.GetRequiredService<IQueueRepository>());
//await Test.RunTest();

FastJobs.FastJobServer.EnqueueJob(() => JobsHelp.Job());
 

public static class JobsHelp
{
    public static void Job()
    {
        Console.WriteLine("Hello Booty");
    }   
}

