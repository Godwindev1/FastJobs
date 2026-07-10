using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.Hosting;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;


public abstract class FastJobsHostFixtureBase : IAsyncLifetime
{
    public IHost Host { get; private set; } = default!;
    protected IDatabaseFixture DbFixture { get; }

    protected FastJobsHostFixtureBase(IDatabaseFixture dbFixture)
    {
        DbFixture = dbFixture;
    }

    protected abstract void ConfigureFastJobs(IServiceCollection services, string connectionString);

    public async Task InitializeAsync()
    {
        Env.Load();
        Console.WriteLine($"DOCKER_HOST resolved to: {Environment.GetEnvironmentVariable("DOCKER_HOST")}");
        
        await DbFixture.InitializeAsync();

        var builder =  Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();;
        ConfigureFastJobs(builder.Services, DbFixture.ConnectionString);

        Host = builder.Build();
        await Host.StartAsync();
        Host.Services.UseFastJobs();
    }

    public async Task DisposeAsync()
    {
        await Host.StopAsync();
        await DbFixture.DisposeAsync();
    }
}

