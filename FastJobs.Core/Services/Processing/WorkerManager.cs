using FastJobs;
using Microsoft.Extensions.DependencyInjection;

public class WorkerManager
{
    private readonly List<Task> _workers = new();
    private readonly CancellationTokenSource _shutdownCts;
    private readonly IServiceScopeFactory _scopeFactory;

    public WorkerManager(int workerCount, IServiceScopeFactory scopeFactory, CancellationTokenSource shutdownCts)
    {
        _shutdownCts = shutdownCts;
        _scopeFactory = scopeFactory;

        for (int i = 0; i < workerCount; i++)
        {
            var workerId = i;
            var worker = new Worker(workerId, _scopeFactory, _shutdownCts.Token);

            var taskResult = Task.Factory.StartNew(
                async (object? _) =>
                {
                    Thread.CurrentThread.Name = $"Creating Worker-Thread-{workerId}"; 
                    //Console.WriteLine(Thread.CurrentThread.Name);
                    await worker.Run();
                },
                state:             null,
                cancellationToken: _shutdownCts.Token,
                creationOptions:   TaskCreationOptions.LongRunning,
                scheduler:         TaskScheduler.Default
            ).Unwrap(); 

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
            await Task.WhenAll(_workers); 
        }
        catch (OperationCanceledException) when (_shutdownCts.IsCancellationRequested)
        {
            // Expected on shutdown
        }
    }
}