using FastJobs;
using FastJobs.Persistence;
using Microsoft.Extensions.Hosting;

public class FastJobsHostFixture : IAsyncLifetime
{
    public IHost Host { get; private set; } = default!;
    public IJobRepository Repository { get; private set; } = default!;

    string connectionstring = "Server=(localdb)\\FastjobsDBServer;Database=FastJobs;Trusted_Connection=True;MultipleActiveResultSets=true";

    public async Task InitializeAsync()
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Services.AddFastJobs(o => o.WorkerCount = 1, new FastJobMSSQLDependencies(x =>{  x.ConnectionString = connectionstring; x.SchemaName = "FastjobsDB"; } ));
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
