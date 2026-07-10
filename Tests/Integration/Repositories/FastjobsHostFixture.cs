using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.Hosting;
using DotNetEnv;

public class FastJobsHostFixture : IAsyncLifetime
{
    public IHost Host { get; private set; } = default!;
    public IJobRepository Repository { get; private set; } = default!;

    private readonly MsSqlFixture _msSqlFixture = new();
    private readonly MariaDBFixture _mariaDbFixture = new();

    //string connectionstring = "Server=(localdb)\\FastjobsDBServer;Database=FastJobs;Trusted_Connection=True;MultipleActiveResultSets=true";

    public async Task InitializeAsync()
    {
        Env.Load();
        Console.WriteLine($"DOCKER_HOST resolved to: {Environment.GetEnvironmentVariable("DOCKER_HOST")}");

        //await _msSqlFixture.InitializeAsync();
        await _mariaDbFixture.InitializeAsync();

        //string connectionstring = _msSqlFixture.ConnectionString;
        string connectionstring = _mariaDbFixture.connectionString;

        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        
        //builder.Services.AddFastJobs(o => o.WorkerCount = 1, new FastJobMSSQLDependencies(x =>{  x.ConnectionString = connectionstring; x.SchemaName = "FastjobsDB"; } ));
        builder.Services.AddFastJobs(o => o.WorkerCount = 1, new FastJobMysqlDependencies(x =>{  x.ConnectionString = connectionstring; x.SchemaName = "FastjobsDB"; } ));
        
        Host = builder.Build();
        await Host.StartAsync();

        Host.Services.UseFastJobs();
    }

    public async Task DisposeAsync() => await Host.StopAsync();
}


[CollectionDefinition("FastjobsCollection")]
public class FastjobsSetupCollection : ICollectionFixture<FastJobsHostFixture>
{
    // Intentionally empty. Just glues fixture + collection name together.
}
