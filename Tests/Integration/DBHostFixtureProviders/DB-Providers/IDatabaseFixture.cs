using MySqlConnector;
using Testcontainers.MariaDb;

namespace HostFixtureProviders;

public interface IDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get;  }
}