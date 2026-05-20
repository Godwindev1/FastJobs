
using FastJobs;
using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace Fastjobs.AfterActions;

public class DeleteAfterAction : IAfterAction
{
    private readonly IJobRepository _Repository;
    private readonly long JobID;

    public DeleteAfterAction(IJobContext job, IServiceScopeFactory ScopeFactory)
    {
        JobID = job.CurrentJob.Id ?? -1;
        _Repository = new ScopeManager(ScopeFactory).Resolve<IJobRepository>();
    }

    public async Task ExecuteAsync(CancellationToken Token)
    {
        //TODO: Add Support Or Extra Types for Scheduled And Recurring And Other Job Types 
        if(JobID > 0)
        {
            await _Repository.DeleteByIdAsync(JobID);
        }
    }
}