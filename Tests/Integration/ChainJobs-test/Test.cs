using System.Diagnostics;
using FastJobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

[CollectionDefinition("FastJobServerTests")]
public class ServerCollectionDefinition { }


[Collection("FastJobServerTests")]
public class JobChainTest : IClassFixture<JobChainTestFixture>
{
    private readonly JobChainTestFixture _fixture;
    private readonly IChainExecutionRecorder _recorder;
    private readonly ITestOutputHelper _output;

    public JobChainTest(JobChainTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _recorder = fixture.Host.Services.GetRequiredService<IChainExecutionRecorder>();
    }

    [Fact]
    public async Task Chain_ExecutesStepsSequentiallyInDeclaredOrder()
    {
        // Given
        await FastJobServer.CreateChain()
            .AddStep<ChainStepAJob>()
            .ThenRun<ChainStepBJob>()
            .ThenRun<ChainStepCJob>()
            .ThenRun<ChainStepDJob>()
            .EnqueueAsync();

        // When
        var expectedOrder = new[] { "A", "B", "C", "D" };
        var pollInterval = TimeSpan.FromMilliseconds(300);
        var maxWait = TimeSpan.FromSeconds(15); // 4 steps * 200ms work + dispatch overhead, generously padded
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < maxWait && _recorder.GetEntries().Count < expectedOrder.Length)
        {
            await Task.Delay(pollInterval);
        }

        var entries = _recorder.GetEntries().OrderBy(e => e.Start).ToList();
        foreach (var e in entries)
            _output.WriteLine($"{e.Step}: {e.Start:O} -> {e.End:O}");

        // Then
        Assert.Equal(expectedOrder.Length, entries.Count);
        Assert.Equal(expectedOrder, entries.Select(e => e.Step));

        for (int i = 1; i < entries.Count; i++)
        {
            Assert.True(entries[i].Start >= entries[i - 1].End,
                $"Expected {entries[i].Step} to start after {entries[i - 1].Step} completed, " +
                $"but it started at {entries[i].Start:O} while {entries[i - 1].Step} ended at {entries[i - 1].End:O}");
        }
    }
}