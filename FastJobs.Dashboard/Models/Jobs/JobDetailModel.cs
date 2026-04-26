using FastJobs.Dashboard.Models.Enums;
using FastJobs.Dashboard.Models.Retries;

namespace FastJobs.Dashboard.Models.Jobs;

public sealed class JobDetailModel
{
    public string Id                   { get; init; } = string.Empty;
    public string JobName              { get; init; } = string.Empty;
    public string QueueName            { get; init; } = string.Empty;
    public JobState State              { get; init; }
    public DateTime CreatedAt          { get; init; }
    public DateTime? EnqueuedAt        { get; init; }
    public DateTime? StartedAt         { get; init; }
    public DateTime? CompletedAt       { get; init; }
    public TimeSpan? Duration          { get; init; }
    public int AttemptCount            { get; init; }
    public string? WorkerName          { get; init; }

    public string TypeName             { get; init; } = string.Empty;
    public string MethodName           { get; init; } = string.Empty;
    public string? SerializedArguments { get; init; }

    public string? ExceptionType       { get; init; }
    public string? ExceptionMessage    { get; init; }
    public string? ExceptionStackTrace { get; init; }

    public DateTime? ScheduledFor      { get; init; }

    public IReadOnlyList<JobStateTransitionModel> StateHistory { get; init; }
        = Array.Empty<JobStateTransitionModel>();

    public IReadOnlyList<RetryAttemptModel> RetryHistory { get; init; }
        = Array.Empty<RetryAttemptModel>();

    public IReadOnlyDictionary<string, string> Metadata { get; init; }
        = new Dictionary<string, string>();
}
