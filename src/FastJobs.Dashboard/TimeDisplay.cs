using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Components;

namespace FastJobs.Dashboard;

public static class TimeDisplay
{
    public static MarkupString Utc(DateTimeOffset value, string pattern)
    {
        var label = WebUtility.HtmlEncode(value.ToString(pattern, CultureInfo.InvariantCulture) + " UTC");
        var iso = value.ToString("o", CultureInfo.InvariantCulture);
        return new MarkupString($"<span data-utc=\"{iso}\" data-pattern=\"{pattern}\" data-utc-label=\"{label}\">{label}</span>");
    }

    public static MarkupString Utc(DateTimeOffset? value, string pattern, string fallback = "—") =>
        value.HasValue ? Utc(value.Value, pattern) : new MarkupString(WebUtility.HtmlEncode(fallback));
}
