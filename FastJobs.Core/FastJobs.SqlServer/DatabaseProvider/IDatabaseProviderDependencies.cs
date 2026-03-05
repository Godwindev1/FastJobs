

using Microsoft.Extensions.DependencyInjection;

namespace FastJobs;

public interface IDatabaseProviderDependencies
{
    public void RegisterDependencies(IServiceCollection services);
    public void SetupDatabase(IServiceCollection Services, string ConnectionString);
}