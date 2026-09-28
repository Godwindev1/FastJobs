using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

//For Recurring Jobs Only
public class RecurringMisfireDetector
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FastJobsOptions _options;

    private readonly ILogger<RecurringMisfireDetector> _Logger;
    public RecurringMisfireDetector(
        IServiceScopeFactory scopeFactory,
        FastJobsOptions options,
        ILogger<RecurringMisfireDetector> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _Logger = logger;
    }

    public async Task DetectAndHandleAsync(CancellationToken ct)
    {
        using var scope = new ScopeManager(_scopeFactory);
        var repository = scope.Resolve<IJobRepository>();
        var recurringJobRepository = scope.Resolve<IRecurringJobRepository>();
        var queueRepository = scope.Resolve<IQueueRepository>();

        var threshold = _options.MisfireThreshold;
        var now = DateTimeOffset.UtcNow;

        //Only Returns Recurring Jobs that have misfired, as Non-Recurring Jobs are handled by the Scheduler directly
        var misfiredJobs = await repository.GetMisfiredJobsAsync(
            cutoff: now - threshold,
            ct: ct
        );


        foreach (var job in misfiredJobs)
        {
            _Logger.LogInformation("Handling Misfire For Job with Id #{JobID}", job.Id);
            await HandleMisfireAsync(job, now, recurringJobRepository, queueRepository, ct);
        }
    }

    private async Task HandleMisfireAsync(Job job, DateTimeOffset now, IRecurringJobRepository recurringJobRepository, IQueueRepository queueRepository, CancellationToken ct)
    {
        var policy = job.misfirePolicy == (int)MisfirePolicy.Smart
            ? await ResolveSmartPolicy(job, now, recurringJobRepository)
            : (MisfirePolicy)job.misfirePolicy;

        // For Skip: do nothing—scheduler already handles next run
        if (policy == MisfirePolicy.Skip)
            return;

        // For FireOnce: enqueue once if not already in queue
        if (policy == MisfirePolicy.FireOnce)
        {
            if (await queueRepository.GetByJob(job.Id ?? 0, ct) == null)
            {
                await queueRepository.EnqueueAsync(
                    new Queue
                    {
                        JobId = job.Id ?? 0,
                        Priority = (int)JobPriority.High,
                        QueueName = QueueNames.Critical,
                        IsMisfireRecovery = true
                    },
                    ct
                );
            }
        }

    }

    private async Task<MisfirePolicy> ResolveSmartPolicy(Job job, DateTimeOffset now, IRecurringJobRepository recurringJobRepository)
    {
        if(job.JobType == JobTypes.Recurring)
        {
            var result = await recurringJobRepository.GetByJob(job);
            var interval = EstimateInterval(result, now); // implement this based on job.Cron or job.Interval

            return interval > TimeSpan.FromHours(1)
            ? MisfirePolicy.FireOnce
            : MisfirePolicy.Skip;
        }

        return MisfirePolicy.FireOnce;
    }

    private TimeSpan EstimateInterval(RecurringJob job, DateTimeOffset now)
    {
        return job.ComputeNextRun(now) - now ?? TimeSpan.FromHours(1);
    }
}
