using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Components;
using FastJobs.Dashboard.Models;

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

public static class DisplayFormat
{
    /// <summary>Short readable job name from a possibly assembly-qualified type name.</summary>
    public static string JobName(string? name, string? methodName = null)
    {
        var shortName = ShortType(name);
        if (string.IsNullOrEmpty(shortName) || shortName == ".")
            shortName = ShortType(methodName);
        return string.IsNullOrEmpty(shortName) ? "—" : shortName;
    }

    /// <summary>Strips assembly info ("Version=…, Culture=…") and namespaces.</summary>
    public static string ShortType(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        var trimmed = name.Trim();
        var comma = trimmed.IndexOf(',');
        if (comma >= 0) trimmed = trimmed[..comma];
        var dot = trimmed.LastIndexOf('.');
        // keep "Type.Method" when the input is already that shape
        if (dot > 0)
        {
            var prev = trimmed.LastIndexOf('.', dot - 1);
            trimmed = trimmed[(prev + 1)..];
        }
        return trimmed.Trim('.');
    }

    public static string Duration(TimeSpan? duration)
    {
        if (!duration.HasValue) return "—";
        var d = duration.Value;
        if (d.TotalSeconds < 1) return $"{d.TotalMilliseconds:F0} ms";
        if (d.TotalMinutes < 1) return $"{d.TotalSeconds:F1} s";
        return $"{(int)d.TotalMinutes}m {d.Seconds}s";
    }

    public static string WorkerName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "—";
        var dash = name.IndexOf('-');
        if (dash < 0) return name;
        var suffix = name[(dash + 1)..];
        return suffix.Length > 8 ? $"{name[..(dash + 1)]}{suffix[..6]}" : name;
    }

    public static string ShortId(string? id) => string.IsNullOrEmpty(id) ? "—" : id.Length > 8 ? id[..8] : id;

    public static string Interval(TimeSpan? t)
    {
        if (!t.HasValue) return "—";
        var v = t.Value;
        if (v.TotalDays >= 1 && v.Hours == 0 && v.Minutes == 0) return $"{(int)v.TotalDays}d";
        if (v.TotalHours >= 1) return $"{(int)v.TotalHours}h {v.Minutes}m".Replace(" 0m", "");
        if (v.TotalMinutes >= 1) return $"{(int)v.TotalMinutes}m {v.Seconds}s".Replace(" 0s", "");
        return $"{(int)v.TotalSeconds}s";
    }

    public static string Count(int value) => value < 0 ? "0" : value.ToString("N0");

    public static string Percent(double rate, int total) => total <= 0 ? "—" : $"{rate:F0}%";
}

public static class StateDisplay
{
    public static string Label(JobState state) => state switch
    {
        JobState.Completed => "Succeeded",
        JobState.Deleted => "Cancelled",
        _ => state.ToString()
    };

    public static string Pill(JobState state) => state switch
    {
        JobState.Completed => "pill-green",
        JobState.Failed => "pill-red",
        JobState.Processing => "pill-blue",
        JobState.Scheduled => "pill-blue",
        JobState.Retrying => "pill-amber",
        JobState.Expired => "pill-amber",
        _ => "pill-gray"
    };
}
