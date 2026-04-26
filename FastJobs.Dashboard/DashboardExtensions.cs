using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Fastjobs.Dashboard.Pages;

public static class FastjobsDashboardExtensions
{
    internal const string InternalPath = "/fastjobs";

    public static IServiceCollection AddFastjobsDashboard(
        this IServiceCollection services)
    {
        services.AddRazorComponents()
                .AddInteractiveServerComponents();
        return services;
    }

    /// <summary>
    // Middleware To rewrites the path Before routing happens
    /// <summary>
    public static IApplicationBuilder UseFastjobsDashboard(
        this IApplicationBuilder app,
        string path = "/fastjobs")
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
    public static IEndpointRouteBuilder MapFastjobsDashboard(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorComponents<DashboardRoot>()
                 .AddInteractiveServerRenderMode();
        return endpoints;
    }
}