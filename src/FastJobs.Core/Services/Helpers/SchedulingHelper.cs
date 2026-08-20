using FastJobs.Persistence;
using Microsoft.Extensions.Logging;

namespace FastJobs;

internal static class RecurringJobScheduling
{
    public static async Task<bool> ScheduleNextOccurrenceAsync(
        RecurringJob recurringJob,
        ScopeManager scope,
        CancellationToken ct)
    {
        //NOTE IN THIS FILE recurringJob.jobId and job.id are interchangeable the RecurringJob Table Column JobId references the Job Table Primary key Id
        var jobRepository = scope.Resolve<IJobRepository>();
        var recurringJobRepository = scope.Resolve<IRecurringJobRepository>();
        var scheduledJobRepository = scope.Resolve<IScheduledJobRepository>();
        var stateHelper = new StateHelpers(jobRepository, scope.Resolve<IStateHistoryRepository>());
        var Logger =  scope.Resolve<ILogger>();

        var job = await jobRepository.GetByIdAsync(recurringJob.JobId);
        if (job == null) return false;


        if (job.ExpiresAt.HasValue && DateTime.UtcNow >= job.ExpiresAt.Value)
        {
            try
            {
                // Update job state with atomic state history creation and rollback support
                await stateHelper.UpdateJobStateAsync(
                    job.Id ?? -1,
                    QueueStateTypes.Expired,
                    $"Job #{job.Id} Has Expired",
                    data: "");
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "[IN ScheduleNextOccurrenceAsync]: Job #{JobID} Failed While setting Expired State", job.Id);
            }
            return false;
        } 

        if (!recurringJob.IsConcurrent && recurringJob.ExecutingInstances > 0)
        {
            return false;
        }

        var nextRun = recurringJob.ComputeNextRun(DateTime.UtcNow);
        if (nextRun == null) return false;

        //If Next run would Be Expired optimistically handle it Here
        if (job.ExpiresAt.HasValue && nextRun.Value >= job.ExpiresAt.Value)
        {
            try
            {
                await stateHelper.UpdateJobStateAsync(
                    job.Id ?? -1,
                    QueueStateTypes.Expired,
                    $"Job #{job.Id} next occurrence would exceed expiration",
                    data: "");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[IN ScheduleNextOccurrenceAsync]: Job #{JobID} Failed While setting Expired State", job.Id);
            }
            return false;
        }

        var scheduledJobInfo = new ScheduledJobInfo
        {
            JobId = recurringJob.JobId, // underlying job, not the recurring job's own id
            ScheduledTo = nextRun.Value
        };

        var scheduledId = await scheduledJobRepository.InsertAsync(scheduledJobInfo, ct);

        recurringJob.NextScheduledID = scheduledId;
        recurringJob.NextScheduledTime = nextRun.Value;
        await recurringJobRepository.UpdateByIdAsync(recurringJob, ct);

        await stateHelper.UpdateJobStateAsync(
            recurringJob.JobId, QueueStateTypes.Scheduled,
            $"Recurring job #{recurringJob.id} rescheduled for {nextRun:O}", "", ct);

        return true;
    }
}