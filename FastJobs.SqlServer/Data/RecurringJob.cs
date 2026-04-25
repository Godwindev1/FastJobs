using Cronos;

namespace FastJobs;

public class RecurringJob
{
     public long id {get; set; }
     public long JobId { get; set; }
     public long? NextScheduledID {get; set; }
     public string? CronExpression {get;set;}

     public DateTime StartTime {get; set; }
     public long? IntervalTicks {get; set; }
     public DateTime NextScheduledTime {get; set;}

     public bool IsConcurrent {get; set;}= true;

     public int ExecutedInstances {get; set; } = 0;
     public int ExecutingInstances {get; set; } = 0; 

     public bool IsCron {get; set; } = false;

     public DateTime? ComputeNextRun(DateTime from)
     {
         if (IsCron && !string.IsNullOrWhiteSpace(CronExpression))
         {
             var cron = CronExpression.Split(' ').Length == 6
                 ? CronFormat.IncludeSeconds
                 : CronFormat.Standard;
             var parsed = Cronos.CronExpression.Parse(CronExpression, cron);
             return parsed.GetNextOccurrence(from, TimeZoneInfo.Utc);
         }
         else if (!IsCron && IntervalTicks.HasValue)
         {
             return from.AddTicks(IntervalTicks.Value);
         }
         throw new InvalidOperationException("RecurringJob must have either CronExpression or IntervalTicks set.");
     }
}