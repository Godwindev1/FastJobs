using FastJobs.Dashboard.Models.Enums;

namespace FastJobs.Dashboard.Models;

public sealed class RecurringJobModel
{
    public string Id                        { get; init; } = string.Empty;
    public string DisplayName               { get; init; } = string.Empty;
    public string TypeName                  { get; init; } = string.Empty;
    public string MethodName                { get; init; } = string.Empty;
    public string QueueName                 { get; init; } = string.Empty;
    public ScheduleType ScheduleType        { get; init; }
    public string? CronExpression           { get; init; }
    public string? CronDescription          { get; init; }
    public TimeSpan? Interval               { get; init; }
    public string TimeZoneId                { get; init; } = "UTC";
    public RecurringJobStatus Status        { get; init; }
    public DateTime? NextRunAt              { get; init; }
    public DateTime? LastRunAt              { get; init; }
    public JobState? LastRunState           { get; init; }
    public string? LastRunJobId             { get; init; }
    public int SucceededCount                { get; init; }
    public int FailedCount                   { get; init; }

    public DateTime RegisteredAt            { get; init; }
}
