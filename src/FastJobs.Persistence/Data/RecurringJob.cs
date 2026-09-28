using Cronos;

namespace FastJobs.Persistence;
public class RecurringJob
{
     public long id {get; set; }
     public long JobId { get; set; }
     public long? NextScheduledID {get; set; }
     public string? CronExpression {get;set;}

     public DateTimeOffset StartTime {get; set; }
     public long? IntervalTicks {get; set; }
     public DateTimeOffset NextScheduledTime {get; set;}

     public bool IsConcurrent {get; set;}= true;

     public int ExecutedInstances {get; set; } = 0;
     public int ExecutingInstances {get; set; } = 0; 

     public bool IsCron {get; set; } = false;

     public DateTimeOffset? ComputeNextRun(DateTimeOffset from)
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