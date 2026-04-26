using FastJobs.Dashboard.Models.Jobs;

namespace FastJobs.Dashboard.Models.Overview;

public sealed class DashboardSummaryModel
{
    public int EnqueuedCount           { get; init; }
    public int ScheduledCount          { get; init; }
    public int ProcessingCount         { get; init; }
    public int SucceededCount          { get; init; }
    public int FailedCount             { get; init; }
    public int RetryingCount           { get; init; }
    public int CancelledCount          { get; init; }
    public int TotalJobs               { get; init; }

    public int ActiveWorkers           { get; init; }
    public int SleepingWorkers         { get; init; }
    public int DeadWorkers             { get; init; }
    public int TotalServers            { get; init; }

    public int SucceededLastHour       { get; init; }
    public int FailedLastHour          { get; init; }
    public double ThroughputPerMinute  { get; init; }

    public IReadOnlyList<ThroughputBucketModel> HourlyTrend { get; init; }
        = Array.Empty<ThroughputBucketModel>();

    public IReadOnlyList<JobSummaryModel> RecentFailures { get; init; }
        = Array.Empty<JobSummaryModel>();

    public int JobRetentionDays        { get; init; }
    public int DefaultMaxRetries       { get; init; }
    public DateTime GeneratedAt        { get; init; } = DateTime.UtcNow;
}
