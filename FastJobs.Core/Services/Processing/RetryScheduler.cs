using FastJobs;
using FastJobs.Persistence;

internal  static class JobRetryScheduler
{
    public static async Task RescheduleAsync(
        Job job, 
        DateTime scheduledTime,
        IJobRepository jobRepository,
        IStateHistoryRepository stateHistoryRepository,
        IScheduledJobRepository scheduledJobRepository,
        ProcessingServer processingServer,
        string reason = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Record the new state
        var state = new State
        {
            JobID     = job.Id ?? 0,
            StateName = QueueStateTypes.Scheduled,
            Reason    = reason ?? $"Job #{job.Id} Requeued for {scheduledTime:O}",
            data      = $"Scheduled to {scheduledTime:O}",
            CreatedAt = DateTime.UtcNow
        };

        var stateId = await stateHistoryRepository.InsertAsync(state, cancellationToken);

        job.RetryCount = job.RetryCount + 1;
        job.stateID = stateId;
        job.StateName = QueueStateTypes.Scheduled;

        // 2. Update the job's state pointer only — all other job data untouched
        await jobRepository.UpdateByIdAsync(
           job,
           cancellationToken);

        // 3. Upsert the scheduled time — row may already exist if this is a re-reschedule
        var existing = await scheduledJobRepository.GetByIdAsync(job.Id ?? 0, cancellationToken);

        if (existing is null)
        {
            await scheduledJobRepository.InsertAsync(
                new ScheduledJobInfo { JobId = job.Id ?? 0, ScheduledTo = scheduledTime },
                cancellationToken);
        }
        else
        {
            await scheduledJobRepository.UpdateByIdAsync(
                new ScheduledJobInfo {Id = existing.Id,  ScheduledTo = scheduledTime },
                cancellationToken);
        }

        // 4. Wake the scheduler
        processingServer.NotifyScheduledJobAdded();
    }
}