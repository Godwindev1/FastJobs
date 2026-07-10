using MySqlConnector;
using Testcontainers.MariaDb;

public interface IDatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get;  }
}