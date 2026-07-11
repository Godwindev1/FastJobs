using System.Diagnostics;
using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

[Collection("FastJobServerTests")]
public class CompletedJobsStrategyTest : IClassFixture<CompletedStrategyMariaDbFastJobsHostFixture>
{
    public CompletedStrategyMariaDbFastJobsHostFixture _fixture;
    public IJobRepository JobRepository;

    public ITestOutputHelper Output;
    public CompletedJobsStrategyTest(CompletedStrategyMariaDbFastJobsHostFixture fixture, ITestOutputHelper output)
    {
        Output = output;
        _fixture = fixture;
        JobRepository = fixture.Host.Services.GetService<IJobRepository>();
    }

    [Fact]
    public async Task PruneByCompletion_RemovesCompletedJobs_WithinConfiguredInterval()
    {
        // Given
        const int jobCount = 2;
        for (int i = 0; i < jobCount; i++)
        {
            await FastJobServer.EnqueueJob<NoOpCompletingJob>().Start();
            // no SetExpiresAt — this strategy prunes on Completed status,
        }

        // When
        var completionWindow = TimeSpan.FromSeconds(3);  // time for jobs to actually execute + reach Completed
        var pruningInterval = TimeSpan.FromSeconds(5);
        var buffer = TimeSpan.FromSeconds(3);
        var pollInterval = TimeSpan.FromMilliseconds(500);
        var maxWait = completionWindow + pruningInterval + buffer;

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

        foreach(var Job in res  )
        {
             Output.WriteLine($"Job {Job.Id} state  {Job.StateName}");
        }

        // Then
        Assert.True(allPruned,
            $"Expected all {jobCount} completed jobs to be pruned within {maxWait.TotalSeconds}s, but some were still present.");
    }
}