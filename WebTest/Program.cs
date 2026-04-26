using FastJobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJobService<ComplexTestJob>();
builder.Services.FastJobs(
    option => { option.WorkerCount = 4; },
    new FastJobs.SqlServer.FastJobMysqlDependencies(options => options.ConnectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;")
);

builder.Services.AddFastjobsDashboard();

var app = builder.Build();

app.UseStaticFiles();   
app.UseFastjobsDashboard("/fastjobs");
app.UseRouting();
app.UseAntiforgery();   


app.MapFastjobsDashboard();

app.MapGet("/", () => "FastJobs Dashboard is running at /fastjobs");

app.Services.UseFastJobs();

await FastJobServer.EnqueueJob<ComplexTestJob>().Start();

app.Run();


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