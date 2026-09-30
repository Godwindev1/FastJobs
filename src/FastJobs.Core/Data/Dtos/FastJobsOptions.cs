
public class FastJobsOptions
{
    public int WorkerCount {get; set; } = 1;
    public TimeSpan DefaultJobExpiration {get; set; } = TimeSpan.FromHours(24);

    //Exponential Backoff for Retry logic 
    public TimeSpan JobRetryDelayBase {get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan MaxJobRetryDelay {get; set; } = TimeSpan.FromSeconds(300);
    public TimeSpan Jitter {get; set; } = TimeSpan.FromSeconds(5);

    //SCHEDULER OPTIONS (Scheduler: moves due scheduled jobs onto the queue)

    /// <summary>How long the Scheduler sleeps when there are no scheduled jobs. Default: 30 seconds</summary>
    public TimeSpan SchedulerIdleWait {get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Upper bound on a single Scheduler sleep even if the next job is further away.
    /// Protects against clock drift and missed signals. Default: 5 minutes
    /// </summary>
    public TimeSpan SchedulerMaxSleep {get; set; } = TimeSpan.FromMinutes(5);

    //RECURRING JOB OPTIONS

    /// <summary>
    /// How often the recurring-jobs recovery sweep (OrphanedRecurringJobSweeper) runs
    /// to reschedule recurring jobs that have no pending occurrence. Default: 30 seconds
    /// </summary>
    public TimeSpan RecurringSweepInterval {get; set; } = TimeSpan.FromSeconds(30);

    public int DefaultMaxRetries {get; set; } = 3;
    public int DefaultWOrkerHeartbeatIntervalSeconds {get; set; } = 30;

    //MISFIRE OPTIONS

    /// <summary>
    /// How late a job must be before it's considered misfired.
    /// Jobs within this window are executed normally, not as misfires.
    /// Default: 60 seconds 
    /// </summary>
    public TimeSpan MisfireThreshold { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan MisfireDetectorInterval { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan MisfireDetectorStartupDelay { get; set; } = TimeSpan.FromSeconds(5);


    //CLEANUP OPTIONS
    public TimeSpan CleanupInterval {get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan InitialCleanupDelay {get; set; } = TimeSpan.FromSeconds(5);
}