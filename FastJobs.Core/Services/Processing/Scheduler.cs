using FastJobs;
using Microsoft.Extensions.DependencyInjection;


public class Scheduler
{
    private readonly IServiceScopeFactory _scopeFactory;
    
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0, 1);
    
    private static TimeSpan IdleWait = TimeSpan.FromSeconds(30);
    
    // Don't sleep longer than this even if next job is far away
    // Protects against clock drift and missed signals
    private static TimeSpan MaxSleep = TimeSpan.FromMinutes(5);

    public Scheduler(IServiceScopeFactory factory)
    {
        _scopeFactory = factory;
        using var scope = new ScopeManager(factory);
        var options = scope.Resolve<FastJobsOptions>();
        IdleWait = options.IdleWaitPeriod;
        MaxSleep = options.MaxSleep;
    }


    //Interrupts Scheduler Sleep If a New Job is Added 
    public void NotifyJobAdded()
    {
        if (_signal.CurrentCount == 0)
            _signal.Release(1);
    }

    public async Task StartScheduler(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var manager = new ScopeManager(_scopeFactory);
                var scheduledRepo = manager.Resolve<IScheduledJobRepository>();

                var dueJobs = await scheduledRepo.GetReadyJobsAsync(ct);

                foreach (var dueJob in dueJobs)
                {
                    _ = EnqueueScheduledJobSafe(dueJob, ct);
                }

                var nextJob = await scheduledRepo.GetNextScheduledJob(ct);

                TimeSpan delay = ComputeDelay(nextJob);

                //Sleep — but interruptible by NotifyJobAdded()
                await WaitAsync(delay, ct);
            }
            catch (OperationCanceledException)
            {
                break; 
            }
            catch (Exception ex)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private TimeSpan ComputeDelay(ScheduledJobInfo? nextJob)
    {
        //if there is Nothing Sleep For IdleWait time
        if (nextJob is null)
            return IdleWait; 

        var timeUntilNext = nextJob.ScheduledTo - DateTime.UtcNow;

        //if its Already Due During This Process Requery The Due Jobs Again
        if (timeUntilNext <= TimeSpan.Zero)
            return TimeSpan.Zero; 

        // Cap at MaxSleep — safety net against missed signals
        return timeUntilNext < MaxSleep ? timeUntilNext : MaxSleep;
    }

    // Interruptible sleep Should  wake early if NotifyJobAdded() is called
    private async Task WaitAsync(TimeSpan delay, CancellationToken ct)
    {
        if (delay <= TimeSpan.Zero) return;

        // Wait returns true if signalled, false if timed out (by delay) — both are fine
        await _signal.WaitAsync(delay, ct);

        // Drain the signal so next WaitAsync starts clean
        while (_signal.CurrentCount > 0)
            _signal.Wait(0);
    }

    private async Task EnqueueScheduledJobSafe(ScheduledJobInfo jobInfo, CancellationToken ct)
    {
        try
        {
            await EnqueueScheduledJob(jobInfo, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task EnqueueScheduledJob(ScheduledJobInfo jobInfo, CancellationToken ct)
    {
        using var manager = new ScopeManager(_scopeFactory);
        
        var jobRepo = manager.Resolve<IJobRepository>();
        var job = await jobRepo.GetByIdAsync(jobInfo.JobId);

        if (job is null)
            throw new InvalidOperationException(
                $"Job {jobInfo.JobId} pointed to by ScheduledJobInfo does not exist.");

        var entry = new Queue
        {
            JobId       = jobInfo.JobId,
            DequeuedAt = jobInfo.ScheduledTo,
            isDequeued = false,
        };

        try {
            await EnqueueJob(entry, ct, QueueNames.Critical, manager);
            var scheduledInfoRepo = manager.Resolve<IScheduledJobRepository>();
            await scheduledInfoRepo.DeleteByIdAsync(jobInfo.Id);
        }
        catch (Exception ex)
        {
            //---
        }
    }

    private async Task EnqueueJob(
        Queue entry, 
        CancellationToken ct, 
        string queueName,
        ScopeManager manager)  
    {
        entry.QueueName = queueName;
        var queueRepo = manager.Resolve<IQueueRepository>();
        await queueRepo.EnqueueAsync(entry, ct);
    }
}