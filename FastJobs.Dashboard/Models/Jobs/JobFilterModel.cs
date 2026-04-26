using FastJobs.Dashboard.Models.Enums;

namespace FastJobs.Dashboard.Models.Jobs;

public sealed class JobFilterModel
{
    public JobState? State         { get; set; }
    public string? QueueName       { get; set; }
    public string? Search          { get; set; }
    public DateTime? CreatedFrom   { get; set; }
    public DateTime? CreatedTo     { get; set; }
    public int Page                { get; set; } = 1;
    public int PageSize            { get; set; } = 25;
    public string SortBy           { get; set; } = nameof(JobSummaryModel.CreatedAt);
    public bool SortDescending     { get; set; } = true;
}
