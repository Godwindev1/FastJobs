namespace FastJobs.Dashboard.Models;

public enum JobState
{
    Enqueued,
    Scheduled,
    Dequeued,
    Processing,
    Completed,
    Failed,
    Retrying,
    Deleted
}
