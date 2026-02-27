using System.Data;
using Microsoft.Extensions.DependencyInjection;

namespace FastJobs
{
    public static class FastJobsConsoleExtensions
    {
        private static void InitializeFastJobsTables(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var connection = scope.ServiceProvider.GetRequiredService<IDbConnection>();
            JobTableInitializer
            .EnsureCreatedAsync(connection)
            .GetAwaiter()
            .GetResult();

             QueueTableInitializer
             .EnsureCreatedAsync(connection)
             .GetAwaiter()
             .GetResult();

             StateHistoryTableInitialization
             .EnsureCreatedAsync(connection)
             .GetAwaiter()
             .GetResult();
        }

        private static void InitializeJobServerAbstraction(IServiceProvider provider)
        {
            FastJobServer.BuildInstance(provider);
        }

        public static void UseFastJobs(this IServiceProvider provider)
        {
            FastJobsConsoleExtensions.InitializeFastJobsTables(provider);
            FastJobsConsoleExtensions.InitializeJobServerAbstraction(provider);
        }
    }
}