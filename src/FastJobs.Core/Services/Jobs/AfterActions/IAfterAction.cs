using FastJobs.Persistence;

namespace FastJobs;

public interface IAfterAction : IBackGroundJob
{
    new Task ExecuteAsync( CancellationToken token);
}

public interface IAfterActionContext 
{
    public AfterActionModel Model {get; set; } 
}

public class AfterActionContext : IAfterActionContext
{
    public AfterActionModel Model {get; set; } 

    public void SetAction(AfterActionModel afterAction) => Model = afterAction;
}