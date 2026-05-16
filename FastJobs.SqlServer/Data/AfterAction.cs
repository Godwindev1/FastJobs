namespace FastJobs.SqlServer;

public class AfterActionModel
{
    public long Id {get; set; }
    public string TypeName {get; set; }
    public int Retries {get; set; }
    public int MaxRetries {get; set; }
    public long JobId {get; set; }
    public long NextActionID {get; set;}
    public long LastActionID {get; set;}
    public long ChainNo {get; set; }
}