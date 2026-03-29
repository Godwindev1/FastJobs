using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

internal class ProcessingServer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private WorkerManager? _workerManager;
    private CancellationTokenSource _shutdownCts;
    
    private int WorkerCount; 

    public ProcessingServer(IServiceScopeFactory serviceScopeFactory, CancellationTokenSource shutdownCts, FastJobsOptions options)
    {
        _scopeFactory = serviceScopeFactory;
        _shutdownCts = shutdownCts;
        WorkerCount = options.WorkerCount;
    }

    public void StartProcessingJobs()
    {
        _workerManager = new WorkerManager(WorkerCount, _scopeFactory, _shutdownCts);
    }

    public async Task StopProcessingJobs()
    {
        await _workerManager?.StopAsync();
    }
}