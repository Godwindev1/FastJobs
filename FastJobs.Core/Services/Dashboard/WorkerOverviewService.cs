
using FastJobs;
using FastJobs.Dashboard.Models;
using FastJobs.SqlServer;

namespace FastJobs.Dashboard;

public class WorkerOverviewService
{
    private readonly IWorkerRepository _workerRepository;

    public WorkerOverviewService( IWorkerRepository workerRepository)
    {
        _workerRepository = workerRepository;
    }

    public WorkerModel MapToModel(FSTJBS_Worker worker)
    {
        WorkerState state = WorkerState.Active;
        if(worker.isSleeping)
        {
            state = WorkerState.Sleeping;
        }
        else
        {
            state = WorkerState.Active;
        }

        if(worker.isCrashed)
        {
            state = WorkerState.Dead;
        }

        return new WorkerModel
        {
            WorkerId = worker.Id.ToString(),
            WorkerName = worker.WorkerName,
            State = state,
            StartedAt = worker.StartedAt,
            LastHeartbeatAt = worker.LastHeartbeat ?? DateTime.Now, // Fallback to now if null  
          
        };
    }

    public async Task<WorkerOverviewModel> GetWorkersOverviewAsync()
    {
        var summary = new WorkerOverviewModel
        {
            ActiveWorkers = (await _workerRepository.GetActiveAsync()).Count,
            SleepingWorkers = (await _workerRepository.GetSleepingAsync()).Count,
            DeadWorkers = (await _workerRepository.GetDeadWorkersAsync()).Count,
            GeneratedAt = DateTime.UtcNow,
        };

        summary.TotalWorkers =  summary.ActiveWorkers + summary.SleepingWorkers + summary.DeadWorkers;

        var Workers = await _workerRepository.GetAllAsync();

        foreach(var worker in Workers)
        {
            summary.Workers.Add(MapToModel(worker));
        }

        return summary;
    }

}