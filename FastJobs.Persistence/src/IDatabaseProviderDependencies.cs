

using Microsoft.Extensions.DependencyInjection;

namespace FastJobs.Persistence;

public interface IDatabaseProviderDependencies
{
    void RegisterDependencies(IServiceCollection services);
    void SetupDatabase(); // Fastjobs.core will call this method during startup to ensure the database is ready before processing jobs
}