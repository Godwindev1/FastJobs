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

    public static IEndpointConventionBuilder MapFastjobsDashboard(this IEndpointRouteBuilder endpoints, string path = "/fastjobs")
    {
        // This maps the dashboard to a specific route
        return endpoints.MapBlazorHub().MapFallbackToPage($"{path}/{{*all}}", "/_Host");
    }
}