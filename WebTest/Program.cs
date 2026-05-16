using FastJobs;
using FastJobs.SqlServer;
using FastJobs.Dashboard;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();


var builder = WebApplication.CreateBuilder(args);

// Add job to the DI container.
builder.Services.AddJobService<ComplexTestJob>();


builder.Services.AddFastJobs(
    option => { option.WorkerCount = 2; },
    new FastJobMysqlDependencies(options => options.ConnectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;")
);

//ADD FastJobs Dashboard services
builder.Services.AddFastJobsDashboard();
builder.Host.UseSerilog();

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


//Adding a recurring job that runs every minute, starting immediately, with a delay of 4 seconds before the first execution, and expires after 5 minutes.


await FastJobServer.EnqueueJob(() => Console.WriteLine("Testing Fire and Forget at " + DateTime.Now)) //ENQUEUE WiTH FIRE & FORGET METHOD
.SetPriority(JobPriority.High) // High priority job
.SetMaxRetryCount(3) // Retry up to 3 times on failure
.Start();

await FastJobServer.EnqueueJob(() => Console.WriteLine("Testing Fire and Forget at " + DateTime.Now)) //ENQUEUE WiTH FIRE & FORGET METHOD
.SetPriority(JobPriority.High) // High priority job
.SetMaxRetryCount(3) // Retry up to 3 times on failure
.Start();


await FastJobServer.EnqueueJob(() => Console.WriteLine("Testing Fire and Forget at " + DateTime.Now)) //ENQUEUE WiTH FIRE & FORGET METHOD
.SetPriority(JobPriority.High) // High priority job
.SetMaxRetryCount(3) // Retry up to 3 times on failure
.Start();


await FastJobServer.EnqueueJob(() => Console.WriteLine("Testing Fire and Forget at " + DateTime.Now)) //ENQUEUE WiTH FIRE & FORGET METHOD
.SetPriority(JobPriority.High) // High priority job
.SetMaxRetryCount(3) // Retry up to 3 times on failure
.Start();

app.Run();

//Concrete Job that implements IBackGroundJob interface
public class ComplexTestJob : IBackGroundJob
{
    private readonly ILogger<ComplexTestJob> _logger;

    private readonly JobContext context1;

    private readonly IRecurringJobRepository repo;

    public ComplexTestJob(ILogger<ComplexTestJob> logger, IJobContext context, IRecurringJobRepository jobRepository)
    {
        _logger = logger;
        repo = jobRepository;
        context1 = (JobContext)context;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try {
            var Result = await repo.GetByJob(context1.CurrentJob);
            _logger.LogInformation("Testing");
            await Task.Delay(40000);
            _logger.LogInformation("This Is Instance {} completed at {Time}", Result.ExecutedInstances,  DateTime.UtcNow);
        }
        catch(Exception e)
        {
            _logger.LogInformation($"Error {e.Message}");
        }
     }
}