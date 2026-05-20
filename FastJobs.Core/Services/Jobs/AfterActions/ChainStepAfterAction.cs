using System.Text.Json;
using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs.AfterActions;

//TODO: Implement This With proper pattern of using Links to The Actual Job class, job data is not stored in here
public class ChainStepAfterAction : IAfterAction
{
    private readonly long _firstActionId;
    private readonly IServiceScopeFactory _scopeFactory;

    public ChainStepAfterAction(IJobContext job, IServiceScopeFactory scopeFactory)
    {
        _firstActionId = job.CurrentJob.AfterActionId ?? 0;
        _scopeFactory  = scopeFactory;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_firstActionId == 0) return;

        using var scope              = new ScopeManager(_scopeFactory);
        var afterActionRepository    = scope.Resolve<IAfterActionRepository>();

        var currentActionId = _firstActionId;

        while (currentActionId != 0)
        {
            var action = await afterActionRepository.GetByIdAsync(currentActionId, cancellationToken);

            if (action is null) break;

            await RunStepAsync(action, cancellationToken);

            currentActionId = action.NextActionID;
        }
    }

    // -------------------------------------------------------------------------
    // Private
    // -------------------------------------------------------------------------

    private async Task RunStepAsync(AfterActionModel action, CancellationToken cancellationToken)
    {
       /* var jobType = Type.GetType(action.TypeName)
            ?? throw new InvalidOperationException(
                $"Chain step {action.ChainNo}: cannot resolve type '{action.TypeName}'.");

        using var scope = _scopeFactory.CreateScope();

        var job = scope.ServiceProvider.GetRequiredService(jobType) as IBackGroundJob
            ?? throw new InvalidOperationException(
                $"Chain step {action.ChainNo}: '{jobType.Name}' is not registered as IBackGroundJob.");

        if (!string.IsNullOrEmpty(action.SerializedArguments))
            HydrateArguments(job, action.SerializedArguments);

        await job.ExecuteAsync(cancellationToken); */
    }

    /// <summary>
    /// Deserializes stored JSON into the job's Arguments property (if present).
    /// Convention: the job exposes a settable property named Arguments.
    /// </summary>
    private static void HydrateArguments(IBackGroundJob job, string serializedArguments)
    {
        var prop = job.GetType().GetProperty("Arguments");

        if (prop is null || !prop.CanWrite) return;

        var deserialized = JsonSerializer.Deserialize(serializedArguments, prop.PropertyType);
        prop.SetValue(job, deserialized);
    }
}