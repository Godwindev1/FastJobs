using FastJobs;
using Microsoft.Extensions.Logging;

public class DeleteAfterActionTestJob : IBackGroundJob
{
    private readonly ILogger<DeleteAfterActionTestJob> _logger;

    public DeleteAfterActionTestJob(ILogger<DeleteAfterActionTestJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] Delete After Action Test Job Simply Doing it Thing ", Thread.CurrentThread.Name);
        _logger.LogInformation("[{Thread}] Job Should Be Deleted After This ", Thread.CurrentThread.Name);
    }
}