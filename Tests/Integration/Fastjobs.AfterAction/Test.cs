using FastJobs;
using FastJobs.AfterActions;
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
    public async Task Job_Throwing_TerminateJobException_Should_Be_Deleted_By_AfterAction()
    {
        // Arrange
        var since = DateTime.UtcNow;

        // Act
        await FastJobServer.EnqueueJob<DeleteAfterActionTestJob>()
            .AddAfterAction(x => x.WithType<DeleteAfterAction>())
            .Start();

        await WaitUntilAsync(
            condition: async () => (await _repo.GetAllAsync()).Count,
            expected: 0,
            timeout: TimeSpan.FromSeconds(50));

        var allEntries = await _repo.GetAllAsync();

        foreach (var e in allEntries)
        {
            _output.WriteLine($"Name: {e.TypeName} state: {e.StateName}");
        }

        // Assert
        var All = await _repo.GetAllAsync();
        var count = All.Count;
        Assert.Equal(0, count);
    }
}