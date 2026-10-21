using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Serilog;

public static class JobTimer
{
    private static readonly Meter Meter = new("FastJobs.LoadTest");
    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("loadtest.job.duration", "ms");

    public static async Task RunAsync(string jobName, object? jobId, Microsoft.Extensions.Logging.ILogger logger , Func<Task> work)
    {
        var sw = Stopwatch.StartNew();
        try { await work(); }
        finally
        {
            sw.Stop();
            Duration.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("job", jobName));
            logger.LogInformation("{JobName} {JobId} completed in {ElapsedMs} ms", jobName, jobId, sw.ElapsedMilliseconds);
        }
    }
}