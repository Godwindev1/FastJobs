namespace FastJobs.Dashboard.Models.Scheduled;

public sealed class ScheduledJobModel
{
    public string Id                    { get; init; } = string.Empty;
    public string JobName               { get; init; } = string.Empty;
    public string QueueName             { get; init; } = string.Empty;
    public string TypeName              { get; init; } = string.Empty;
    public string MethodName            { get; init; } = string.Empty;
    public DateTime EnqueueAt           { get; init; }
    public TimeSpan TimeUntilEnqueue    => EnqueueAt - DateTime.UtcNow;
    public DateTime CreatedAt           { get; init; }
    public bool IsRecurringTrigger      { get; init; }
    public string? RecurringJobId       { get; init; }
}
