using FastJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


string connectionString = "Server=ppmpdb;Database=FastJobs;User=root;Password=rootpassword;";

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddJobService<ConsoleTestJob>();
builder.Services.FastJobs(option => option.ConnectionString = connectionString, new FastJobs.SqlServer.FastJobMysqlDependincies());



var app = builder.Build();

app.Services.UseFastJobs();

await app.StartAsync();

await FastJobServer.EnqueueJob<ConsoleTestJob>()
    .WithDelay(TimeSpan.FromSeconds(10))
    .Start();

await app.WaitForShutdownAsync();


public class ConsoleTestJob : IBackGroundJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        for (int i = 10; i > 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Counting Down: {i}");
            await Task.Delay(200, cancellationToken);
        }

        Console.WriteLine("Completed Console Job");
    }
}