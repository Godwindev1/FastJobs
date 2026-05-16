namespace FastJobs;

public interface IAfterAction : IBackGroundJob
{
    new Task ExecuteAsync( CancellationToken token);
}