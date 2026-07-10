using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class JobCleanupManager : BackgroundService
{
    private readonly ICleanupStrategy _cleanupStrategy;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    private readonly ILogger<JobCleanupManager> _logger;

    public JobCleanupManager(ICleanupStrategy cleanupStrategy, ILogger<JobCleanupManager> logger, FastJobsOptions options)
    {
        _cleanupStrategy = cleanupStrategy;
        _interval = options.CleanupInterval;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        //initial Delay To make Sure all other Systems Are Running Before this Starts 
         await Task.Delay(TimeSpan.FromSeconds(15), ct);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await _cleanupStrategy.Clean(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // expected during shutdown — just exit the loop
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cleanup strategy failed during scheduled run");
            }
        }
    }
}