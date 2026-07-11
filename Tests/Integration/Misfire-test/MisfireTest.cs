using System.Diagnostics;
using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

[Collection("FastJobServerTests")]
public class MisfireTest : IClassFixture<MisfireTestFixture>
{
    public MisfireTestFixture _fixture;
    public IJobRepository JobRepository;
    public ITestOutputHelper Output;

    public MisfireTest(MisfireTestFixture fixture, ITestOutputHelper output)
    {
        Output = output;
        _fixture = fixture;
        JobRepository = fixture.Host.Services.GetService<IJobRepository>();
    }
    
    [Theory]
    [InlineData(MisfirePolicy.FireOnce)]
    [InlineData(MisfirePolicy.Skip)]
    [InlineData(MisfirePolicy.Smart)]
    public async Task MisfireDetector_DetectsMisfiredRecurringJob_WithinConfiguredInterval(MisfirePolicy policy)
    {
        // Given
        var startTime = DateTime.UtcNow.AddSeconds(1);
        var interval = TimeSpan.FromSeconds(2); // shorter than the job's 6s runtime — guarantees an overrun

        await FastJobServer
            .AddRecurringJob<MisfireTestJob>()
            .WithInterval(interval, startTime)
            .SetMaxRetryCount(5)
            .WithMisfirePolicy(policy)
            .Start();

        // When
        var misfireThreshold = TimeSpan.FromSeconds(1);  // must match FastJobsOptions.MisfireThreshold
        var startupDelay = TimeSpan.FromSeconds(2);      // matches MisfireDetectorStartupDelay
        var detectorInterval = TimeSpan.FromSeconds(4);  // matches MisfireDetectorInterval
        var buffer = TimeSpan.FromSeconds(3);
        var pollInterval = TimeSpan.FromMilliseconds(500);

        var maxWait = (startTime.AddSeconds(interval.TotalSeconds) - DateTime.UtcNow)
                    + misfireThreshold + startupDelay + detectorInterval + buffer;

        var stopwatch = Stopwatch.StartNew();
        bool misfireDetected = false;

        while (stopwatch.Elapsed < maxWait)
        {
            var cutoff = DateTime.UtcNow - misfireThreshold; // replicate the detector's own calculation
            var misfiredJobs = await JobRepository.GetMisfiredJobsAsync(cutoff);
            Output.WriteLine($"Cutoff: {cutoff:O}, Number of Jobs Returned: {misfiredJobs.Count}");

            if (misfiredJobs.Any())
            {
                misfireDetected = true;
                break;
            }

            await Task.Delay(pollInterval);
        }

        // Then
        Assert.True(misfireDetected,
            $"Expected the misfired recurring job to be detected within {maxWait.TotalSeconds}s, but none were found.");
    }




}