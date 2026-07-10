namespace FastJobs.Dashboard.Models;

public sealed class ScheduledJobModel
{
    public long Id                    { get; init; }

    //TODO: JobName / DisplayName Does not Exist Yet Will Default to methodname 
    public string JobName               { get; init; } = string.Empty;
    public string QueueName             { get; init; } = string.Empty;
    public string TypeName              { get; init; } = string.Empty;
    public string MethodName            { get; init; } = string.Empty;
    public DateTime EnqueueAt           { get; init; }

    //Replace WIth Next Schedule Time 
    public TimeSpan TimeTillScheduledrun    { get; init; }
    public DateTime CreatedAt           { get; init; }

    public string JobType {get; set;} = string.Empty;
}
