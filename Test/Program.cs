using Fastjobs.AfterActions;
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
.AddAfterAction(builder => builder.WithType<EnqueueAfterAction>())
.Start();

//Schdeuling a Concrete Job that implements IBackGroundJob interface to run after a delay of 45 seconds
//await FastJobServer.ScheduleJob<ComplexTestJob>()
//.WaitDelay(TimeSpan.FromSeconds(45))
//.Start();

//Schedulng a Recurring  Concrete Job that implements IBackGroundJob interface
//await FastJobServer.AddRecurringJob<ComplexTestJob>()
//.WithInterval(TimeSpan.FromSeconds(10), DateTime.Now)// Simple Interval Based to Run every 40 minutes, starting immediately
///.Start();
///
/////Scheduling a simple job to run after a delay of 45 seconds
///await FastJobServer.ScheduleJob(() => Console.WriteLine("Hello Kaboom At " + DateTime.Now))
///.WaitDelay(TimeSpan.FromSeconds(45))
///.Start();
///
/////Adding a recurring job that runs every minute, starting immediately, with a delay of 4 seconds before the first execution, and expires after 5 minutes.
///await FastJobServer.AddRecurringJob(() =>  Console.WriteLine($"Hello FastJobs {DateTime.Now.ToShortTimeString()} ") )
///.AddCronExpression("*/1 * * * *") // Every minute
///.RunAt(DateTime.Now)
///.WaitDelay(TimeSpan.FromSeconds(4))
///.SetExpiresAt(DateTime.Now.Add(TimeSpan.FromMinutes(5)))
///.Start();
///
///await FastJobServer.EnqueueJob(() => Console.WriteLine("Testing Fire and Forget at " + DateTime.Now)) //ENQUEUE WiTH FIRE & FORGET METHOD
///.SetPriority(JobPriority.High) // High priority job
///.SetMaxRetryCount(3) // Retry up to 3 times on failure
///.Start();
///
///
///

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
