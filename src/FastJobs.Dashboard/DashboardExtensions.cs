using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using FastJobs.Dashboard.Pages; 

namespace FastJobs.Dashboard;



public static class FastJobsDashboardExtensions
{
    
    internal const string InternalPath = "/FastJobs";

    public static IServiceCollection AddFastJobsDashboard(
        this IServiceCollection services)
    {
        services.AddRazorComponents()
                .AddInteractiveServerComponents();
        return services;
    }

    /// <summary>
    /// Middleware that rewrites the path before routing happens.
    /// </summary>
    public static IApplicationBuilder UseFastJobsDashboard(
        this IApplicationBuilder app,
        string path = "/FastJobs")
    {
        path = "/" + path.Trim('/');

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(path, out var matched, out var remaining))
            {
                // Mount the dashboard as a path base so the browser, router and circuit all agree on the URL.
                context.Request.PathBase = context.Request.PathBase.Add(matched);

                // Framework requests (_blazor, _framework, _content) keep their own paths; pages map to the internal routes.
                context.Request.Path = remaining.StartsWithSegments("/_blazor") ||
                                       remaining.StartsWithSegments("/_framework") ||
                                       remaining.StartsWithSegments("/_content")
                    ? remaining
                    : InternalPath + remaining;
            }
            await next();
        });

        return app;
    }

    // Endpoint registration  uses internal path
    public static IEndpointRouteBuilder MapFastJobsDashboard(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorComponents<DashboardRoot>()
                 .AddInteractiveServerRenderMode();
        return endpoints;
    }
}