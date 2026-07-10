// ── ChainJobBuilder ───────────────────────────────────────────────────────────

using System.Linq.Expressions;
using System.Text.Json;
using FastJobs.AfterActions;
using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

public class ChainJobBuilder
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<Job>            _steps = new();

    private DateTime  OptionalSchedule;
    private bool FirstJobisScheduled = false;

    internal ChainJobBuilder(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public ChainJobBuilder RunAt(DateTime scheduledTime)
    {
        if (scheduledTime.ToUniversalTime() <= DateTime.UtcNow)
        {
            throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledTime));
        }

        OptionalSchedule = scheduledTime;
        FirstJobisScheduled = true;
        return this;
    }

    public ChainJobBuilder WaitDelay(TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentException("Delay must be greater than zero.", nameof(delay));
        }
        OptionalSchedule = DateTime.UtcNow.Add(delay);
        FirstJobisScheduled = true;
        return this;
    } 


    // Called by ChainStepOptions to keep ThenRun chains working And on First Addition 
    public ChainStepOptions AddStep<TJob>() where TJob : class, IBackGroundJob
    {
        var job = FastJobServer.CreateJobTemplate<TJob>();
        job.JobType = JobTypes.ChainHead;
        _steps.Add(job);
        return new ChainStepOptions(job, this);
    }

    public ChainStepOptions AddStep(Expression<Action> actionExpression)
    {
        var job = FastJobServer.CreateJobTemplate(actionExpression);
        _steps.Add(job);
        return new ChainStepOptions(job, this);
    }

    public async Task EnqueueAsync(CancellationToken cancellationToken = default)
    {
        using var scope = new ScopeManager(_scopeFactory);

        var jobRepo          = scope.Resolve<IJobRepository>();
        var queueRepo        = scope.Resolve<IQueueRepository>();
        var scheduledJobRepository = scope.Resolve<IScheduledJobRepository>();
        var afterActionRepo  = scope.Resolve<IAfterActionRepository>();
        var stateHistoryRepo = scope.Resolve<IStateHistoryRepository>();

        // ── Step 1: Insert all jobs, none enqueued yet ────────────────────
        var jobIds = new List<long>();
        foreach (var job in _steps)
            jobIds.Add(await jobRepo.InsertAsync(job, cancellationToken));

        // ── Step 2: Wire ChainAfterActions front-to-back ──────────────────
        // Each job except the last gets a ChainAfterAction pointing at the next job
        for (int i = 0; i < _steps.Count - 1; i++)
        {
            var action = new AfterActionModel
            {
                TypeName = typeof(ChainAfterAction).AssemblyQualifiedName!,
                JobId    = jobIds[i],
                Payload  = JsonSerializer.Serialize(new ChainAfterActionPayload(jobIds[i + 1])),
                
                Retries      = 0,
                MaxRetries   = 3,
                ChainNo      = 1,
                LastActionID = 0,
                NextActionID = 0
            };

            var actionId = await afterActionRepo.InsertAsync(action, cancellationToken);

            await jobRepo.UpdateByIdAsync(
                jobIds[i],
                "AfterActionId = @AfterActionId",
                new Job { AfterActionId = actionId },
                cancellationToken
            );
        }


        if( FirstJobisScheduled )
        {
            // ── Step 3: Schedule only the first job through the normal pipeline ─
            var state = new State
            {
                JobID     = jobIds[0],
                StateName = QueueStateTypes.Scheduled,
                Reason    = $"Scheduled chain head #{jobIds[0]} of type {_steps[0].TypeName} To Start At { OptionalSchedule }",
                data      = "",
                CreatedAt = DateTime.UtcNow
            };

            var stateId = await stateHistoryRepo.InsertAsync(state, cancellationToken);

            await jobRepo.UpdateByIdAsync(
                jobIds[0],
                "stateID = @stateID, StateName = @StateName, ScheduledRunAt = @ScheduledRunAt",
                new Job { stateID = stateId, StateName = QueueStateTypes.Enqueued, ScheduledRunAt = OptionalSchedule  },
                cancellationToken
            );

            await scheduledJobRepository.InsertAsync(
                new ScheduledJobInfo {
                ScheduledTo = OptionalSchedule, 
                JobId = jobIds[0]
            }, cancellationToken);

        }
        else
        {
            // ── Step 3: Enqueue only the first job through the normal pipeline ─
            var state = new State
            {
                JobID     = jobIds[0],
                StateName = QueueStateTypes.Enqueued,
                Reason    = $"Enqueued chain head #{jobIds[0]} of type {_steps[0].TypeName}",
                data      = "",
                CreatedAt = DateTime.UtcNow
            };

            var stateId = await stateHistoryRepo.InsertAsync(state, cancellationToken);

            await jobRepo.UpdateByIdAsync(
                jobIds[0],
                "stateID = @stateID, StateName = @StateName, ScheduledRunAt = @ScheduledRunAt",
                new Job { stateID = stateId, StateName = QueueStateTypes.Enqueued, ScheduledRunAt = DateTime.UtcNow },
                cancellationToken
            );

            await queueRepo.EnqueueAsync(new Queue
            {
                JobId      = jobIds[0],
                QueueName  = FastJobConstants.DefaultQueue,
                Priority   = _steps[0].Priority,
                DequeuedAt = DateTime.UtcNow
            }, cancellationToken);
        }
       
    }
}