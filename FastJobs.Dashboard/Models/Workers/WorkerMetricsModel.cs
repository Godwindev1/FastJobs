namespace FastJobs.Dashboard.Models.Workers;

public sealed class WorkerMetricsModel
{
    public string WorkerId                  { get; init; } = string.Empty;

    public double AvgProcessingLatencyMs    { get; init; }
    public double MinProcessingLatencyMs    { get; init; }
    public double MaxProcessingLatencyMs    { get; init; }
    public double P95ProcessingLatencyMs    { get; init; }

    public double AvgEnqueueLatencyMs       { get; init; }
    public double MinEnqueueLatencyMs       { get; init; }
    public double MaxEnqueueLatencyMs       { get; init; }
    public double P95EnqueueLatencyMs       { get; init; }

    public int JobsProcessed                { get; init; }
    public int JobsSucceeded                { get; init; }
    public int JobsFailed                   { get; init; }
    public double SuccessRate               => JobsProcessed == 0
        ? 0d
        : Math.Round((double)JobsSucceeded / JobsProcessed * 100, 2);

    public int LocksHeld                    { get; init; }
    public int TotalLocksAcquired           { get; init; }
    public int TotalLocksReleased           { get; init; }

    public IReadOnlyList<HeartbeatRecordModel> RecentHeartbeats { get; init; }
        = Array.Empty<HeartbeatRecordModel>();
}
