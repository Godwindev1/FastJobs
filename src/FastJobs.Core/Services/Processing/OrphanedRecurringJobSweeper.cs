using Microsoft.Extensions.DependencyInjection;


namespace FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.Logging;

internal class OrphanedRecurringJobSweeper 
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0, 1);
    private readonly TimeSpan _idleWait;
    private readonly Action _notifyScheduledJobAdded;

    private readonly ILogger<OrphanedRecurringJobSweeper > _logger;

    public OrphanedRecurringJobSweeper (IServiceScopeFactory scopeFactory, Action notifyScheduledJobAdded)
    {
        _scopeFactory = scopeFactory;
        _notifyScheduledJobAdded = notifyScheduledJobAdded;

        using var scope = new ScopeManager(scopeFactory);
        _logger = scope.Resolve<ILogger<OrphanedRecurringJobSweeper >>();
        var options = scope.Resolve<FastJobsOptions>();
        _idleWait = options.IdleWaitPeriod;
    }

    public void NotifyJobAdded()
    {
        if (_signal.CurrentCount == 0)
            _signal.Release();
    }

    public async Task StartAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunRecoverySweepAsync(ct);
                await Task.Delay(_idleWait, ct); 
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Encountered While Running Recovery Sweep For Orphaned Jobs");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task RunRecoverySweepAsync(CancellationToken ct)
    {
        using var scope = new ScopeManager(_scopeFactory);
        var recurringRepo = scope.Resolve<IRecurringJobRepository>();

        var orphanedJobs = await recurringRepo.GetOrphanedRecurringJobsAsync(ct);

        var anyScheduled = false;
        foreach (var recurringJob in orphanedJobs)
        {
            if (await RecurringJobScheduling.ScheduleNextOccurrenceAsync(recurringJob, scope, ct))
                anyScheduled = true;
        }

        if (anyScheduled)
            _notifyScheduledJobAdded();
    }

  
}
