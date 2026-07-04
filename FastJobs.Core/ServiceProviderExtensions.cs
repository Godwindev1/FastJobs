using FastJobs.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs
{
    public static class FastJobsConsoleExtensions
    {
    
        private static void InitializeFastJobsTables(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            
            var bootstrapper = scope.ServiceProvider.GetRequiredService<FastJobsDatabaseBootstrapper>();
            bootstrapper.InitializeAsync().GetAwaiter().GetResult();
        }

        private static void InitializeJobServerAbstraction(IServiceProvider provider)
        {
            FastJobServer.BuildInstance(provider.GetRequiredService<IServiceScopeFactory>());
        }

        public static void UseFastJobs(this IServiceProvider provider)
        {
            FastJobsConsoleExtensions.InitializeFastJobsTables(provider);
            FastJobsConsoleExtensions.InitializeJobServerAbstraction(provider);

            var ProcessingServer =  provider.GetRequiredService<ProcessingServer> ();
            ProcessingServer.StartProcessingJobs();
            ProcessingServer.StartScheduler();
        }
    }
}