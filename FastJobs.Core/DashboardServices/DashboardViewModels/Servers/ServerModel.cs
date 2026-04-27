namespace FastJobs.Dashboard.Models;

public sealed class ServerModel
{
    public string ServerId                      { get; init; } = string.Empty;
    public string ServerName                    { get; init; } = string.Empty;
    public int TotalWorkerCount                 { get; init; }
    public int ActiveWorkerCount                { get; init; }
    public int SleepingWorkerCount              { get; init; }
    public DateTime StartedAt                   { get; init; }
    public DateTime LastHeartbeatAt             { get; init; }
    public TimeSpan Uptime                      => DateTime.UtcNow - StartedAt;
    public bool IsAlive                         { get; init; }
    public IReadOnlyList<string> Queues         { get; init; } = Array.Empty<string>();
}
