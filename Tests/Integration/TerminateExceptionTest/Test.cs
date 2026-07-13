using FastJobs;
using FastJobs.Dashboard.Models;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
[CollectionDefinition("FastJobServerTests")]
public class ServerCollectionDefinition { }

[Collection("FastJobServerTests")]
public class TerminateExceptionJobTests : IClassFixture<TerminateJobTestFixture>
{
    private readonly TerminateJobTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IJobRepository _repo;

    public TerminateExceptionJobTests(TerminateJobTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _repo = fixture.Host.Services.GetService<IJobRepository>();
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
    public async Task Job_Throwing_TerminateJobException_Should_Be_Recorded_As_Failed()
    {
        // Arrange
        var since = DateTime.UtcNow;

        // Act
        await FastJobServer.EnqueueJob<TerminateExceptionJob>()
            .Start();

        await WaitUntilAsync(
            () => _repo.CountFailedSinceAsync(since),
            expected: 1,
            timeout: TimeSpan.FromSeconds(50));


        var allEntries = await _repo.GetAllAsync();
        var failedJob = allEntries
        .Where(e => e.StateName == QueueStateTypes.Failed) 
        .SingleOrDefault();

        foreach (var e in allEntries)
        {
            _output.WriteLine($"Name: {e.TypeName} state: {e.StateName}");
        }

        // Assert
        var count = await _repo.CountFailedSinceAsync(since);
        Assert.Equal(1, count);
        Assert.NotNull(failedJob);
        Assert.Equal(QueueStateTypes.Failed, failedJob.StateName);
    }
}