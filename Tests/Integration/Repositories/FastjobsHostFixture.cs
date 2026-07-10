using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.Hosting;
using DotNetEnv;

public class FastJobsHostFixture : IAsyncLifetime
{
    public static class DbIdentifiers
    {
        public static string MSSQL = "MSSQL";
        public static string MariaDB = "MariaDB";
    }

    public IHost Host { get; private set; } = default!;
    public IJobRepository Repository { get; private set; } = default!;

    private readonly Dictionary<string, IDatabaseFixture> _DBFixtures = new(); 

    //string connectionstring = "Server=(localdb)\\FastjobsDBServer;Database=FastJobs;Trusted_Connection=True;MultipleActiveResultSets=true";

    public async Task InitializeAsync()
    {
        Env.Load();
        Console.WriteLine($"DOCKER_HOST resolved to: {Environment.GetEnvironmentVariable("DOCKER_HOST")}");

        _DBFixtures.Add(DbIdentifiers.MSSQL, new MsSqlFixture());
       // _DBFixtures.Add(DbIdentifiers.MariaDB, new MariaDBFixture());

        var Tasks = _DBFixtures.Select( async kvp => {
            var (providerName, fixture) = (kvp.Key, kvp.Value);
            await fixture.InitializeAsync();
            return (providerName, fixture);
        }) ;

        await Task.WhenAll(Tasks);

        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        
        builder.Services.AddFastJobs(o => o.WorkerCount = 1, new FastJobMSSQLDependencies(x =>{  x.ConnectionString = _DBFixtures[DbIdentifiers.MSSQL].ConnectionString; x.SchemaName = "FastjobsDB"; } ));
        //builder.Services.AddFastJobs(o => o.WorkerCount = 1, new FastJobMysqlDependencies(x =>{  x.ConnectionString = connectionstring; x.SchemaName = "FastjobsDB"; } ));
        
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
