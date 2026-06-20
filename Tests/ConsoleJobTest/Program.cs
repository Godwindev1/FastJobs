using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FastJobs;
using FastJobs.SqlServer;
using FastJobs.AfterActions;
//
string connectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;";

var builder = Host.CreateApplicationBuilder(args);

// Register the ComplexTestJob with the DI container so it can be resolved when the job is executed.
builder.Services.AddJobService<FailTestJob>();
builder.Services.AddJobService<ValidateOrderJob>();
builder.Services.AddJobService<ChargePaymentJob>();
builder.Services.AddJobService<SendConfirmationEmailJob>();
builder.Services.AddJobService<NotifyWarehouseJob>();

builder.Services.AddFastJobs(
    option => {  option.WorkerCount = 2; },
     new FastJobMysqlDependencies(options => options.ConnectionString = connectionString)
);



var app = builder.Build();


app.Services.UseFastJobs();


//start host 
await app.StartAsync();

await FastJobServer
.EnqueueJob<FailTestJob>()
.AddAfterAction( x => x.WithType<DeleteAfterAction>())
.SetMaxRetryCount(5)
.Start();

//await FastJobServer.CreateChain()
//.AddStep<ValidateOrderJob>()
//.ThenRun<ChargePaymentJob>()
//.ThenRun<SendConfirmationEmailJob>()
//.ThenRun<NotifyWarehouseJob>()
//.EnqueueAsync();


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
public class FailTestJob : IBackGroundJob
{
    private readonly Logger<FailTestJob> _logger;

    public FailTestJob(ILogger<FailTestJob> logger)
    {
        _logger = logger as Logger<FailTestJob> ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] Fail Job Started", Thread.CurrentThread.Name);



        // Simulate saving results
        _logger.LogInformation("Phase 1: Throwing an Exception To Simulate Failure and Test Backoff Logic");
        throw new TerminateRetryException("Simulated failure for testing backoff logic.");
        await Task.Delay(300, cancellationToken);

    }
}
