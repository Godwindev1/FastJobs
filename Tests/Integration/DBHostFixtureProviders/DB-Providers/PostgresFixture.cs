using Testcontainers.PostgreSql;

namespace HostFixtureProviders;

public class PostgresFixture : IDatabaseFixture
{
    private PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("Fastjobs_db")
        .WithUsername("postgres")
        .WithPassword("secure_password_123")
        .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
