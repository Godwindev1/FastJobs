namespace FastJobs;

public sealed class Job
{
    public long Id { get; set; }
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


    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt {get; set; }
}
