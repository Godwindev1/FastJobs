


using System.Diagnostics;
using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

[Collection("FastJobServerTests")]
public class ExpiredJobsPruningStrategyTests : IClassFixture<ExpiryStrategyMariaDbFastJobsHostFixture>
{
    public ExpiryStrategyMariaDbFastJobsHostFixture _fixture;
    public IJobRepository JobRepository;

    public ITestOutputHelper Output;
    public ExpiredJobsPruningStrategyTests(ExpiryStrategyMariaDbFastJobsHostFixture fixture, ITestOutputHelper output)
    {
        Output = output;
        _fixture = fixture;
        JobRepository = fixture.Host.Services.GetService<IJobRepository>();
    }   

    [Fact]
    public async Task PruneByExpiry_RemovesExpiredJobs_WithinConfiguredInterval()
    {
        // Given
        const int jobCount = 10;
        for (int i = 0; i < jobCount; i++)
        {
            await FastJobServer.EnqueueJob<NoOpCompletingJob>()
                .SetExpiresAt(DateTime.UtcNow.AddSeconds(1))
                .Start();
        }

        // When
        var pruningInterval = TimeSpan.FromSeconds(5);
        var buffer = TimeSpan.FromSeconds(3);
        var pollInterval = TimeSpan.FromMilliseconds(500);
        var maxWait = pruningInterval + buffer;

        var stopwatch = Stopwatch.StartNew();
        bool allPruned = false;

        while (stopwatch.Elapsed < maxWait)
        {
            var remaining = await JobRepository.GetAllAsync();
            if (!remaining.Any())
            {
                allPruned = true;
                break;
            }

            await Task.Delay(pollInterval);
        }

        var res = await JobRepository.GetAllAsync();
        Output.WriteLine($"Number of Jobs Returned {res.Count}");

        // Then
        Assert.True(allPruned,
            $"Expected all {jobCount} expired jobs to be pruned within {maxWait.TotalSeconds}s, but some were still present.");
    }
}

