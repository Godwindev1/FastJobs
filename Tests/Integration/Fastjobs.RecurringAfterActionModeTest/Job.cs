using FastJobs;
using Microsoft.Extensions.Logging;

public class RunPerInstanceModeTestJob : IBackGroundJob
{
    private readonly ILogger<RunPerInstanceModeTestJob> _logger;

    public RunPerInstanceModeTestJob(ILogger<RunPerInstanceModeTestJob> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] RunPerInstance mode test job executed", Thread.CurrentThread.Name);
        return Task.CompletedTask;
    }
}

public class RunAfterFinalCompletionModeTestJob : IBackGroundJob
{
    private readonly ILogger<RunAfterFinalCompletionModeTestJob> _logger;

    public RunAfterFinalCompletionModeTestJob(ILogger<RunAfterFinalCompletionModeTestJob> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{Thread}] RunAfterFinalCompletion mode test job executed", Thread.CurrentThread.Name);
        return Task.CompletedTask;
    }
}

public static class PerInstanceAfterActionTracker
{
    public static int RunCount;
}

public class PerInstanceTrackingAfterAction : IAfterAction
{
    public Task ExecuteAsync(CancellationToken token)
    {
        Interlocked.Increment(ref PerInstanceAfterActionTracker.RunCount);
        return Task.CompletedTask;
    }
}

public static class FinalCompletionAfterActionTracker
{
    public static int RunCount;
}

public class FinalCompletionTrackingAfterAction : IAfterAction
{
    ILogger<FinalCompletionTrackingAfterAction> _logger;
    public FinalCompletionTrackingAfterAction(ILogger<FinalCompletionTrackingAfterAction> logger)
    {
        _logger = logger;
        _logger.LogInformation("Setup of Logger for FinalCompletionTrackingAction");
    }

    public Task ExecuteAsync(CancellationToken token)
    {
        Interlocked.Increment(ref FinalCompletionAfterActionTracker.RunCount);
        _logger.LogInformation("Increment FinalCompletionTrackingAction");
        return Task.CompletedTask;
    }
}
