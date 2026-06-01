using FastJobs;
using FastJobs.SqlServer;
using Microsoft.Extensions.Logging;

//For Recurring Jobs Only
public class RecurringMisfireDetector
{
    private readonly IJobRepository _repository;
    private readonly IRecurringJobRepository _recurringJobRepository;
    private readonly IQueueRepository _queueRepository;
    private readonly FastJobsOptions _options;

    private readonly ILogger<RecurringMisfireDetector> _Logger;
    public RecurringMisfireDetector(
        IJobRepository repository,
        IRecurringJobRepository recurringJobRepository,
        IQueueRepository queueRepository,
        FastJobsOptions options,
        ILogger<RecurringMisfireDetector> logger)
    {
        _repository = repository;
        _recurringJobRepository = recurringJobRepository;
        _queueRepository = queueRepository;
        _options = options;
        _Logger = logger;
    }

    public async Task DetectAndHandleAsync(CancellationToken ct)
    {
        
        var threshold = _options.MisfireThreshold;
        var now = DateTime.UtcNow;

        //Only Returns Recurring Jobs that have misfired, as Non-Recurring Jobs are handled by the Scheduler directly
        var misfiredJobs = await _repository.GetMisfiredJobsAsync(
            cutoff: now - threshold,
            ct: ct
        );

        
        foreach (var job in misfiredJobs)
        {
            _Logger.LogInformation("Handling Misfire For Job with Id #{JobID}", job.Id);
            await HandleMisfireAsync(job, now, ct);
        }
    }

    private async Task HandleMisfireAsync(Job job, DateTime now, CancellationToken ct)
    {
        var policy = job.misfirePolicy == (int)MisfirePolicy.Smart
            ? await ResolveSmartPolicy(job, now)
            : (MisfirePolicy)job.misfirePolicy;

        // For Skip: do nothing—scheduler already handles next run
        if (policy == MisfirePolicy.Skip)
            return;

        // For FireOnce: enqueue once if not already in queue
        if (policy == MisfirePolicy.FireOnce)
        {
            if (await _queueRepository.GetByJob(job.Id ?? 0, ct) == null)
            {
                await _queueRepository.EnqueueAsync(
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

    private async Task<MisfirePolicy> ResolveSmartPolicy(Job job, DateTime now)
    {
        if(job.JobType == JobTypes.Recurring)
        {
            var result = await _recurringJobRepository.GetByJob(job);
            var interval = EstimateInterval(result, now); // implement this based on job.Cron or job.Interval

            return interval > TimeSpan.FromHours(1)
            ? MisfirePolicy.FireOnce
            : MisfirePolicy.Skip;           
        }
        
        return MisfirePolicy.FireOnce; 
    }

    private TimeSpan EstimateInterval(RecurringJob job, DateTime now)
    {
        return job.ComputeNextRun(now) - now ?? TimeSpan.FromHours(1);
    }
}