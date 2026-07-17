using FastJobs.Persistence;

namespace FastJobs;

internal static class RecurringJobScheduling
{
    public static async Task<bool> ScheduleNextOccurrenceAsync(
        RecurringJob recurringJob,
        ScopeManager scope,
        CancellationToken ct)
    {
        var jobRepository = scope.Resolve<IJobRepository>();
        var recurringJobRepository = scope.Resolve<IRecurringJobRepository>();
        var scheduledJobRepository = scope.Resolve<IScheduledJobRepository>();
        var stateHelper = new StateHelpers(jobRepository, scope.Resolve<IStateHistoryRepository>());

        var job = await jobRepository.GetByIdAsync(recurringJob.JobId);
        if (job == null) return false;

        if (job.ExpiresAt.HasValue && DateTime.UtcNow >= job.ExpiresAt.Value)
            return false;

        if (!recurringJob.IsConcurrent && recurringJob.ExecutingInstances > 0)
            return false;

        var nextRun = recurringJob.ComputeNextRun(DateTime.UtcNow);
        if (nextRun == null) return false;

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
            recurringJob.id, QueueStateTypes.Scheduled,
            $"Recurring job #{recurringJob.id} rescheduled for {nextRun:O}", "", ct);

        return true;
    }
}