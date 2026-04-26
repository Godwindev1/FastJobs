namespace FastJobs.Dashboard.Models.Servers;

public sealed class ServerModel
{
//woker ID in list (e.g. "Default:1")
    public string ServerId                      { get; init; } = string.Empty;
    //Processing Server name (e.g. "Default")
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
