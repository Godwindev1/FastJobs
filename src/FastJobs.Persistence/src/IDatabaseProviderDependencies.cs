

using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs.Persistence;

public interface IDatabaseProviderDependencies
{
    void RegisterMandatoryDependencies()
    {
        SqlMapper.AddTypeHandler(new DateTimeOffsetTypeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeOffsetTypeHandler());
    }

    void RegisterDependencies(IServiceCollection services);
    void SetupDatabase(); // Fastjobs.core will call this method during startup to ensure the database is ready before processing jobs
}