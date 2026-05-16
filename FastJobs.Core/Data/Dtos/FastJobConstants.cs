namespace FastJobs;


public static class FastJobConstants
{
    public static string DefaultQueue = "Default";
}

//REPEATED IN FASTJOBS.SQLSERVER
public static class JobTypes
{
    public static string Enqueued = "Enqueued";
    public static string Scheduled = "Scheduled";
    public static string Recurring = "Recurring";
    public static string Chain = "Chain";
}

public enum JobPriority
{
    Low = 3,
    Normal = 2,
    Medium = 1,
    High = 0,
}