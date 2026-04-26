namespace FastJobs.Dashboard.Models.Enums;

public enum JobState
{
    Enqueued,
    Scheduled,
    Dequeued,
    Processing,
    Succeeded,
    Failed,
    Retrying,
    Deleted
}
