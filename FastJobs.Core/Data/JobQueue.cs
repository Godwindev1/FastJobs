using FastJobs;

public class Queue
{
    // Identity
    public long Id { get; set; }
    // Queue Routing
    public string QueueName { get; set; }

    // Job Reference (FK to Jobs table)
    public long JobId { get; set; }

    public int Priority { get; set; }

    // Scheduling
    public DateTime? ScheduledAt { get; set; }
}