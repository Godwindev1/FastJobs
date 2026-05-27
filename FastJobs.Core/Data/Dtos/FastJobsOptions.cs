
public class FastJobsOptions
{
    public int WorkerCount {get; set; } = 1;
    public TimeSpan DefaultJobExpiration {get; set; } = TimeSpan.FromHours(24);

    public TimeSpan JobRetryDelayBase {get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan MaxJobRetryDelay {get; set; } = TimeSpan.FromSeconds(300);
    public TimeSpan Jitter {get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan IdleWaitPeriod {get; set; }  = TimeSpan.FromSeconds(30);
    public TimeSpan  MaxSleep  {get; set; }  = TimeSpan.FromMinutes(5);

    public int DefaultMaxRetries {get; set; } = 3;
    public int DefaultWOrkerHeartbeatIntervalSeconds {get; set; } = 30;
}