using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

internal class ProcessingServer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private WorkerManager? _workerManager;
    private CancellationTokenSource _shutdownCts;

    public ProcessingServer(IServiceScopeFactory serviceScopeFactory, CancellationTokenSource shutdownCts)
    {
        _scopeFactory = serviceScopeFactory;
        _shutdownCts = shutdownCts;
    }

    public void StartProcessingJobs(int workerCount = 1)
    {
        _workerManager = new WorkerManager(workerCount, _scopeFactory, _shutdownCts);
        _workerManager.Start();
    }

    public void StopProcessingJobs()
    {
        _workerManager?.Stop();
    }
}