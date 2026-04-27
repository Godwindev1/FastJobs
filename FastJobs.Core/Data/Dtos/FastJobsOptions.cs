
public class FastJobsOptions
{
    public int WorkerCount {get; set; } = 1;
    public TimeSpan DefaultJobExpiration {get; set; } = TimeSpan.FromHours(24);

    public TimeSpan IdleWaitPeriod {get; set; }  = TimeSpan.FromSeconds(30);
    public TimeSpan  MaxSleep  {get; set; }  = TimeSpan.FromMinutes(5);

    public int DefaultMaxRetries {get; set; } = 3;
    public int DefaultWOrkerHeartbeatIntervalSeconds {get; set; } = 30;
}