using FastJobs;
using Microsoft.Extensions.Logging;

public class TerminateExceptionJob : IBackGroundJob
{
    private readonly ILogger<TerminateExceptionJob> _logger;

    public TerminateExceptionJob(ILogger<TerminateExceptionJob> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] Exception Termination Test Started Processing", Thread.CurrentThread.Name);

        throw new TerminateJobException ("Test for intentional job Termination by TerminateRetryException ");

        _logger.LogInformation("[{Thread}] Processing Should Never reach this point", Thread.CurrentThread.Name);
    }
}