public class FSTJBS_Worker
{
    public long Id { get; set; }
    public required string WorkerName { get; set; }
    public required string ThreadName { get; set; }
    public required DateTime StartedAt { get; set; }

    public bool isSleeping { get; set; }

    public bool isCrashed {get; set;}

    public DateTime? LastHeartbeat { get; set; }
}