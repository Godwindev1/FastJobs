namespace FastJobs.Dashboard.Models;

public sealed class QueueDepthModel
{
    public string QueueName             { get; init; } = string.Empty;
    public int EnqueuedCount            { get; init; }
    public int ProcessingCount          { get; init; }
    public TimeSpan AverageWaitTime     { get; init; }
    public double ThroughputPerMinute   { get; init; }
}
