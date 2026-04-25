using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace FastJobs;

using FastJobs.SqlServer;

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
        RegisterApplicationShutdownToken(services);

        FastJobsOptions Options = new FastJobsOptions();
        optionsFactory.Invoke(Options); 
        services.AddSingleton(Options);

        Console.WriteLine("Adding FastJobs");
        //TODO: Use A options Or Descriptor For Parameters
        databaseProvider.SetupDatabase();
        databaseProvider.RegisterDependencies(services);

        services.AddScoped<QueueProcessor>();
        services.AddSingleton<ProcessingServer>();

        // Register expression-based job execution services
        services.AddScoped<IJobContext, JobContext>();
        services.AddScoped<ExpressionFireAndForgetJob>();
        services.AddScoped<IExpressionResolver, DefaultExpressionResolver>();

        return services;
    }
 
    public static IServiceCollection AddJobService<TJob>(
        this IServiceCollection services)
        where TJob : class, IBackGroundJob
    {
        services.AddScoped<TJob>();
        return services;
    }

    private static void RegisterApplicationShutdownToken(IServiceCollection Services)
    {

        Services.AddSingleton(sp =>
        {
            var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
            var cts = new CancellationTokenSource();

            // Hook it to the app shutdown event
            lifetime.ApplicationStopping.Register(() => cts.Cancel());
            lifetime.ApplicationStopped.Register(() => cts.Dispose());
            
            return cts;
        });
        
    }

}

