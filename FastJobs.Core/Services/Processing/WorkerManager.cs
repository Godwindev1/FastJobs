using FastJobs;
using Microsoft.Extensions.DependencyInjection;
using FastJobs.Dashboard.Models;
using FastJobs.Dashboard.Models.Enums;

public class WorkerManager
{
    private readonly List<Task> _workers = new();
    private readonly CancellationTokenSource _shutdownCts;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<int, WorkerModel> _workerStates = new();
    private readonly object _lock = new();

    public WorkerManager(int workerCount, IServiceScopeFactory scopeFactory, CancellationTokenSource shutdownCts)
    {
        _shutdownCts = shutdownCts;
        _scopeFactory = scopeFactory;

        for (int i = 0; i < workerCount; i++)
        {
            var workerId = i;
            var worker = new Worker(workerId, _scopeFactory, _shutdownCts.Token, this);

            // Initialize worker state
            _workerStates[workerId] = new WorkerModel
            {
                WorkerId = $"worker-{workerId}",
                WorkerName = $"Worker-{workerId}",
                ServerName = Environment.MachineName,
                State = WorkerState.Sleeping,
                StartedAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow
            };

            var taskResult = Task.Factory.StartNew(
                async (object? _) =>
                {
                    Thread.CurrentThread.Name = $"Creating Worker-Thread-{workerId}";
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

    public int GetWorkerCount() => _workers.Count;

    public async Task<IReadOnlyList<WorkerModel>> GetWorkerModelsAsync()
    {
        lock (_lock)
        {
            return _workerStates.Values.ToList();
        }
    }

    // Method for workers to update their state
    public void UpdateWorkerState(int workerId, WorkerState state, string? currentJobId = null, string? currentJobName = null, DateTime? currentJobStartedAt = null)
    {
        lock (_lock)
        {
            if (_workerStates.TryGetValue(workerId, out var worker))
            {
                _workerStates[workerId] = worker with
                {
                    State = state,
                    LastHeartbeatAt = DateTime.UtcNow,
                    CurrentJobId = currentJobId,
                    CurrentJobName = currentJobName,
                    CurrentJobStartedAt = currentJobStartedAt
                };
            }
        }
    }

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