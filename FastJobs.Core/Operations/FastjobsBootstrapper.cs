using System.Data;
using FastJobs.Persistence;

namespace FastJobs;

public class FastJobsDatabaseBootstrapper
{
 private readonly IEnumerable<ISchemaInitializer> _initializers;
 private readonly DbConnectionFactory _connectionFactory;
    public FastJobsDatabaseBootstrapper(IEnumerable<ISchemaInitializer> initializers, DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
          _initializers = initializers;
    }

    public async Task InitializeAsync()
    {
        IDbConnection connection = _connectionFactory.CreateConnection();
        var ordered = _initializers.OrderBy(i => i.Order);

        foreach (var initializer in ordered)
            await initializer.EnsureCreatedAsync(connection);
    }
}