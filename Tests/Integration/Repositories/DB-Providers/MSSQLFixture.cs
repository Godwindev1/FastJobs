using Testcontainers.MsSql;

public class MsSqlFixture : IAsyncLifetime
{
    private MsSqlContainer _container ;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("FastJobs-Integration-Test-Pa$$w0rd2026")
        .Build();

        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}