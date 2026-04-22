namespace FastJobs;

public class RecurringJob
{
     public long id {get; set; }
     public long JobId { get; set; }
     public long NextScheduledID {get; set; }
     public string CronExpression {get;set;}

     public DateTime StartTime {get; set; }
     public TimeSpan IntervalVMs {get; set; }
     public DateTime NextScheduledTime {get; set;}

     public bool IsConcurrent {get; set;}= true;

     public int ExecutedInstances {get; set; } = 0;
     public int ExecutingInstances {get; set; } = 0; 

     public bool IsCron {get; set; } = false;
}