using Microsoft.Extensions.Hosting;

public class MisfireDetectorService : BackgroundService
{
    private readonly RecurringMisfireDetector _misfireDetector;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _MisfireDetectorStartupDelay = TimeSpan.FromMinutes(1);

    public MisfireDetectorService(RecurringMisfireDetector misfireDetector, FastJobsOptions options)
    {
        _misfireDetector = misfireDetector;
        _MisfireDetectorStartupDelay = options.MisfireDetectorStartupDelay;
        _interval = options.MisfireDetectorInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        //initial Delay To make Sure all other Systems Are Running Before this Starts 
         await Task.Delay(_MisfireDetectorStartupDelay, ct);

        using var timer = new PeriodicTimer(_interval);

        while (await timer.WaitForNextTickAsync(ct))
        {
            await _misfireDetector.DetectAndHandleAsync(ct);
        }
    }
}