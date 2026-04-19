
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs {
  public class ScheduledJobOptions<TJob> where TJob : class, IBackGroundJob
{
    private readonly Job _job;
    private readonly IServiceScopeFactory _scopeFactory;
    private DateTime _scheduledTime = DateTime.UtcNow.AddHours(1); // Default: 1 hour from now

    internal ScheduledJobOptions(Job job, IServiceScopeFactory factory)
    {
        _job = job;
        _scopeFactory = factory;
    }

    public ScheduledJobOptions<TJob> RunAt(DateTime scheduledTime)
    {
        if (scheduledTime <= DateTime.UtcNow)
        {
            throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledTime));
        }
        _scheduledTime = scheduledTime;
        return this;
    }

    public ScheduledJobOptions<TJob> WaitDelay(TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentException("Delay must be greater than zero.", nameof(delay));
        }
        _scheduledTime = DateTime.UtcNow.Add(delay);
        return this;
    } 

    public ScheduledJobOptions<TJob> SetMaxRetryCount(int retryCount)
    {
        _job.MaxRetries = retryCount;
        return this;
    }

    public ScheduledJobOptions<TJob> SetExpiresAt(DateTime expiresAt)
    {
        _job.ExpiresAt = expiresAt;
        return this;
    }

    /// <summary>
    /// Saves the job and schedules it for later execution.
    /// Job is stored but NOT enqueued - it will be picked up by ScheduledJobService when its scheduled time arrives.
    /// </summary>
    public async Task Start(CancellationToken cancellationToken = default)
    {
        using var Scope = new ScopeManager(this._scopeFactory);

        var jobRepository             = Scope.Resolve<IJobRepository>();
        var stateHistoryRepository    = Scope.Resolve<IStateHistoryRepository>();
        var scheduledJobRepository    = Scope.Resolve<IScheduledJobRepository>();

        // Insert the job
        var jobId = await jobRepository.InsertAsync(_job, cancellationToken);

        // Create state history entry
        var state = new State
        {
            JobID     = jobId,
            StateName = QueueStateTypes.Scheduled,
            Reason    = $"Scheduled for execution at {_scheduledTime:O}",
            data      = $"Scheduled to {_scheduledTime:O}",
            CreatedAt = DateTime.UtcNow
        };

        var stateId = await stateHistoryRepository.InsertAsync(state, cancellationToken);

        // Update job with state ID and mark as Scheduled type
        await jobRepository.UpdateByIdAsync(jobId, "stateID = @stateID, StateName = @StateName, JobType = @JobType",
            new Job 
            { 
                stateID = stateId, 
                StateName = QueueStateTypes.Scheduled,
                JobType = JobTypes.Scheduled
            }, cancellationToken);


        // Insert into ScheduledJobs table - will NOT be in active queue yet
        var scheduledJobInfo = new ScheduledJobInfo
        {
            JobId = jobId,
            ScheduledTo = _scheduledTime
        };

        var scheduledJobId = await scheduledJobRepository.InsertAsync(scheduledJobInfo, cancellationToken);
    }
}
}