
using FastJobs;
using Microsoft.Extensions.Logging;

public class RetryThenSucceedJob : IBackGroundJob
{
    private readonly ILogger<RetryThenSucceedJob> _logger;
    private readonly JobContext context;

    public RetryThenSucceedJob(ILogger<RetryThenSucceedJob> logger, IJobContext jobContext)
    {
        context = (JobContext)jobContext;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] Retry-then-succeed test  ", Thread.CurrentThread.Name);

        if(context.CurrentJob.RetryCount < 1)
        {
            throw new ArgumentException( "Fail On First trial " ); 
        }
        else
        {
            _logger.LogInformation("[{Thread}] Complete On Second Try  ", Thread.CurrentThread.Name);
        }
    }
}
