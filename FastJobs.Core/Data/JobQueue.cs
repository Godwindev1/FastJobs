using System.ComponentModel.DataAnnotations;
using FastJobs;

public class Queue
{
    // Identity
    [Required]
    public long Id { get; set; }
    // Queue Routing
    [Required]
    public string QueueName { get; set; }

    // Job Reference (FK to Jobs table)
    [Required]
    public long JobId { get; set; }

    public int Priority { get; set; }

    [Required]
    public bool IsScheduled { get; set; }

    // Scheduling
    public DateTime? ScheduledAt { get; set; }
}