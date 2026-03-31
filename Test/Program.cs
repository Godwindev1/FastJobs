using FastJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


string connectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;";

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddJobService<ComplexTestJob>();
builder.Services.FastJobs(option => { option.ConnectionString = connectionString; option.WorkerCount = 4; }, new FastJobs.SqlServer.FastJobMysqlDependincies());



var app = builder.Build();

app.Services.UseFastJobs();

await app.StartAsync();

for(int i = 0; i < 3; i++)
{
   // await FastJobServer.EnqueueJob<ComplexTestJob>()
   // .Start();

    await FastJobServer.ScheduleJob<ComplexTestJob>()
    .WaitDelay(TimeSpan.FromSeconds(10))
    .Start();

    //await FastJobServer.EnqueueJob(() => Console.WriteLine("Testing Fire and Forget at " + DateTime.Now))
    //.SetPriority(JobPriority.High) // High priority job
    //.SetMaxRetryCount(i < 10 ? 3 : 0) // First 10 jobs will retry up to 3 times, others won't retry
    //.Start();
}

await app.WaitForShutdownAsync();


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
