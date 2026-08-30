using FastJobs;
using Microsoft.Extensions.Logging;

public class BasicJob : IBackGroundJob
{
    private readonly ILogger<BasicJob> _logger;

    public BasicJob(ILogger<BasicJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] This Is a Basic Logging job", Thread.CurrentThread.Name);
    }
}