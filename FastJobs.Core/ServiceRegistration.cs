using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace FastJobs;

using FastJobs.Dashboard;
using FastJobs.SqlServer;
using Microsoft.Extensions.Logging;

public static  class ServiceCollectionExtensions
{
    /// <summary>
    ///     Connection String Should Not Contain DB if it Doesnt already exist on the Server 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="ConnectionString"> Database Connection string </param>
    /// <param name="Database Provider"> Selected Or Only Installed Provider </param>
    /// <returns></returns>
    public static IServiceCollection AddFastJobs(
        this IServiceCollection services, Action<FastJobsOptions> optionsFactory, IDatabaseProviderDependencies databaseProvider)
    {
        RegisterApplicationShutdownToken(services);

        var _LoggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information).AddConsole());

        FastJobsOptions Options = new FastJobsOptions();
        optionsFactory.Invoke(Options); 
        services.AddSingleton(Options);

        var _Logger = _LoggerFactory.CreateLogger("Fastjobs.NET");

        _Logger.LogInformation("Starting Fastjobs.NET Core At {DateTime}", DateTime.UtcNow);
        //TODO: Use A options Or Descriptor For Parameters
        databaseProvider.SetupDatabase();
        databaseProvider.RegisterDependencies(services);

        services.AddScoped<QueueProcessor>();
        services.AddSingleton<ProcessingServer>();

        // Register expression-based job execution services
        services.AddScoped<IJobContext, JobContext>();
        services.AddScoped<ExpressionFireAndForgetJob>();
        services.AddScoped<IExpressionResolver, DefaultExpressionResolver>();

        //DASHBOARD SERVICES
        services.AddScoped<JobDetailService>();
        services.AddScoped<DashboardSummaryService>();
        services.AddScoped<RecurringJobService>();
        services.AddScoped<ScheduledJobService>();
        services.AddScoped<WorkerOverviewService>();

        

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

