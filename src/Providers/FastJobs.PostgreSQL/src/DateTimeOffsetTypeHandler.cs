using System.Data;
using Dapper;

namespace FastJobs.Persistence;

/// <summary>
/// Npgsql only accepts DateTimeOffset values with a zero offset for timestamptz and hands back
/// UTC DateTime values on read. This normalises both directions so the models keep DateTimeOffset.
/// </summary>
internal sealed class PostgresDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.Value = value.ToUniversalTime();
    }

    public override DateTimeOffset Parse(object value)
    {
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime(), TimeSpan.Zero),
            _ => throw new DataException($"Cannot convert {value.GetType()} to DateTimeOffset")
        };
    }
}
