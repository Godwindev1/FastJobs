using MySqlConnector;
using Testcontainers.MariaDb;

public class MariaDBFixture : IAsyncLifetime
{
    private  MariaDbContainer _mariaDbContainer;

    public string connectionString => _mariaDbContainer.GetConnectionString();
    // Automatically called before any test runs
    public async Task InitializeAsync()
    {
        _mariaDbContainer =  new MariaDbBuilder()
        .WithImage("mariadb:10.11")
        .WithDatabase("Fastjobs_db")
        .WithUsername("root")
        .WithPassword("secure_password_123")
        .Build();

        await _mariaDbContainer.StartAsync();
    }

    [Fact]
    public async Task Can_Connect_And_Query_MariaDb_Container()
    {
        // 1. Get the dynamic connection string allocated by Testcontainers
        string connectionString = _mariaDbContainer.GetConnectionString();

        // 2. Open a standard connection using MySqlConnector
        using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();

        // 3. Execute a test query
        using var command = new MySqlCommand("SELECT VERSION();", connection);
        var version = await command.ExecuteScalarAsync();

        // 4. Assert the database responded correctly
        Assert.NotNull(version);
        Assert.StartsWith("10.11", version.ToString());
    }

    // Automatically called after all tests finish
    public async Task DisposeAsync()
    {
        await _mariaDbContainer.StopAsync();
    }
}