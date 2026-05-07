---
title: Quickstart
nav_order: 2
next_page:
  title: Configuration
  url: /Configuration
---

# Quickstart

Welcome to the Quick Start Guide for Getting up and running with **FastJobs** .

FastJobs is a lightweight .NET background job processing library built for simplicity and speed.
As you read this guide, expect to see details of:
- Fastjobs Installation
- Configuration & Setup
- Using Fastjobs in a sample .net console App
- Using Fastjobs in a sample Web App 


## Fastjobs Installation
You can install Fastjobs via the .NET CLI or the NuGet Package Manager.

### .NET CLI

```bash
dotnet add package FastJobs
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package FastJobs
```



## NuGet Packages

FastJobs is split into focused packages so you only install what you need.


`FastJobs`  Core engine already discussed above and required for all setups 

`FastJobs.SqlServer`  Sql Server  Required for Persistance persistence for recurring jobs *Currently Supports only My Sql* 

```bash
dotnet add package FastJobs.SqlServer
```
`FastJobs.Dashboard` Optional RCL dashboard for monitoring and  observability 

```bash
dotnet add package FastJobs.Dashboard
```


---

## Configuration & Setup
Fastjobs Is Very Easy To Setup And Get Going. The main job scheduling services you will be interacting with live in `Fastjobs` namespace and the persistence layer for sql in `Fastjobs.sqlServer` namespace.

To use Fastjobs Add the following using statements 
``` csharp
using FastJobs;
using FastJobs.SqlServer;
```

Next Call `builder.Services.AddFastJobs()` with Options for extra config info like so 

```csharp 
string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
builder.Services.AddFastJobs(
    option => {  option.WorkerCount = 4; },

    //Fastjobs.sqlServer only has mysql / mariadb provider as of april 2026
     new FastJobs.SqlServer.FastJobMysqlDependencies(
        options => options.ConnectionString =  connectionString
    )
);

//TO INCLUDE THE WEB DASHBOARD
builder.Services.AddFastjobsDashboard();

```

to Finish up configuration call use FastJobs()
```csharp

var app = builder.Build();

app.Services.UseFastJobs();
```
---
if you would like to include the Web Dashboard NB: This Wont work if your application does not use a Web host

```csharp
//ADD USING STATEMENT 
using FastJobs.Dashboard;

/*{
    DI And Services Setup
}*/

var app = builder.Build();

app.UseFastJobsDashboard("/Dashboard"); //should come before routing is done to allow rewriting path to internal Dashboard path
app.UseStaticFiles();   
app.UseRouting();
app.UseAntiforgery();   

//Expose Dashboard Components
app.MapFastJobsDashboard();

app.Services.UseFastJobs();
```

## Console App Tutorial Fastjobs 
The Below Code snippet shows usage of Fastjobs in a simple .NET app With a generic Host.

