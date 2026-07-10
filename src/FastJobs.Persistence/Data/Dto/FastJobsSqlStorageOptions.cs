namespace FastJobs.Persistence;
public class FastJobsSqlStorageOptions
{
    public string ConnectionString {get; set; }
    public string SchemaName { get; set; } = "FastJobs";
}