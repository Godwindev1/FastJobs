using System.Threading.Tasks;
using FastJobs.Persistence;
using Microsoft.Extensions.Logging;

namespace FastJobs;
public class ExpiredJobsPruningStrategy : ICleanupStrategy
{
    private readonly ILogger<ExpiredJobsPruningStrategy> _logger;
    private readonly IJobRepository _JobRepo;

    public ExpiredJobsPruningStrategy(IJobRepository jobRepository, ILogger<ExpiredJobsPruningStrategy> logger)
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