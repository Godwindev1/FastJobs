using Microsoft.Extensions.DependencyInjection;
namespace FastJobs;

public static  class ServiceCollectionExtensions
{
    /// <summary>
    ///     Connection String Should Not Contain DB if it Doesnt already exist on the Server 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="ConnectionString"> Database Connection string </param>
    /// <param name="Database Provider"> Selected Or Only Installed Provider </param>
    /// <returns></returns>
    public static IServiceCollection FastJobs(
        this IServiceCollection services, Action<FastJobsOptions> optionsFactory, IDatabaseProviderDependencies databaseProvider)
    {
        FastJobsOptions Options = new FastJobsOptions();
        optionsFactory.Invoke(Options); 
        services.AddSingleton(Options);

        Console.WriteLine("Adding FastJobs");
        //TODO: Use A options Or Descriptor For Parameters
        databaseProvider.SetupDatabase(services, Options.ConnectionString);
        databaseProvider.RegisterDependencies(services);

        services.AddTransient<JobProcessor>();

        return services;
    }
 

}

