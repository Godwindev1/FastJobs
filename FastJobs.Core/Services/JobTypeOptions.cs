
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs {
  public class EnqueueOptions<TJob> where TJob : class, IBackGroundJob
{
    private readonly Job _job;
    private readonly IServiceScopeFactory _scopeFactory;

    internal EnqueueOptions(Job job, IServiceScopeFactory factory)
    {
        _job = job;
        _scopeFactory = factory;
    }

    /// <summary>
    /// TODO:
    /// Set Job Delay period before it gets executed
    /// </summary>
    public EnqueueOptions<TJob> WithDelay(TimeSpan delay)
    {
        //NB: This Below Field Does not Exist IN the Job class or DB yet 
        //_job.ScheduledAt = DateTime.UtcNow.Add(delay);
        return this;
    }

    public async Task Start()
    {
        using var Scope = new ScopeManager(this._scopeFactory);

        var jobRepository          = Scope.Resolve<IJobRepository>();
        var stateHistoryRepository = Scope.Resolve<IStateHistoryRepository>();
        var queueRepository        = Scope.Resolve<IQueueRepository>();

        var jobId = await jobRepository.InsertAsync(_job);

        var state = new State
        {
            JobID     = jobId,
            StateName = QueueStateTypes.Enqueued,
            Reason    = "Enqueued Job",
            data      = "Enqueued Job",
            CreatedAt = DateTime.UtcNow
        };

        var stateId = await stateHistoryRepository.InsertAsync(state);

        await jobRepository.UpdateByIdAsync(jobId, "stateID = @stateID, StateName = @StateName",
            new Job { stateID = stateId, StateName = QueueStateTypes.Enqueued });

        await queueRepository.EnqueueAsync(new Queue
        {
            JobId       = jobId,
            QueueName   = FastJobConstants.DefaultQueue,
            Priority    = _job.Priority,
            ScheduledAt = DateTime.UtcNow
        });
    }
}
}