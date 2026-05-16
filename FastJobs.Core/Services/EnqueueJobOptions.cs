using Microsoft.Extensions.DependencyInjection;
using FastJobs.SqlServer;

namespace FastJobs;

public class EnqueueOptions<TJob> where TJob : class, IBackGroundJob
{
    private readonly Job _job;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<Action<AfterActionBuilder>> _afterActionConfigs = new();

    internal EnqueueOptions(Job job, IServiceScopeFactory factory)
    {
        _job          = job;
        _scopeFactory = factory;
    }

    public EnqueueOptions<TJob> SetPriority(JobPriority priority)
    {
        _job.Priority = (int)priority;
        return this;
    }

    public EnqueueOptions<TJob> SetMaxRetryCount(int retryCount)
    {
        _job.MaxRetries = retryCount;
        return this;
    }

    public EnqueueOptions<TJob> SetExpiresAt(DateTime expiresAt)
    {
        _job.ExpiresAt = expiresAt;
        return this;
    }

    public EnqueueOptions<TJob> AddAfterAction(Action<AfterActionBuilder> configure)
    {
        _afterActionConfigs.Add(configure);
        return this;
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        using var scope = new ScopeManager(_scopeFactory);

        var jobRepository           = scope.Resolve<IJobRepository>();
        var stateHistoryRepository  = scope.Resolve<IStateHistoryRepository>();
        var queueRepository         = scope.Resolve<IQueueRepository>();
        var afterActionRepository   = scope.Resolve<IAfterActionRepository>();

        var jobId = await jobRepository.InsertAsync(_job, cancellationToken);

        var state = new State
        {
            JobID     = jobId,
            StateName = QueueStateTypes.Enqueued,
            Reason    = $"Enqueued New Job #{jobId} of type {_job.MethodDeclaringTypeName}",
            data      = "",
            CreatedAt = DateTime.UtcNow
        };

        var stateId = await stateHistoryRepository.InsertAsync(state, cancellationToken);

        await jobRepository.UpdateByIdAsync(jobId, "stateID = @stateID, StateName = @StateName",
            new Job { stateID = stateId, StateName = QueueStateTypes.Enqueued }, cancellationToken);

        await queueRepository.EnqueueAsync(new Queue
        {
            JobId      = jobId,
            QueueName  = FastJobConstants.DefaultQueue,
            Priority   = _job.Priority,
            DequeuedAt = DateTime.UtcNow
        }, cancellationToken);

        await BuildAfterActionChainAsync(jobId, afterActionRepository, jobRepository, cancellationToken);
    }


    private async Task BuildAfterActionChainAsync(
        long jobId,
        IAfterActionRepository afterActionRepository,
        IJobRepository jobRepository,
        CancellationToken cancellationToken)
    {
        if (_afterActionConfigs.Count == 0) return;

        long lastInsertedId = 0;

        for (int i = 0; i < _afterActionConfigs.Count; i++)
        {
            var builder = new AfterActionBuilder();
            _afterActionConfigs[i](builder);

            var model = builder.Build(jobId, chainNo: i, lastActionId: lastInsertedId);
            var insertedId = await afterActionRepository.InsertAsync(model, cancellationToken);

            // Link the previous action's NextActionId to this one
            if (lastInsertedId != 0)
            {
                await afterActionRepository.UpdateByIdAsync(
                    lastInsertedId,
                    "NextActionId = @NextActionId",
                    new AfterActionModel { NextActionID = insertedId },
                    cancellationToken);
            }

            if(i == 0)
            {
                //Update Job With First ID
                await jobRepository.UpdateByIdAsync(jobId, "AfterActionId = @AfterActionId",
                new Job { AfterActionId = insertedId }, cancellationToken);
            }

            lastInsertedId = insertedId;
        }
    }
}