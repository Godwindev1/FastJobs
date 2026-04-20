using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

public class RecurringJobOptions<TJob> where TJob : class, IBackGroundJob
{
    private readonly Job _job;
    private readonly IServiceScopeFactory _scopeFactory;

    // Recurring-specific state
    private DateTime  _startTime        = DateTime.UtcNow.AddHours(1);
    private TimeSpan  _IntervalVMs         = TimeSpan.FromHours(1);
    private string    _cronExpression   = string.Empty;
    private bool      _isConcurrent     = false;

    internal RecurringJobOptions(Job job, IServiceScopeFactory factory)
    {
        _job          = job;
        _scopeFactory = factory;
    }


    /// <summary>
    /// Sets the absolute UTC time at which the recurring job should first run.
    /// </summary>
    public RecurringJobOptions<TJob> RunAt(DateTime startTime)
    {
        if (startTime <= DateTime.UtcNow)
            throw new ArgumentException("Start time must be in the future.", nameof(startTime));

        _startTime = startTime;
        return this;
    }

    /// <summary>
    /// Sets the first run time relative to now via a delay.
    /// </summary>
    public RecurringJobOptions<TJob> WaitDelay(TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
            throw new ArgumentException("Delay must be greater than zero.", nameof(delay));

        _startTime = DateTime.UtcNow.Add(delay);
        return this;
    }

    /// <summary>
    /// Sets how often the job repeats after each successful execution.
    /// </summary>
    public RecurringJobOptions<TJob> WithInterval(TimeSpan IntervalVMs)
    {
        if (IntervalVMs <= TimeSpan.Zero)
            throw new ArgumentException("Interval must be greater than zero.", nameof(IntervalVMs));

        _IntervalVMs = IntervalVMs;
        return this;
    }

    /// <summary>
    /// Attaches a cron expression to the recurring job.
    /// Cron-based scheduling is not yet active — this is reserved for future use.
    /// </summary>
    public RecurringJobOptions<TJob> AddCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            throw new ArgumentException("Cron expression cannot be empty.", nameof(cronExpression));

        // TODO: Validate via Cronos once integrated.
        _cronExpression = cronExpression;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of times this job will be retried on failure.
    /// </summary>
    public RecurringJobOptions<TJob> SetMaxRetryCount(int retryCount)
    {
        if (retryCount < 0)
            throw new ArgumentException("Retry count cannot be negative.", nameof(retryCount));

        _job.MaxRetries = retryCount;
        return this;
    }


    /// <summary>
    /// Sets the UTC datetime after which this recurring job will no longer be scheduled.
    /// </summary>
    public RecurringJobOptions<TJob> SetExpiresAt(DateTime expiresAt)
    {
        if (expiresAt <= _startTime)
            throw new ArgumentException("Expiry must be after the job start time.", nameof(expiresAt));

        _job.ExpiresAt = expiresAt;
        return this;
    }


    /// <summary>
    /// Allows a new instance of this job to start even if a previous instance is still running.
    /// Defaults to false (non-concurrent) if not called.
    /// </summary>
    public RecurringJobOptions<TJob> AllowConcurrentExecution()
    {
        _isConcurrent = true;
        return this;
    }

    /// <summary>
    /// Persists the recurring job configuration and registers it for repeated execution.
    /// The first run time is determined by <see cref="RunAt"/> or <see cref="WaitDelay"/>;
    /// subsequent runs are driven by <see cref="WithIntervalVMs"/> or the attached cron expression.
    /// </summary>
    public async Task Start(CancellationToken cancellationToken = default)
    {
        using var scope = new ScopeManager(_scopeFactory);

        var jobRepository           = scope.Resolve<IJobRepository>();
        var stateHistoryRepository  = scope.Resolve<IStateHistoryRepository>();
        var recurringJobRepository  = scope.Resolve<IRecurringJobRepository>();
        var processingServer        = scope.Resolve<ProcessingServer>();

        var jobId = await jobRepository.InsertAsync(_job, cancellationToken);

        var state = new State
        {
            JobID     = jobId,
            StateName = QueueStateTypes.Scheduled,
            Reason    = $"Recurring job registered. First run at {_startTime:O}. IntervalVMs: {_IntervalVMs}.",
            data      = $"StartTime={_startTime:O}; IntervalVMs={_IntervalVMs}; Cron={_cronExpression}",
            CreatedAt = DateTime.UtcNow
        };

        var stateId = await stateHistoryRepository.InsertAsync(state, cancellationToken);

        await jobRepository.UpdateByIdAsync(
            jobId,
            "stateID = @stateID, StateName = @StateName, JobType = @JobType",
            new Job
            {
                stateID   = stateId,
                StateName = QueueStateTypes.Scheduled,
                JobType   = JobTypes.Recurring
            },
            cancellationToken);

        var recurringJob = new RecurringJob
        {
            JobId             = jobId,
            NextScheduledID   = 0,               // No scheduled run exists Atp the RecurringJobService will Be Responsible For Creating Those
            CronExpression    = _cronExpression,
            StartTime         = _startTime,
            IntervalVMs          = _IntervalVMs,
            NextScheduledTime = _startTime,       // First execution target
            IsConcurrent      = _isConcurrent
        };

        await recurringJobRepository.InsertAsync(recurringJob, cancellationToken);

    }
}