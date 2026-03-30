namespace FastJobs;

public static class FastJobConstants
{
    public static string DefaultQueue = "Default";
}

public enum JobPriority
{
    Low = 3,
    Normal = 2,
    Medium = 1,
    High = 0,
}