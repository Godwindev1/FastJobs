namespace FastJobs.Dashboard.Models.Jobs;

public sealed class JobQueueStatModel
{
    public string? QueueName       { get; init; }
    public int EnqueuedCount       { get; init; }
    public int ScheduledCount      { get; init; }
    public int ProcessingCount     { get; init; }
    public int SucceededCount      { get; init; }
    public int FailedCount         { get; init; }
    public int RetryingCount       { get; init; }
    public int CancelledCount      { get; init; }
    public int DeletedCount        { get; init; }

    public int Total =>
        EnqueuedCount + ScheduledCount + ProcessingCount +
        SucceededCount + FailedCount + RetryingCount +
        CancelledCount + DeletedCount;
}
