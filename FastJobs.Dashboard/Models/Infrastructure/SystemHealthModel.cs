using FastJobs.Dashboard.Models.Servers;

namespace FastJobs.Dashboard.Models.Infrastructure;

public sealed class SystemHealthModel
{
    public DatabaseInfoModel Database           { get; init; } = new();

    public IReadOnlyList<QueueDepthModel> Queues { get; init; }
        = Array.Empty<QueueDepthModel>();

    public IReadOnlyList<ServerModel> Servers   { get; init; }
        = Array.Empty<ServerModel>();

    public string LibraryVersion                { get; init; } = string.Empty;
    public int ConfiguredWorkerCount            { get; init; }
    public int DefaultMaxRetries                 { get; init; }
    public TimeSpan HeartbeatInterval           { get; init; }
    public DateTime GeneratedAt                 { get; init; } = DateTime.UtcNow;
}
