namespace FastJobs.Dashboard.Models;

public sealed class WorkerOverviewModel
{
    public int TotalWorkers                 { get; init; }
    public int ActiveWorkers                { get; init; }
    public int SleepingWorkers              { get; init; }
    public int DeadWorkers                  { get; init; }
    public int TotalActiveJobs              { get; init; }
    public DateTime GeneratedAt             { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<WorkerModel> Workers { get; init; } = Array.Empty<WorkerModel>();
}
