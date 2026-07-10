
namespace FastJobs;

public interface IBackGroundJob
{
    Task ExecuteAsync( CancellationToken token);
}

