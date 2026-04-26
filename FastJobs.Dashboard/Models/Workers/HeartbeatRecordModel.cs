using FastJobs.Dashboard.Models.Enums;

namespace FastJobs.Dashboard.Models.Workers;

public sealed class HeartbeatRecordModel
{
    public string WorkerId                      { get; init; } = string.Empty;
    public DateTime Timestamp                   { get; init; }
    public WorkerState State                    { get; init; }
    public string? ActiveJobId                  { get; init; }
    public TimeSpan? IntervalFromPrevious       { get; init; }
}
