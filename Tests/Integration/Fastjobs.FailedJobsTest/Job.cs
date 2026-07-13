using FastJobs;
using Microsoft.Extensions.Logging;

public class FailJob : IBackGroundJob
{
    private readonly ILogger<FailJob> _logger;

    public FailJob(ILogger<FailJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] This Job Is Expected to Fail Continously ", Thread.CurrentThread.Name);

        throw new ArgumentException( "Fail test " ); 
    }
}