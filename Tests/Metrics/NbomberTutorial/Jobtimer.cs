using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

public static class JobTimer
{
    private static readonly Meter Meter = new("FastJobs.LoadTest");
    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("loadtest.job.duration", "ms");

    public static void Record(string jobName, double elapsedMs) =>
        Duration.Record(elapsedMs, new KeyValuePair<string, object?>("job", jobName));

    public static async Task RunAsync(string jobName, object? jobId, ILogger logger, Func<Task> work)
    {
        var sw = Stopwatch.StartNew();
        try { await work(); }
        finally
        {
            sw.Stop();
            Record(jobName, sw.Elapsed.TotalMilliseconds);
            logger.LogInformation("{JobName} {JobId} completed in {ElapsedMs} ms", jobName, jobId, sw.ElapsedMilliseconds);
        }
    }
}