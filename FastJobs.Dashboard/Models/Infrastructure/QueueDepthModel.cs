namespace FastJobs.Dashboard.Models.Infrastructure;

public sealed class QueueDepthModel
{
    public string QueueName             { get; init; } = string.Empty;
    //Work awaiting a worker (The "Backlog").
    public int EnqueuedCount            { get; init; }

    //Work currently in progress.
    public int ProcessingCount          { get; init; }

    //Workers that are currently processing jobs from 
    public int AssignedWorkers          { get; init; }
    //The "customer experience" metric (how long jobs wait).
    public TimeSpan AverageWaitTime     { get; init; }
    
    //The "heartbeat" of the queue (are we moving fast enough?)
    public double ThroughputPerMinute   { get; init; }
}
