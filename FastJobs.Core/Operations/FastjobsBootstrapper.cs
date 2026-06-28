using System.Data;
using FastJobs.Persistence;

namespace FastJobs;

public class FastJobsBootstrapper
{
 private readonly IEnumerable<ISchemaInitializer> _initializers;

    public FastJobsBootstrapper(IEnumerable<ISchemaInitializer> initializers)
        => _initializers = initializers;

    public async Task InitializeAsync(IDbConnection connection)
    {
        var ordered = _initializers.OrderBy(i => i.Order);

        foreach (var initializer in ordered)
            await initializer.EnsureCreatedAsync(connection);
    }
}