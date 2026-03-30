
public class FastJobsOptions
{
    public string ConnectionString {get; set; }
    public int WorkerCount {get; set; } = 1;
    public TimeSpan DefaultJobExpiration {get; set; } = TimeSpan.FromHours(24);

}