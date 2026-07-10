using System.Data;

namespace FastJobs.Persistence;

public interface ISchemaInitializer
{
    /// <summary>
    /// Lower values run first. Use to express dependency ordering
    /// (e.g. a table must exist before another references it via FK).
    /// </summary>
    /// NOTE If DB SCHEMA GRAPH SHOULD GET TOO COMPLEX TO BE CAPTURED BY A SINGLE ORDER metric. 
    /// Default TO Using Single Schema initializer Which Contains All the necessary SQL in the Right Order instead of (JobsInitializer, QueueInitializer )
    /// use DbInitializer() to bundle Everything
    public int Order { get; }

    /// <summary>
    /// Ensures all tables/indexes required by this provider package exist.
    /// Must be idempotent — safe to call on every startup.
    /// </summary>
    Task EnsureCreatedAsync(IDbConnection connection /*, CancellationToken ct = default */);
}