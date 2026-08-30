using FastJobs;
using FastJobs.AfterActions;
using FastJobs.Dashboard.Models;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
[CollectionDefinition("FastJobServerTests")]
public class ServerCollectionDefinition { }

[Collection("FastJobServerTests")]
public class Test : IClassFixture<ExpiryStateTestFixture>
{
    private readonly ExpiryStateTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IJobRepository _repo;

    public Test(ExpiryStateTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _repo = fixture.Host.Services.GetService<IJobRepository>();
        _output = output;
    }

    private static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (await condition())
                return;

            await Task.Delay(100); // poll interval
        }
    }

    [Fact]
    public async Task Job_should_transition_to_expired_state_after_expiring()
    {
        // Arrange
        var since = DateTime.UtcNow;

        // Act
        await FastJobServer.AddRecurringJob<BasicJob>()
        .WithInterval(TimeSpan.FromMinutes(1), DateTime.Now + TimeSpan.FromSeconds(1))
        .SetExpiresAt(DateTime.Now + TimeSpan.FromMinutes(1))
        .Start();


        await WaitUntilAsync(
            condition:async () => (await _repo.GetAllAsync()).Where(x => x.StateName == QueueStateTypes.Expired).Count() == 1,
            timeout: TimeSpan.FromSeconds(130));

        var allEntries = (await _repo.GetAllAsync()).Where(x => x.StateName == QueueStateTypes.Expired);


        // Assert
        var single = allEntries.SingleOrDefault();
        Assert.NotNull(single);
        Assert.Equal(1, allEntries.Count());
    }
}