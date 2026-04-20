namespace FastJobs;

public class RecurringJob
{
     public long id {get; set; }
     public long JobId { get; set; }
     public long NextScheduledID {get; set; }
     public string CronExpression {get;set;}

     public DateTime StartTime {get; set; }
     public TimeSpan Interval {get; set; }
     public DateTime NextScheduledTime {get; set;}

     public bool IsConcurrent {get; set;}= true;
}