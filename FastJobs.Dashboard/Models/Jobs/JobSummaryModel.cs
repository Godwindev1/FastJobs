using FastJobs.Dashboard.Models.Enums;

namespace FastJobs.Dashboard.Models.Jobs;

public sealed class JobSummaryModel
{
    public string Id               { get; init; } = string.Empty;
    public string JobName          { get; init; } = string.Empty;
    public string QueueName        { get; init; } = string.Empty;
    public JobState State          { get; init; }
    public DateTime CreatedAt      { get; init; }
    public DateTime? EnqueuedAt    { get; init; }
    public DateTime? StartedAt     { get; init; }
    public DateTime? CompletedAt   { get; init; }
    public TimeSpan? Duration      { get; init; }
    public int AttemptCount        { get; init; }
    public string? WorkerName      { get; init; }
}
