namespace FastJobs.Dashboard.Models;

public sealed class WorkerOverviewModel
{
    public int TotalWorkers                 { get; set; }
    public int ActiveWorkers                { get; set; }
    public int SleepingWorkers              { get; set; }
    public int DeadWorkers                  { get; set; }
    public int TotalActiveJobs              { get; set; }
    public DateTime GeneratedAt             { get; set; } = DateTime.UtcNow;
    public List<WorkerModel> Workers { get; set; } = [];
}
