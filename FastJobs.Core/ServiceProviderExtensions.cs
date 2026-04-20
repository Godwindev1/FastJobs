using System.Data;
using FastJobs.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs
{
    public static class FastJobsConsoleExtensions
    {
        private static void InitializeFastJobsTables(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();

            var connection  = factory.CreateConnection();

            JobTableInitializer
            .EnsureCreatedAsync(connection)
            .GetAwaiter()
            .GetResult();

             QueueTableInitializer
             .EnsureCreatedAsync(connection)
             .GetAwaiter()
             .GetResult();

             ScheduledJobTableInitializer
             .EnsureCreatedAsync(connection)
             .GetAwaiter()
             .GetResult();

             StateHistoryTableInitialization
             .EnsureCreatedAsync(connection)
             .GetAwaiter()
             .GetResult();

             RecurringJobTableInitializer
             .EnsureCreatedAsync(connection)
             .GetAwaiter()
             .GetResult();
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