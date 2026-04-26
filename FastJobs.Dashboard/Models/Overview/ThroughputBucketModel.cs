namespace FastJobs.Dashboard.Models.Overview;

public sealed class ThroughputBucketModel
{
    public DateTime HourStart      { get; init; }
    public int SucceededCount      { get; init; }
    public int FailedCount         { get; init; }
    public int EnqueuedCount       { get; init; }
}
