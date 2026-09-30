using FastJobs;
using FastJobs.Persistence;
using Xunit.Abstractions;

[CollectionDefinition("FastJobServerTests")]
public class ServerCollectionDefinition { }

[Collection("FastJobServerTests")]
public class RecurringAfterActionModeTest : IClassFixture<RecurringAfterActionModeTestFixture>
{
    private readonly ITestOutputHelper _output;

    public RecurringAfterActionModeTest(RecurringAfterActionModeTestFixture fixture, ITestOutputHelper output)
    {
        _output = output;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (condition())
                return;

            await Task.Delay(200);
        }
    }

    [Fact]
    public async Task RunPerInstance_Fires_AfterAction_On_Every_Successful_Instance()
    {
        // No expiry — the job keeps recurring, so a correct RunPerInstance implementation
        // must fire the after-action on each completed instance, not just once.
        await FastJobServer.AddRecurringJob<RunPerInstanceModeTestJob>()
            .WithInterval(TimeSpan.FromSeconds(3), DateTime.UtcNow.AddSeconds(1))
            .SetAfterActionExecutionMode(AfterActionExecutionMode.RunPerInstance)
            .AddAfterAction(x => x.WithType<PerInstanceTrackingAfterAction>())
            .Start();

        await WaitUntilAsync(() => PerInstanceAfterActionTracker.RunCount >= 2, TimeSpan.FromSeconds(60));

        _output.WriteLine($"PerInstance after-action run count: {PerInstanceAfterActionTracker.RunCount}");

        Assert.True(PerInstanceAfterActionTracker.RunCount >= 2,
            $"Expected the after-action to fire on at least 2 instances, but it ran {PerInstanceAfterActionTracker.RunCount} time(s).");
    }

    [Fact]
    public async Task RunAfterFinalCompletion_Fires_AfterAction_Exactly_Once_On_The_Final_Instance()
    {
        // WithInterval schedules the first occurrence one interval after the supplied start time.
        // This puts the first run before expiry and its next occurrence after expiry.
        await FastJobServer.AddRecurringJob<RunAfterFinalCompletionModeTestJob>()
            .WithInterval(TimeSpan.FromSeconds(5), DateTime.UtcNow.AddSeconds(1))
            .SetExpiresAt(DateTime.UtcNow.AddSeconds(10))
            .SetAfterActionExecutionMode(AfterActionExecutionMode.RunAfterFinalCompletion)
            .AddAfterAction(x => x.WithType<FinalCompletionTrackingAfterAction>())
            .Start();

        await WaitUntilAsync(() => FinalCompletionAfterActionTracker.RunCount >= 1, TimeSpan.FromSeconds(60));

        // Give any incorrect extra firings a window to show up before asserting exactness.
        await Task.Delay(TimeSpan.FromSeconds(5));

        _output.WriteLine($"FinalCompletion after-action run count: {FinalCompletionAfterActionTracker.RunCount}");

        Assert.Equal(1, FinalCompletionAfterActionTracker.RunCount);
    }
}
