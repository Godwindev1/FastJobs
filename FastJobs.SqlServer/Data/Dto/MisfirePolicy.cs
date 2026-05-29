namespace FastJobs.SqlServer;
public enum MisfirePolicy
{
    /// <summary>
    /// Skip all missed executions. Wait for next scheduled time.
    /// Best for: time-sensitive data fetches, market prices, heartbeats.
    /// </summary>
    Skip = 0,

    /// <summary>
    /// Run the job once immediately regardless of how many executions were missed.
    /// Best for: cleanup tasks, health checks, maintenance jobs.
    /// </summary>
    FireOnce = 1,

    //TODO: ADD SYSTEM SUPPORT FOR THE TWO MISFIRE POLICIES BELOW

    /// <summary>
    /// Run every missed execution sequentially before resuming schedule.
    /// Best for: hourly reports, audit logs, billing cycles.
    /// </summary>
    //RunAll = 2,

    /// <summary>
    /// Framework decides based on job type and schedule density.
    /// Sparse schedules (>1hr apart) → FireOnce. Dense schedules → Skip.
    /// </summary>
    Smart = 3
}

