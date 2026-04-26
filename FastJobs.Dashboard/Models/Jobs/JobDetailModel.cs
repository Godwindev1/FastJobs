using FastJobs.Dashboard.Models.Enums;

namespace FastJobs.Dashboard.Models;

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

    //END OF SUMMARY USE CASE PROPERTIES, BELOW ARE DETAIL-ONLY PROPERTIES
    public string? WorkerName          { get; init; }
    public string TypeName             { get; init; } = string.Empty;
    public string MethodName           { get; init; } = string.Empty;
    public string? SerializedArguments { get; init; }
    public IReadOnlyList<JobStateTransitionModel> StateHistory { get; init; } = Array.Empty<JobStateTransitionModel>();
}
