using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class JobCleanupManager : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _CleanupStartDelay = TimeSpan.FromMinutes(15);

    private readonly ILogger<JobCleanupManager> _logger;

    public JobCleanupManager(IServiceScopeFactory scopeFactory, ILogger<JobCleanupManager> logger, FastJobsOptions options)
    {
        _scopeFactory = scopeFactory;
        _CleanupStartDelay = options.InitialCleanupDelay;
        _interval = options.CleanupInterval;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        //initial Delay To make Sure all other Systems Are Running Before this Starts
         await Task.Delay(_CleanupStartDelay, ct);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = new ScopeManager(_scopeFactory);
                await scope.Resolve<ICleanupStrategy>().Clean(ct);
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