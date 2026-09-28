using System.Data;
using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

public class FastJobsDatabaseBootstrapper
{
    private readonly IEnumerable<ISchemaInitializer> _initializers;
    private readonly IServiceScopeFactory _scopeFactory;

    public FastJobsDatabaseBootstrapper(IEnumerable<ISchemaInitializer> initializers, IServiceScopeFactory scopeFactory)
    {
        _initializers = initializers;
        _scopeFactory = scopeFactory;
    }

    public async Task InitializeAsync()
    {
        using var scope = new ScopeManager(_scopeFactory);
        IDbConnection connection = scope.Resolve<DbConnectionFactory>().CreateConnection();
        var ordered = _initializers.OrderBy(i => i.Order);

        foreach (var initializer in ordered)
            await initializer.EnsureCreatedAsync(connection);
    }
}