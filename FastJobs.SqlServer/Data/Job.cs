namespace FastJobs.SqlServer;

//THIS IS REPEATED IN CORE PROJECT AS WELL, ANY CHANGES HERE SHOULD BE REFLECTED IN THE CORE PROJECT TILL A UNIFIED MODEL IS DECIDED UPON.
public static class JobTypes
{
    public static string Enqueued = "Enqueued";
    public static string Scheduled = "Scheduled";
    public static string Recurring = "Recurring";
    public static string Batch = "Batch";
}


public sealed class Job
{
    public long Id { get; set; }

    public string JobType {get; set; } = JobTypes.Enqueued;
    public  string TypeName { get; set; } 
    public  string MethodName { get; set; }

    //For Fire And Forget Jobs
    public string MethodDeclaringTypeName {get; set; }

    // Stored as JSON
    public  string ParameterTypeNamesJson { get; set; } 
    public  string ArgumentsJson { get; set; }

    public  string Queue { get; set; }
    public long stateID  { get; set; }
    public  string StateName { get; set; }

    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int Priority { get; set; }


    public long LeaseOwner {get; set; }
    public DateTime LeaseExpiresAt {get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt {get; set; }
}
