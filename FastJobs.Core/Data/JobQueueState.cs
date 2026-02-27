namespace FastJobs;

public static class QueueStateTypes
{
    public static string Enqueued = "Enqueued";
    public static string Scheduled = "Scheduled";
    public static string Processing = "Processing";
    public static string Completed = "Completed";
    public static string Failed = "Failed";
}

public class State
{
    public long Id {get; set;}

    public required long JobID {get; set; }
    public required string StateName {get; set;}
    public required string Reason {get; set; }
    public required string data {get; set; }

    public required DateTime CreatedAt {get; set;}
}