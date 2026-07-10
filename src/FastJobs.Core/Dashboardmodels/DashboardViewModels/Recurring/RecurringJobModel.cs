
namespace FastJobs.Dashboard.Models;
public sealed class RecurringJobModel
{
    public long Id                        { get; init; } 
    
    //TODO: Display name Should Be A Custom Set Name For The Job the DB store DOes not reflect this Yet 
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
    
    //Uses Default Values For Now Until This Data is Tracked Properly in the DB
    public int SucceededCount                { get; init; } = -1;
    public int FailedCount                   { get; init; } = -1;

    public DateTime RegisteredAt            { get; init; }
}
