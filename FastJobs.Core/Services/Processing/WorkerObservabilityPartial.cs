
using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;
public partial class Worker
{
private ScopeManager? _observabilityScope;
    private IWorkerRepository? _workerRepo;
    private FSTJBS_Worker? _workerRecord;

    public async Task<FSTJBS_Worker> WorkerObservability()
    {
        // Scope lives on the class, not disposed when method returns
        _observabilityScope = new ScopeManager(serviceScopeFactory);
        _workerRepo = _observabilityScope.Resolve<IWorkerRepository>();

        _workerRecord = new FSTJBS_Worker
        {
            WorkerName    = $"Worker-{_workerId:N}",
            ThreadName    = Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString(),
            StartedAt     = DateTime.UtcNow,
            isSleeping    = false,
            isCrashed     = false,
            LastHeartbeat = DateTime.UtcNow
        };

        long workerId = await _workerRepo.InsertAsync(_workerRecord, _shutdownToken);
        _workerRecord.Id = workerId;
        SetDBWorkerID(workerId);

        _heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(_shutdownToken);
        _ = Task.Run(async () =>
        {
            while (!_heartbeatCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.DefaultWOrkerHeartbeatIntervalSeconds), _heartbeatCts.Token);
                    _workerRecord.LastHeartbeat = DateTime.UtcNow;
                    await _workerRepo.UpdateAsync(_workerRecord, _heartbeatCts.Token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Heartbeat] Failed: {ex.Message}");
                }
            }
        }, _heartbeatCts.Token);

        return _workerRecord;
    }

    public async ValueTask DisposeAsync()
    {
        if (_heartbeatCts != null)
        {
            await _heartbeatCts.CancelAsync();
            _heartbeatCts.Dispose();
        }

        _observabilityScope?.Dispose();
    }
}