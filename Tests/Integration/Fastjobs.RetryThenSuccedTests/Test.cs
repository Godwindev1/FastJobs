using FastJobs;
using FastJobs.AfterActions;
using FastJobs.Dashboard.Models;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
[CollectionDefinition("FastJobServerTests")]
public class ServerCollectionDefinition { }


[Collection("FastJobServerTests")]
public class RetryThenSucceedTest : IClassFixture<RetryThenSuccedFixture>
{
    private readonly RetryThenSuccedFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IJobRepository _repo;

    public RetryThenSucceedTest(RetryThenSuccedFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _repo = fixture.Host.Services.GetRequiredService<IJobRepository>();
        _output = output;
    }

    private static async Task WaitUntilAsync(
        Func<Task<int>> condition,
        int expected,
        TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        var actual = 0;
        while (DateTime.UtcNow - start < timeout)
        {
            actual = await condition();
            if (actual == expected)
                return;

            await Task.Delay(100); // poll interval
        }
    }

    [Fact]
    public async Task Job_Failing_Once_Should_Retry_And_Then_Succeed()
    {
        // Arrange
        var since = DateTime.UtcNow;

        // Act
        await FastJobServer.EnqueueJob<RetryThenSucceedJob>()
            .Start();

        await WaitUntilAsync(
            condition: () => _repo.CountCompletedSinceAsync(since),
            expected: 1,
            timeout: TimeSpan.FromSeconds(70));

        var allEntries = await _repo.GetAllAsync();

        foreach (var e in allEntries)
        {
            _output.WriteLine($"Name: {e.TypeName} state: {e.StateName} retryCount: {e.RetryCount}");
        }

        // Assert
        var single = allEntries.Where(x => x.StateName == QueueStateTypes.Completed).SingleOrDefault();
        Assert.NotNull(single);

        // Confirms it actually failed once before succeeding, not that it
        // just happened to complete on the first attempt.
        Assert.Equal(1, single.RetryCount);
    }
}