
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

    public EnqueueOptions<TJob> SetPriority(JobPriority priority)
    {
        _job.Priority = (int)priority;
        return this;
    }

    public EnqueueOptions<TJob>  SetMaxRetryCount(int retryCount)
    {
        _job.MaxRetries = retryCount;
        return this;
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        using var Scope = new ScopeManager(this._scopeFactory);

        var jobRepository          = Scope.Resolve<IJobRepository>();
        var stateHistoryRepository = Scope.Resolve<IStateHistoryRepository>();
        var queueRepository        = Scope.Resolve<IQueueRepository>();

        var jobId = await jobRepository.InsertAsync(_job, cancellationToken);

        var state = new State
        {
            JobID     = jobId,
            StateName = QueueStateTypes.Enqueued,
            Reason    = "Enqueued Job",
            data      = "Enqueued Job",
            CreatedAt = DateTime.UtcNow
        };

        var stateId = await stateHistoryRepository.InsertAsync(state, cancellationToken);

        await jobRepository.UpdateByIdAsync(jobId, "stateID = @stateID, StateName = @StateName",
            new Job { stateID = stateId, StateName = QueueStateTypes.Enqueued }, cancellationToken);

        await queueRepository.EnqueueAsync(new Queue
        {
            JobId       = jobId,
            QueueName   = FastJobConstants.DefaultQueue,
            Priority    = _job.Priority,
            ScheduledAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
}