using FastJobs;
using Microsoft.Extensions.DependencyInjection;
using FastJobs.SqlServer;
using Microsoft.Extensions.Logging;

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

         using var Scopemanager = new ScopeManager(scopeFactory);
         var repo = Scopemanager.Resolve<IWorkerRepository>();

         var _Logger = Scopemanager.Resolve<ILogger<WorkerManager>>();

         repo.TruncateAsync()
         .GetAwaiter()
         .GetResult();

        for (int i = 0; i < workerCount; i++)
        {
            var workerId = i;
            string workerName = $"Worker-{workerId}";

            _Logger.LogInformation("New Worker Created Worker: {WorkerName}", workerName);

            var worker = new Worker(workerId,workerName, _scopeFactory, _shutdownCts.Token);

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