using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Fastjobs.Dashboard.Pages; //small j in fastjobs seems to be preffered by razor So Well have both namespaces for now till its unified into FastJobs

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
            if (context.Request.Path.StartsWithSegments(path, out var remaining))
            {
                //Rewrite to Internal Path Regardlesss of Setpath
                context.Request.Path = InternalPath + remaining;
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