

namespace FastJobs.Dashboard.Models;

public sealed class JobDetailModel
{
    public long Id                   { get; init; } 
    public string JobName              { get; init; } = string.Empty;
    public string QueueName            { get; init; } = string.Empty;
    public JobState State              { get; init; }
    public DateTimeOffset CreatedAt          { get; init; }
    public DateTimeOffset? EnqueuedAt        { get; init; }
    public DateTimeOffset? StartedAt         { get; init; }
    public DateTimeOffset? CompletedAt       { get; init; }
    public TimeSpan? Duration          { get; init; }
    public int AttemptCount            { get; init; }

    //END OF SUMMARY USE CASE PROPERTIES, BELOW ARE DETAIL-ONLY PROPERTIES
    public string MethodName           { get; init; } = string.Empty;
    public string? SerializedArguments { get; init; }

    //TODO: Retrieve this in later iterations, as it requires an additional query and may not be necessary for all use cases
   // public IReadOnlyList<JobStateTransitionModel> StateHistory { get; init; } = Array.Empty<JobStateTransitionModel>();
}
