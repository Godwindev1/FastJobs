using FastJobs.SqlServer;

namespace FastJobs;

public class AfterActionBuilder
{
    private string _typeName = string.Empty;
    private int _maxRetries = 3;

    public AfterActionBuilder WithType<T>() where T : class, IAfterAction
    {
        _typeName = typeof(T).AssemblyQualifiedName!;
        return this;
    }

    public AfterActionBuilder WithMaxRetries(int maxRetries)
    {
        _maxRetries = maxRetries;
        return this;
    }

    internal AfterActionModel Build(long jobId, long chainNo, long lastActionId = 0)
    {
        return new AfterActionModel
        {
            TypeName     = _typeName,
            Retries      = 0,
            MaxRetries   = _maxRetries,
            JobId        = jobId,
            ChainNo      = chainNo,
            LastActionID = lastActionId,
            NextActionID = 0
        };
    }
}