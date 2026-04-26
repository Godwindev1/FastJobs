using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

public static class FastjobsDashboardExtensions
{
    public static IServiceCollection AddFastjobsDashboard(this IServiceCollection services)
    {
        // Register any required services for the dashboard logic here
        return services;
    }

    public static void MapFastjobsDashboard(this IEndpointRouteBuilder endpoints, string path = "/fastjobs")
    {
        // 1. Map the Blazor Hub
        endpoints.MapBlazorHub();

        // 2. Map the fallback page using the endpoints builder directly
        // This uses the extension method that expects IEndpointRouteBuilder
        endpoints.MapFallbackToPage($"{path}/{{*all}}", "/_Host");
    }
}