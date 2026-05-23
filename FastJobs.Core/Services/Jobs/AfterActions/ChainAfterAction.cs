
using System.Text.Json;
using FastJobs;
using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace Fastjobs.AfterActions;

internal sealed record ChainAfterActionPayload(long NextJobId);

public class ChainAfterAction : IAfterAction
{
    private readonly ChainAfterActionPayload _payload;
    private readonly IServiceScopeFactory    _scopeFactory;

    public ChainAfterAction(AfterActionModel model, IServiceScopeFactory scopeFactory)
    {
        _payload      = JsonSerializer.Deserialize<ChainAfterActionPayload>(model.Payload!)!;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(CancellationToken token)
    {
        using var scope = new ScopeManager(_scopeFactory);
        var queueRepo   = scope.Resolve<IQueueRepository>();

        await queueRepo.EnqueueAsync(new Queue { JobId = _payload.NextJobId });
    }
}