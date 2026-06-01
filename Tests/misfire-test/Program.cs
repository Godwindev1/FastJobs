using CronExpressionDescriptor;
using FastJobs;
using FastJobs.SqlServer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

//
string connectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;";

var builder = Host.CreateApplicationBuilder(args);

// Register the ComplexTestJob with the DI container so it can be resolved when the job is executed.
builder.Services.AddJobService<MisfireTestJob>();


builder.Services.AddFastJobs(
    option => {  option.WorkerCount = 1; option.MisfireDetectorInterval = TimeSpan.FromSeconds(30); },
     new FastJobMysqlDependencies(options => options.ConnectionString = connectionString)
);



var app = builder.Build();


app.Services.UseFastJobs();


//start host 
await app.StartAsync();

await FastJobServer
.AddRecurringJob<MisfireTestJob>()
.WithInterval(TimeSpan.FromMinutes(2), DateTime.Now)
.SetMaxRetryCount(5)
.Start();

await FastJobServer
.AddRecurringJob<MisfireTestJob>()
.WithInterval(TimeSpan.FromMinutes(2), DateTime.Now)
.SetMaxRetryCount(5)
.Start();


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
public class MisfireTestJob : IBackGroundJob
{
    private readonly Logger<MisfireTestJob> _logger;
    public MisfireTestJob(ILogger<MisfireTestJob> logger)
    {
        _logger = logger as Logger<MisfireTestJob> ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] Misfire Job Started Delay of 1 minutes", Thread.CurrentThread.Name);

        await Task.Delay(TimeSpan.FromMinutes(1));

        // Simulate saving results
        _logger.LogInformation("Phase 1: Misfire Delay Window has Completed");

        await Task.Delay(300, cancellationToken);

    }
}
