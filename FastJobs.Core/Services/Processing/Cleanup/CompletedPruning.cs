using FastJobs.SqlServer;

namespace FastJobs;
public class CompletedJobsPruningStrategy : CleanupStrategy
{
    private readonly IJobRepository _JobRepo;

    public CompletedJobsPruningStrategy(IJobRepository jobRepository)
    {
        _JobRepo = jobRepository;
    }

    public  void  Clean()
    {   
        
    } 
}