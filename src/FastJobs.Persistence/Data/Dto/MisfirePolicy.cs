namespace FastJobs.Persistence;
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


    /// <summary>
    /// Framework decides based on job type and schedule density.
    /// Sparse schedules (>1hr apart) → FireOnce. Dense schedules → Skip.
    /// </summary>
    Smart = 3

    //TODO
    //FireAll = 2
}

