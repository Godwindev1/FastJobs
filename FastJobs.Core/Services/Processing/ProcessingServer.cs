using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

internal class ProcessingServer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private WorkerManager? _workerManager;

    private Scheduler? SchedulerProcess; 
    private RecurringScheduler? RecurringSchedulerProcess;
    private CancellationTokenSource _shutdownCts;

    private Task SchedulerTask;
    private Task RecurringSchedulerTask;
    
    private int WorkerCount; 

    public ProcessingServer(IServiceScopeFactory serviceScopeFactory, CancellationTokenSource shutdownCts, FastJobsOptions options)
    {
        _scopeFactory = serviceScopeFactory;
        _shutdownCts = shutdownCts;
        WorkerCount = options.WorkerCount;
        SchedulerProcess = new Scheduler(serviceScopeFactory);
        RecurringSchedulerProcess = new RecurringScheduler(serviceScopeFactory, NotifyScheduledJobAdded);
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

       RecurringSchedulerTask = Task.Run(async () => {
            await RecurringSchedulerProcess.StartAsync(_shutdownCts.Token);
       });
    }

    public void NotifyScheduledJobAdded()
    {
        SchedulerProcess.NotifyJobAdded();
    }

    public void NotifyRecurringJobAdded()
    {
        RecurringSchedulerProcess.NotifyJobAdded();
    }

    public async Task StopProcessingJobs()
    {
        await _workerManager?.StopAsync();
    }
}