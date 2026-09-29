using FastJobs.Persistence;

namespace FastJobs;

public class AfterActionBuilder
{
    private string _typeName = string.Empty;

    public AfterActionBuilder WithType<T>() where T : class, IAfterAction
    {
        _typeName = typeof(T).AssemblyQualifiedName!;
        return this;
    }


    internal AfterActionModel Build(long jobId, long chainNo, long lastActionId = 0)
    {
        return new AfterActionModel
        {
            TypeName     = _typeName,
            JobId        = jobId,
            ChainNo      = chainNo,
            LastActionID = lastActionId,
            NextActionID = 0
        };
    }
}