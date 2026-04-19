using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

internal class ProcessingServer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private WorkerManager? _workerManager;

    private Scheduler? SchedulerProcess; 
    private CancellationTokenSource _shutdownCts;

    private Task SchedulerTask;
    
    private int WorkerCount; 

    public ProcessingServer(IServiceScopeFactory serviceScopeFactory, CancellationTokenSource shutdownCts, FastJobsOptions options)
    {
        _scopeFactory = serviceScopeFactory;
        _shutdownCts = shutdownCts;
        WorkerCount = options.WorkerCount;
        SchedulerProcess = new Scheduler(serviceScopeFactory);
    }

    public void StartProcessingJobs()
    {
        _workerManager = new WorkerManager(WorkerCount, _scopeFactory, _shutdownCts);
    }

    public void StartScheduler()
    {
       SchedulerTask =  Task.Run(async () => { 
            await SchedulerProcess.StartScheduler(_shutdownCts.Token);
        });
    }

    public async Task StopProcessingJobs()
    {
        await _workerManager?.StopAsync();
    }
}