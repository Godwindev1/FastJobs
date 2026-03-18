using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FastJobs.SqlServer;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

internal class ProcessingServer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private WorkerManager? _workerManager;

    public ProcessingServer(IServiceScopeFactory serviceScopeFactory)
    {
        _scopeFactory = serviceScopeFactory;
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