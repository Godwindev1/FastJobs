using FastJobs.SqlServer;
using Microsoft.Extensions.Hosting;

public class JobCleanupManager : BackgroundService
{
    private readonly IJobRepository _JobRepo;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public JobCleanupManager(IJobRepository jobRepository, FastJobsOptions options)
    {
        _JobRepo = jobRepository;
        _interval = options.CleanupInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        //initial Delay To make Sure all other Systems Are Running Before this Starts 
         await Task.Delay(TimeSpan.FromSeconds(15), ct);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(ct))
        {
            //Cascade Delete Query
        }
    }
}