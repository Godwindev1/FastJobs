namespace FastJobs.Persistence;
public sealed class JobTimestamps
{
    public DateTimeOffset? EnqueuedAt   { get; init; }
    public DateTimeOffset? StartedAt    { get; init; }
    public DateTimeOffset? CompletedAt  { get; init; }
}