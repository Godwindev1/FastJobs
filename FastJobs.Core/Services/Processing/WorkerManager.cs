using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

//PENDING MODIFICATIONS
public class WorkerManager
{
    private readonly List<Thread> _workers = new();
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public WorkerManager(int workerCount, IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        for (int i = 0; i < workerCount; i++)
        {
            var worker = new Worker(i, _scopeFactory, _shutdownCts.Token);

            var thread = new Thread(() => { worker.Run().GetAwaiter().GetResult(); } )
            {
                IsBackground = false
            };

            _workers.Add(thread);
        }
    }

    public void Start()
    {
        foreach (var worker in _workers)
        {
            worker.Start();
        }
    }

    public void Stop()
    {
        _shutdownCts.Cancel();

        foreach (var worker in _workers)
        {
            worker.Join();
        }
    }
}