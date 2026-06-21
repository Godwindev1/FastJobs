using FastJobs.SqlServer;
using Microsoft.Extensions.Logging;

namespace FastJobs;
public class CompletedJobsPruningStrategy : ICleanupStrategy
{
    private readonly ILogger<CompletedJobsPruningStrategy> _logger;
    private readonly IJobRepository _JobRepo;

    public CompletedJobsPruningStrategy(IJobRepository jobRepository, ILogger<CompletedJobsPruningStrategy> logger)
    {
        _JobRepo = jobRepository;
        _logger = logger;
    }

    public async Task  Clean(CancellationToken cancellationToken)
    {   
        var AffectedRows = await _JobRepo.PruneCompletedJobs(cancellationToken);
        _logger.LogInformation("Pruned {AffectedRows} Completed Jobs", AffectedRows);
    } 
}