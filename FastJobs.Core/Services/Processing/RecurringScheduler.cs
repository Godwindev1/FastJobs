using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Cronos;

namespace FastJobs;
using FastJobs.SqlServer;
internal class RecurringScheduler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0, 1);
    private readonly TimeSpan _idleWait;
    private readonly TimeSpan _maxSleep;
    private readonly Action _notifyScheduledJobAdded;

    public RecurringScheduler(IServiceScopeFactory scopeFactory, Action notifyScheduledJobAdded)
    {
        _scopeFactory = scopeFactory;
        _notifyScheduledJobAdded = notifyScheduledJobAdded;

        using var scope = new ScopeManager(scopeFactory);
        var options = scope.Resolve<FastJobsOptions>();
        _idleWait = options.IdleWaitPeriod;
        _maxSleep = options.MaxSleep;
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
                await Task.Delay(TimeSpan.FromSeconds(60), ct); // Run every 60 seconds
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task RunRecoverySweepAsync(CancellationToken ct)
    {
        using var scope = new ScopeManager(_scopeFactory);
        var recurringRepo = scope.Resolve<IRecurringJobRepository>();
        var scheduledRepo = scope.Resolve<IScheduledJobRepository>();

        // Query for orphaned recurring jobs
        var orphanedJobs = await recurringRepo.GetOrphanedRecurringJobsAsync(ct);
        foreach (var recurringJob in orphanedJobs)
        {
            // Compute next run from now
            var nextRun = recurringJob.ComputeNextRun(DateTime.UtcNow);
            if (nextRun == null) continue;

            var scheduledJobInfo = new ScheduledJobInfo
            {
                JobId = recurringJob.JobId,
                ScheduledTo = nextRun.Value
            };

            var scheduledId = await scheduledRepo.InsertAsync(scheduledJobInfo, ct);
            recurringJob.NextScheduledID = scheduledId;
            recurringJob.NextScheduledTime = nextRun.Value;

            await recurringRepo.UpdateByIdAsync(recurringJob, ct);
        }

        if (orphanedJobs.Any())
        {
            _notifyScheduledJobAdded();
        }
    }

  
}
