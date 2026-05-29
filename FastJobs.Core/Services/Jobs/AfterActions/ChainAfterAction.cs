
using System.Text.Json;
using FastJobs;
using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs.AfterActions;

internal sealed record ChainAfterActionPayload(long NextJobId);

public class ChainAfterAction : IAfterAction
{
    private readonly ChainAfterActionPayload _payload;
    private readonly IServiceScopeFactory    _scopeFactory;

    public ChainAfterAction(IAfterActionContext Context, IServiceScopeFactory scopeFactory)
    {
        _payload      = JsonSerializer.Deserialize<ChainAfterActionPayload>(Context.Model.Payload!)!;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(CancellationToken token)
    {
        using var scope = new ScopeManager(_scopeFactory);
        var queueRepo   = scope.Resolve<IQueueRepository>();

        await queueRepo.EnqueueAsync(new Queue { QueueName = QueueNames.Default, JobId = _payload.NextJobId, DequeuedAt = DateTime.UtcNow, Priority = (int)JobPriority.Medium });
    }
}