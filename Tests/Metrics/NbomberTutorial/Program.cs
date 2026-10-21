using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NBomber.CSharp;


await LoadTestHarness.RunAsync();

public static class LoadTestHarness
{
    static IHost? _host;

    public static async Task RunAsync()
    {
        string connectionString = "Server=(localdb)\\FastjobsDBServer;Database=FastJobs;Trusted_Connection=True;MultipleActiveResultSets=true";

        await StartFastJobsAsync(connectionString);
        try
        {
            NBomberRunner
                .RegisterScenarios(
                    EnqueueScenarios.FireAndForget(),
                    EnqueueScenarios.InvoicePdf(),
                    EnqueueScenarios.ImageCompress())
                .WithReportFolder("results/SqlServer")
                .Run();
        }
        finally
        {
            await StopFastJobsAsync();
        }
    }

    static async Task StartFastJobsAsync(string connectionString)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddJobService<ImageCompressJob>(); // register concrete jobs if your scenarios use them
        builder.Services.AddJobService<InvoicePdfJob>(); // register concrete jobs if your scenarios use them

        builder.Services.AddFastJobs(
            o => { o.WorkerCount = 2; },
            new FastJobMSSQLDependencies(     // assumed name, see note
                opts => opts.ConnectionString = connectionString));

        _host = builder.Build();
        _host.Services.UseFastJobs();
        await _host.StartAsync();
    }

    static async Task StopFastJobsAsync()
    {
        if (_host is null) return;
        await _host.StopAsync();
        _host.Dispose();
    }
}