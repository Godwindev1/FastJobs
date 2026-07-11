
using FastJobs;
using Microsoft.Extensions.Logging;

public class MisfireTestJob : IBackGroundJob
{
    private readonly ILogger<MisfireTestJob> _logger;

    public MisfireTestJob(ILogger<MisfireTestJob> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] MisfireTestJob started, running long enough to overrun its own interval", Thread.CurrentThread.Name);

        // Deliberately longer than the recurring interval below, so the next
        // scheduled fire is missed while this run is still in progress.
        await Task.Delay(TimeSpan.FromSeconds(6), cancellationToken);

        _logger.LogInformation("MisfireTestJob finished");
    }
}