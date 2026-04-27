namespace FastJobs.Dashboard.Models;

//METRICS MODEL WILL BE IMPLEMENTED AT A LATER STAGE, CURRENTLY JUST A PLACEHOLDER FOR FUTURE METRICS RELATED TO WORKERS
public sealed class WorkerMetricsModel
{
    public string WorkerId                  { get; init; } = string.Empty;
    public double AvgProcessingLatencyMs    { get; init; }
    public int JobsProcessed                { get; init; }
    public int JobsSucceeded                { get; init; }
    public int JobsFailed                   { get; init; }
    public double SuccessRate               => JobsProcessed == 0 ? 0d : Math.Round((double)JobsSucceeded / JobsProcessed * 100, 2);
}
