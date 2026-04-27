using FastJobs;
using FastJobs.SqlServer;
using FastJobs.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// Add job to the DI container.
builder.Services.AddJobService<ComplexTestJob>();

builder.Services.AddFastJobs(
    option => { option.WorkerCount = 2; },
    new FastJobMysqlDependencies(options => options.ConnectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;")
);

//ADD FastJobs Dashboard services
builder.Services.AddFastJobsDashboard();

var app = builder.Build();

//Add FastJobs Dashboard middleware for the dashboard endpoint. This should be added before routing  middleware.
app.UseFastJobsDashboard("/Dashboard");
app.UseStaticFiles();   
app.UseRouting();
app.UseAntiforgery();   

// Endpoint registration  uses internal path To map the dashboard components and API endpoints. This should be added after routing middleware.
app.MapFastJobsDashboard();

app.MapGet("/", () => "FastJobs Dashboard is running at /Dashboard");

app.Services.UseFastJobs();

await FastJobServer.EnqueueJob<ComplexTestJob>().Start();

//Schdeuling a Concrete Job that implements IBackGroundJob interface to run after a delay of 45 seconds
await FastJobServer.ScheduleJob<ComplexTestJob>()
.WaitDelay(TimeSpan.FromSeconds(45))
.Start();

//Schedulng a Recurring  Concrete Job that implements IBackGroundJob interface
//await FastJobServer.AddRecurringJob<ComplexTestJob>()
//.WithInterval(TimeSpan.FromSeconds(10), DateTime.Now)// Simple Interval Based to Run every 40 minutes, starting immediately
//.Start();

//Scheduling a simple job to run after a delay of 45 seconds
await FastJobServer.ScheduleJob(() => Console.WriteLine("Hello Kaboom At " + DateTime.Now))
.WaitDelay(TimeSpan.FromSeconds(45))
.Start();

//Adding a recurring job that runs every minute, starting immediately, with a delay of 4 seconds before the first execution, and expires after 5 minutes.
//await FastJobServer.AddRecurringJob(() =>  Console.WriteLine($"Hello FastJobs {DateTime.Now.ToShortTimeString()} ") )
//.AddCronExpression("*/1 * * * *") // Every minute
//.RunAt(DateTime.Now)
//.WaitDelay(TimeSpan.FromSeconds(4))
//.SetExpiresAt(DateTime.Now.Add(TimeSpan.FromMinutes(5)))
//.Start();

//await FastJobServer.EnqueueJob(() => Console.WriteLine("Testing Fire and Forget at " + DateTime.Now)) //ENQUEUE WiTH FIRE & FORGET METHOD
//.SetPriority(JobPriority.High) // High priority job
//.SetMaxRetryCount(3) // Retry up to 3 times on failure
//.Start();


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