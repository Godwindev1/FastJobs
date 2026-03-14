using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FastJobs.SqlServer;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

//Processing Server WIll ALso Have to Run on its Own Dedicated Thread So it Does Not Block Client 
internal class ProcessingServer
{
    private ConcurrentQueue<Tuple<IBackGroundJob, SessionDatabaseLock>> _ScheduledJobs;   
    private readonly IServiceScopeFactory _scopeFactory;
    private WorkerManager? _workerManager;

    public ProcessingServer(IServiceScopeFactory serviceScopeFactory)
    {
        _scopeFactory = serviceScopeFactory;
        _ScheduledJobs = new ConcurrentQueue<Tuple<IBackGroundJob, SessionDatabaseLock>>();
    }

    private async Task ScheduleJob( Tuple<Queue, SessionDatabaseLock> JobDetails)
    {
        //Call JobReflection CLass And Store the BackgroundJob On Concurrent Queue Allong With its DBLock
        using ScopeManager scopeManager = new ScopeManager(_scopeFactory);
        IJobRepository _JobRepo = scopeManager.Resolve<IJobRepository>();

        var Job =   await _JobRepo.GetByIdAsync(JobDetails.Item1.JobId);
        _ScheduledJobs.Enqueue(
             new Tuple<IBackGroundJob, SessionDatabaseLock>(
                JobResolver.ResolveFireAndForgetJob(Job),
                JobDetails.Item2
            )
        ); 
    }


    public void StartProcessingJobs(int workerCount = 4)
    {
        _workerManager = new WorkerManager(workerCount, _scopeFactory);

        _workerManager.Start();
    }

    public void StopProcessingJobs()
    {
        _workerManager?.Stop();
    }
}