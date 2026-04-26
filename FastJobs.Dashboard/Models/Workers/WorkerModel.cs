using FastJobs.Dashboard.Models.Enums;

namespace FastJobs.Dashboard.Models.Workers;

public sealed class WorkerModel
{
    public string WorkerId                      { get; init; } = string.Empty;
    public string WorkerName                    { get; init; } = string.Empty;
    public string ServerName                    { get; init; } = string.Empty;
    public IReadOnlyList<string> Queues         { get; init; } = Array.Empty<string>();

    public WorkerState State                    { get; init; }
    public DateTime StartedAt                   { get; init; }
    public DateTime LastHeartbeatAt             { get; init; }
    public TimeSpan HeartbeatAge                => DateTime.UtcNow - LastHeartbeatAt;

    public string? CurrentJobId                 { get; init; }
    public string? CurrentJobName               { get; init; }
    public DateTime? CurrentJobStartedAt        { get; init; }
    public TimeSpan? CurrentJobRunTime          =>
        CurrentJobStartedAt.HasValue ? DateTime.UtcNow - CurrentJobStartedAt.Value : null;

    public int LocksHeld                        { get; init; }
    public WorkerMetricsModel Metrics           { get; init; } = new();
}
