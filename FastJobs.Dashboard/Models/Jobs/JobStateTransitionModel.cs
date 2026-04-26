using FastJobs.Dashboard.Models.Enums;

namespace FastJobs.Dashboard.Models.Jobs;

public sealed class JobStateTransitionModel
{
    public JobState FromState       { get; init; }
    public JobState ToState         { get; init; }
    public DateTime TransitionedAt  { get; init; }
    public string? Reason           { get; init; }
}
