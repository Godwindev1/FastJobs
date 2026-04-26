namespace FastJobs.Dashboard.Models.Infrastructure;

public sealed class DatabaseInfoModel
{
    public string Provider                      { get; init; } = string.Empty;
    public string MaskedConnectionString        { get; init; } = string.Empty;

    public int ActiveConnections                { get; init; }
    public int IdleConnections                  { get; init; }
    public int MaxPoolSize                      { get; init; }
    public double PoolUtilisationPct            =>
        MaxPoolSize == 0 ? 0 : Math.Round((double)ActiveConnections / MaxPoolSize * 100, 1);

    public bool IsHealthy                       { get; init; }
    public TimeSpan LastPingLatency             { get; init; }
    public DateTime LastCheckedAt               { get; init; }
    public string? ErrorMessage                 { get; init; }
}
