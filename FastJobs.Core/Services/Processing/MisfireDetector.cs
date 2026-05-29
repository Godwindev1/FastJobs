using FastJobs;
using FastJobs.SqlServer;

public class MisfireDetector
{
    private readonly IJobRepository _repository;
    private readonly IQueueRepository _queueRepository;
    private readonly FastJobsOptions _options;

    public async Task DetectAndHandleAsync(CancellationToken ct)
    {
        var threshold = _options.MisfireThreshold;
        var now = DateTime.UtcNow;

        // Fetch all scheduled/recurring jobs that are overdue beyond threshold
        var misfiredJobs = await _repository.GetMisfiredJobsAsync(
            cutoff: now - threshold, 
            ct: ct
        );

        foreach (var job in misfiredJobs)
        {
            await HandleMisfireAsync(job, now, ct);
        }
    }

    private async Task HandleMisfireAsync(Job job, DateTime now, CancellationToken ct)
    {
        var policy = job.misfirePolicy == (int)MisfirePolicy.Smart
            ? ResolveSmartPolicy(job, now)
            : (MisfirePolicy)job.misfirePolicy;

        switch (policy)
        {
            case MisfirePolicy.Skip:
                // Just advance the next run time, don't execute
                if( await _queueRepository.GetByJob(job.Id ?? 0, ct) == null)
                {
                    await _queueRepository.EnqueueAsync(new Queue { JobId = job.Id ?? 0, Priority = (int)JobPriority.High, QueueName = QueueNames.Critical  }, ct);
                }
                break;

            case MisfirePolicy.FireOnce:
                // Enqueue a single catch-up execution
                if( await _queueRepository.GetByJob(job.Id ?? 0, ct) == null)
                    await _queueRepository.EnqueueAsync(new Queue { JobId = job.Id ?? 0, Priority = (int)JobPriority.High, QueueName = QueueNames.Critical  }, ct);
               
                break;

            /*case MisfirePolicy.RunAll:
                // Reconstruct all missed execution windows and enqueue each
                var missedTimes = ComputeMissedExecutionTimes(job, now);
                foreach (var missedTime in missedTimes)
                {
                    await _executor.EnqueueAsync(job, scheduledFor: missedTime, isMisfireRecovery: true, ct);
                }
                await _repository.UpdateNextRunTimeAsync(job.Id, ComputeNextRun(job, now), ct);
                break;*/ //TO BE IMPLEMENTED AT A LATER DATE
        }
    }

    private MisfirePolicy ResolveSmartPolicy(Job job, DateTime now)
    {
        //INTERVALS ARE FOR RECURRING AND SCHEDULED JOBS
        // Sparse: > 1 hour between executions → FireOnce (missing one matters)
        // Dense: <= 1 hour → Skip (catch-up flood isn't worth it)
        //var interval = EstimateInterval(job);
        var interval = TimeSpan.FromHours(1);
        return interval > TimeSpan.FromHours(1) 
            ? MisfirePolicy.FireOnce 
            : MisfirePolicy.Skip;
    }
}