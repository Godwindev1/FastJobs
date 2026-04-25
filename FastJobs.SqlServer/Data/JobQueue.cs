using System.ComponentModel.DataAnnotations;
namespace FastJobs.SqlServer;

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
    public bool isDequeued { get; set; }

    // Scheduling
    public DateTime? DequeuedAt { get; set; }
}