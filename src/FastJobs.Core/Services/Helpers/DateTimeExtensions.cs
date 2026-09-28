namespace FastJobs;

public static class DateTimeExtensions
{
    public static DateTimeOffset ToUtcOffsetStrict(this DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => new DateTimeOffset(dt),
        DateTimeKind.Local => new DateTimeOffset(dt).ToUniversalTime(),
        _ => throw new ArgumentException("DateTime.Kind must not be Unspecified.", nameof(dt))
    };
}