```csharp
using FastJobs;
using FastJobs.SqlServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


//
string connectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;";

var builder = Host.CreateApplicationBuilder(args);

// Register the ComplexTestJob with the DI container so it can be resolved when the job is executed.
builder.Services.AddJobService<ComplexTestJob>();

builder.Services.AddFastJobs(
    option => {  option.WorkerCount = 2; },
     new FastJobMysqlDependencies(options => options.ConnectionString = connectionString)
);



var app = builder.Build();

app.Services.UseFastJobs();

//start host 
await app.StartAsync();

//Enqueuing a Concrete Job that implements IBackGroundJob interface
await FastJobServer.EnqueueJob<ComplexTestJob>()
.Start();

//Schdeuling a Concrete Job that implements IBackGroundJob interface to run after a delay of 45 seconds
await FastJobServer.ScheduleJob<ComplexTestJob>()
.WaitDelay(TimeSpan.FromSeconds(45))
.Start();

//Schedulng a Recurring  Concrete Job that implements IBackGroundJob interface
await FastJobServer.AddRecurringJob<ComplexTestJob>()
.WithInterval(TimeSpan.FromSeconds(10), DateTime.Now)// Simple Interval Based to Run every 40 minutes, starting immediately
.Start();

//Scheduling a simple job to run after a delay of 45 seconds
await FastJobServer.ScheduleJob(() => Console.WriteLine("Hello Kaboom At " + DateTime.Now))
.WaitDelay(TimeSpan.FromSeconds(45))
.Start();

//Adding a recurring job that runs every minute, starting immediately, with a delay of 4 seconds before the first execution, and expires after 5 minutes.
await FastJobServer.AddRecurringJob(() =>  Console.WriteLine($"Hello FastJobs {DateTime.Now.ToShortTimeString()} ") )
.AddCronExpression("*/1 * * * *") // Every minute
.RunAt(DateTime.Now)
.WaitDelay(TimeSpan.FromSeconds(4))
.SetExpiresAt(DateTime.Now.Add(TimeSpan.FromMinutes(5)))
.Start();

await FastJobServer.EnqueueJob(() => Console.WriteLine("Testing Fire and Forget at " + DateTime.Now)) //ENQUEUE WiTH FIRE & FORGET METHOD
.SetPriority(JobPriority.High) // High priority job
.SetMaxRetryCount(3) // Retry up to 3 times on failure
.Start();




await app.WaitForShutdownAsync();


//CONCRETE JOBS THAT INHERIT FROM IBackGroundJob INTERFACE
public class ComplexTestJob : IBackGroundJob
{
    private readonly Logger<ComplexTestJob> _logger;

    public ComplexTestJob(ILogger<ComplexTestJob> logger)
    {
        _logger = logger as Logger<ComplexTestJob> ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] Job Started", Thread.CurrentThread.Name);

        // Simulate fetching data
        _logger.LogInformation("Phase 1: Fetching data...");
        await Task.Delay(500, cancellationToken);
        var items = Enumerable.Range(1, 20).ToList();

        // Simulate processing each item with cancellation awareness
        _logger.LogInformation("Phase 2: Processing {Count} items...", items.Count);
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simulate variable-length work per item
            var delay = Random.Shared.Next(100, 400);
            await Task.Delay(delay, cancellationToken);

            _logger.LogInformation("  Processed item {Item} in {Ms}ms on [{Thread}]",
                item, delay, Thread.CurrentThread.Name);
        }

        // Simulate saving results
        _logger.LogInformation("Phase 3: Saving results...");
        await Task.Delay(300, cancellationToken);

        _logger.LogInformation("[{Thread}] Job Completed", Thread.CurrentThread.Name);
    }
}
```

## Web App Tutorial Fastjobs 
The Below Code snippet shows usage of Fastjobs in a Web application with usage of the Dashboard.

```csharp 
using FastJobs;
using FastJobs.Dashboard;
using FastJobs.SqlServer;

var builder = WebApplication.CreateBuilder(args);

// Add job to the DI container.
builder.Services.AddJobService<ComplexTestJob>();

builder.Services.AddFastJobs(
    option => { option.WorkerCount = 4; },
    new FastJobMysqlDependencies(options => options.ConnectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;")
);

//Add FastJobs Dashboard services
builder.Services.AddFastjobsDashboard();

var app = builder.Build();

app.Services.UseFastJobs();
app.UseFastjobsDashboard("/Dashboard"); //This should be added before routing  middleware.


app.UseStaticFiles();   
app.UseRouting();
app.UseAntiforgery();   

//Map Dashboard Endpoints
app.MapFastjobsDashboard();

app.MapGet("/", () => "FastJobs Dashboard is running at /Dashboard");



{
    await FastJobServer.EnqueueJob<ComplexTestJob>().Start();

    //Schdeuling a Concrete Job that implements IBackGroundJob interface to run after a delay of 45 seconds
    await FastJobServer.ScheduleJob<ComplexTestJob>()
    .WaitDelay(TimeSpan.FromSeconds(45))
    .Start();

    //Schedulng a Recurring  Concrete Job that implements IBackGroundJob interface
    await FastJobServer.AddRecurringJob<ComplexTestJob>()
    .WithInterval(TimeSpan.FromSeconds(10), DateTime.Now)// Simple Interval Based to Run every 40 minutes, starting immediately
    .Start();

}


app.Run();

//Concrete Job that implements IBackGroundJob interface
public class ComplexTestJob : IBackGroundJob
{
    private readonly ILogger<ComplexTestJob> _logger;

    public ComplexTestJob(ILogger<ComplexTestJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ComplexTestJob started at {Time}", DateTime.UtcNow);
        // Simulate work
        await Task.Delay(5000);
        _logger.LogInformation("ComplexTestJob completed at {Time}", DateTime.UtcNow);
    }
}
```

And Thats How easy it is to Get Fastjobs Running. To Continue And Learn More Ways to Leverage Fastjobs Contine By reading  [Jobs](/jobs.md)