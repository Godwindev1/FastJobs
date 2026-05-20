using Microsoft.Extensions.DependencyInjection;
using FastJobs.SqlServer;

namespace FastJobs;

//TODO: Implement This With proper pattern of using Links to The Actual Job classes, And proper After Action usage 


/// <summary>
/// Fluent builder for a chained job sequence.
/// The first job (TJob) is the chain head — it gets enqueued normally.
/// Each .Next() call registers a subsequent step stored via AfterAction.
/// When the worker picks up the head job, it executes the full chain
/// inline (worker-hijack model) before returning the worker to the pool.
/// </summary>
public class ChainedJobOptions<TJob> where TJob : class, IBackGroundJob
{
    private readonly Job _job;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<Action<AfterActionBuilder>> _chainSteps = new();

    internal ChainedJobOptions(Job job, IServiceScopeFactory factory)
    {
        _job          = job;
        _scopeFactory = factory;
    }

    // -------------------------------------------------------------------------
    // Chain step registration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds the next job in the chain.
    /// Steps are executed sequentially on the same worker as the head job.
    /// </summary>
    public ChainedJobOptions<TJob> Next(Action<AfterActionBuilder> configure)
    {
        _chainSteps.Add(configure);
        return this;
    }

    // -------------------------------------------------------------------------
    // Head job configuration  (mirrors EnqueueOptions fluent surface)
    // -------------------------------------------------------------------------

    public ChainedJobOptions<TJob> SetPriority(JobPriority priority)
    {
        _job.Priority = (int)priority;
        return this;
    }

    public ChainedJobOptions<TJob> SetMaxRetryCount(int retryCount)
    {
        _job.MaxRetries = retryCount;
        return this;
    }

    public ChainedJobOptions<TJob> SetExpiresAt(DateTime expiresAt)
    {
        _job.ExpiresAt = expiresAt;
        return this;
    }

    // -------------------------------------------------------------------------
    // Terminal: persist and enqueue
    // -------------------------------------------------------------------------

    /// <summary>
    /// Persists the chain head and all registered steps, then enqueues the head.
    /// The worker will execute every step inline before releasing.
    /// </summary>
    public async Task Start(CancellationToken cancellationToken = default)
    {
        using var scope = new ScopeManager(_scopeFactory);

        var jobRepository          = scope.Resolve<IJobRepository>();
        var stateHistoryRepository = scope.Resolve<IStateHistoryRepository>();
        var queueRepository        = scope.Resolve<IQueueRepository>();
        var afterActionRepository  = scope.Resolve<IAfterActionRepository>();

        // Mark as chain head so the worker knows to run steps inline
        _job.JobType = JobTypes.ChainHead;

        var jobId = await jobRepository.InsertAsync(_job, cancellationToken);

        var state = new State
        {
            JobID     = jobId,
            StateName = QueueStateTypes.Enqueued,
            Reason    = $"Enqueued chain head #{jobId} ({_chainSteps.Count} step(s) follow) of type {_job.MethodDeclaringTypeName}",
            data      = $"ChainLength={_chainSteps.Count + 1}",
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

        // Persist chain steps as a linked AfterAction list on the head job.
        // Reuses the exact same infrastructure as EnqueueOptions/ScheduledJobOptions.
        await BuildChainStepsAsync(jobId, afterActionRepository, jobRepository, cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Internal: identical logic to BuildAfterActionChainAsync in sibling classes
    // -------------------------------------------------------------------------

    private async Task BuildChainStepsAsync(
        long headJobId,
        IAfterActionRepository afterActionRepository,
        IJobRepository jobRepository,
        CancellationToken cancellationToken)
    {
        if (_chainSteps.Count == 0) return;

        long lastInsertedId = 0;

        for (int i = 0; i < _chainSteps.Count; i++)
        {
            var builder = new AfterActionBuilder();
            _chainSteps[i](builder);

            var model      = builder.Build(headJobId, chainNo: i, lastActionId: lastInsertedId);
            var insertedId = await afterActionRepository.InsertAsync(model, cancellationToken);

            // Link previous step → this step
            if (lastInsertedId != 0)
            {
                await afterActionRepository.UpdateByIdAsync(
                    lastInsertedId,
                    "NextActionId = @NextActionId",
                    new AfterActionModel { NextActionID = insertedId },
                    cancellationToken);
            }

            // Point the head job at step zero
            if (i == 0)
            {
                await jobRepository.UpdateByIdAsync(headJobId, "AfterActionId = @AfterActionId",
                    new Job { AfterActionId = insertedId }, cancellationToken);
            }

            lastInsertedId = insertedId;
        }
    }
}