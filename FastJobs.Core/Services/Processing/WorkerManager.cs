using FastJobs;
using Microsoft.Extensions.DependencyInjection;
using FastJobs.SqlServer;

public class WorkerManager
{
    private readonly List<Task> _workers = new();

    private readonly List<Worker> _workerInstances = new();
    private readonly CancellationTokenSource _shutdownCts;
    private readonly IServiceScopeFactory _scopeFactory;

    public WorkerManager(int workerCount, IServiceScopeFactory scopeFactory, CancellationTokenSource shutdownCts)
    {
        _shutdownCts = shutdownCts;
        _scopeFactory = scopeFactory;



        for (int i = 0; i < workerCount; i++)
        {
            var workerId = i;
            string workerName = $"Worker-{workerId}";
            var worker = new Worker(workerId, _scopeFactory, _shutdownCts.Token);

            var taskResult = Task.Factory.StartNew(
                async (object? _) =>
                {
                    await worker.Run();
                },
                state:             null,
                cancellationToken: _shutdownCts.Token,
                creationOptions:   TaskCreationOptions.LongRunning,
                scheduler:         TaskScheduler.Default
            ).Unwrap(); 

            _workerInstances.Add(worker);
            _workers.Add(taskResult);
        }
    }

    // Workers already running 
    public Task[] GetWorkerTasks() => _workers.ToArray();

    public async Task StopAsync()
    {
        _shutdownCts.Cancel();

        try
        {
            _workerInstances.ForEach(async w => await w.DisposeAsync());
            await Task.WhenAll(_workers); 
        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            // Expected on shutdown
        }
    }
}