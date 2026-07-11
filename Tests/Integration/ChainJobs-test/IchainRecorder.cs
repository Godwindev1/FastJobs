using System.Collections.Concurrent;

public interface IChainExecutionRecorder
{
    void RecordStart(string step);
    void RecordEnd(string step);
    IReadOnlyList<(string Step, DateTime Start, DateTime End)> GetEntries();
}

public class ChainExecutionRecorder : IChainExecutionRecorder
{
    private readonly ConcurrentDictionary<string, (DateTime Start, DateTime End)> _entries = new();

    public void RecordStart(string step) =>
        _entries.AddOrUpdate(step, (DateTime.UtcNow, DateTime.MinValue), (_, existing) => (DateTime.UtcNow, existing.End));

    public void RecordEnd(string step) =>
        _entries.AddOrUpdate(step, (DateTime.MinValue, DateTime.UtcNow), (_, existing) => (existing.Start, DateTime.UtcNow));

    public IReadOnlyList<(string Step, DateTime Start, DateTime End)> GetEntries() =>
        _entries.Select(kv => (kv.Key, kv.Value.Start, kv.Value.End)).ToList();
}