// ValidateOrderJob.cs
using FastJobs;
using Microsoft.Extensions.Logging;
public class NoOpCompletingJob : IBackGroundJob
{
    private readonly ILogger<NoOpCompletingJob> _logger;

    public NoOpCompletingJob(ILogger<NoOpCompletingJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] NoOpCompletingJob executed", Thread.CurrentThread.Name);
        return Task.CompletedTask;
    }
}