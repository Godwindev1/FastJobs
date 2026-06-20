// ── ChainStepOptions ──────────────────────────────────────────────────────────
// Mirrors EnqueueOptions but owns one chain step and delegates chain
// continuation back to the builder

using System.Linq.Expressions;
using FastJobs;
using FastJobs.SqlServer;

public class ChainStepOptions
{
    private readonly Job             _job;
    private readonly ChainJobBuilder _builder;

    internal ChainStepOptions(Job job, ChainJobBuilder builder)
    {
        _job     = job;
        _builder = builder;
    }

    public ChainStepOptions WithMisfirePolicy(MisfirePolicy policy)
    {
        _job.misfirePolicy = (int)policy;
        return this;
    }


    public ChainStepOptions SetPriority(JobPriority priority)
    {
        _job.Priority = (int)priority;
        return this;
    }

    public ChainStepOptions SetMaxRetryCount(int retryCount)
    {
        _job.MaxRetries = retryCount;
        return this;
    }

    public ChainStepOptions SetExpiresAt(DateTime expiresAt)
    {
        _job.ExpiresAt = expiresAt.ToUniversalTime();
        return this;
    }

    // Returns a new ChainStepOptions for the next step — builder owns the list
    public ChainStepOptions ThenRun<TJob>() where TJob : class, IBackGroundJob =>
        _builder.AddStep<TJob>();

    public ChainStepOptions ThenRun(Expression<Action> actionExpression) => 
        _builder.AddStep(actionExpression);

    public Task EnqueueAsync(CancellationToken cancellationToken = default) =>
        _builder.EnqueueAsync(cancellationToken);
}