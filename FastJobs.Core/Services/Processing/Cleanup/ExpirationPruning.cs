using System.Threading.Tasks;
using FastJobs.Persistence;
using Microsoft.Extensions.Logging;

namespace FastJobs;
public class ExpiredJobsPruningStrategy : ICleanupStrategy
{
    private readonly ILogger<CompletedJobsPruningStrategy> _logger;
    private readonly IJobRepository _JobRepo;

    public ExpiredJobsPruningStrategy(IJobRepository jobRepository, ILogger<CompletedJobsPruningStrategy> logger)
    {
        _JobRepo = jobRepository;
        _logger = logger;
    }

    public async Task  Clean(CancellationToken cancellationToken)
    {   
        var AffectedRows = await _JobRepo.PruneExpiredJobs(cancellationToken);
        _logger.LogInformation("Pruned {AffectedRows} Expired Jobs", AffectedRows);
    } 
}