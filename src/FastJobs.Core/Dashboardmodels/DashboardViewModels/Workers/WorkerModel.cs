

namespace FastJobs.Dashboard.Models;
public sealed class WorkerModel
{
    public string WorkerId                      { get; init; } = string.Empty;
    public string WorkerName                    { get; init; } = string.Empty;
    public WorkerState State                    { get; init; }
    public DateTimeOffset StartedAt                   { get; init; }
    public DateTimeOffset LastHeartbeatAt             { get; init; }
    public TimeSpan HeartbeatAge                => DateTimeOffset.UtcNow - LastHeartbeatAt;
    
    //public DateTime? CurrentJobStartedAt        { get; init; }
    //public TimeSpan? CurrentJobRunTime          => CurrentJobStartedAt.HasValue ? DateTime.UtcNow - CurrentJobStartedAt.Value : null;
    public WorkerMetricsModel Metrics           { get; init; } = new();
}