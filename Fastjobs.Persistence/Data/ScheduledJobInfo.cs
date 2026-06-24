namespace FastJobs.Persistence;
public class ScheduledJobInfo
{
    public long Id { get; set; }
    public long JobId { get; set; }
    public DateTime ScheduledTo { get; set; }
}