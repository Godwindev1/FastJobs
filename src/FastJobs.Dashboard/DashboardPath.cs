using Microsoft.AspNetCore.Components;

namespace FastJobs.Dashboard;

internal static class DashboardPath
{
    /// <summary>
    /// Returns the current page path relative to the dashboard root (e.g. "", "jobs").
    /// During prerender the request path still carries the internal "FastJobs" segment; in the
    /// interactive circuit it does not, so it is stripped when present.
    /// </summary>
    public static string Relative(NavigationManager navigation)
    {
        var relative = navigation.ToBaseRelativePath(navigation.Uri);
        var end = relative.IndexOfAny(new[] { '?', '#' });
        if (end >= 0) relative = relative[..end];
        relative = relative.Trim('/');

        var internalSegment = FastJobsDashboardExtensions.InternalPath.Trim('/');
        if (relative.Equals(internalSegment, StringComparison.OrdinalIgnoreCase)) return "";
        if (relative.StartsWith(internalSegment + "/", StringComparison.OrdinalIgnoreCase))
            return relative[(internalSegment.Length + 1)..];
        return relative;
    }
}
