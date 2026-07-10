
using FastJobs;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs.AfterActions;

public class EnqueueAfterAction : IAfterAction
{
    EnqueueOptions<EnqueueAfterAction> JobEnqueueOption;

    public EnqueueAfterAction(IJobContext job, IServiceScopeFactory ScopeFactory)
    {
        var CurrentJob = job.CurrentJob;
        CurrentJob.Id = null; 
        CurrentJob.RetryCount = 0;
        CurrentJob.AfterActionId = null;
        CurrentJob.ScheduledRunAt = DateTime.UtcNow;
        JobEnqueueOption = new EnqueueOptions<EnqueueAfterAction>(CurrentJob, ScopeFactory);
    }

    public async Task ExecuteAsync(CancellationToken Token)
    {
        await JobEnqueueOption.Start();
    }
}