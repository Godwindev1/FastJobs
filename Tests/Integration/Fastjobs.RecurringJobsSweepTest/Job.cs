using FastJobs;
using Microsoft.Extensions.Logging;

public class RecurringJobSweepTestJob : IBackGroundJob
{
    private readonly ILogger<RecurringJobSweepTestJob> _logger;

    public RecurringJobSweepTestJob(ILogger<RecurringJobSweepTestJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] recurring Sweep Test Job ", Thread.CurrentThread.Name);
    }
}