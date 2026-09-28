using System.Data;
using Dapper;

namespace FastJobs.Persistence;

/// <summary>
/// For all Databases ,
/// binding DateTimeOffset parameters directly through Dapper. This handler stores/reads DateTimeOffset
/// values as UTC DATETIME2(6), keeping the storage column type unchanged while the app-level type is DateTimeOffset.
/// MSSQL also follows this even though it has DATETIMEOFFSET
/// </summary>
internal sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.DbType = DbType.DateTime2;
        parameter.Value = value.UtcDateTime;
    }

    public override DateTimeOffset Parse(object value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc));
    }
}

internal sealed class NullableDateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset?>
{
    public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
    {
        parameter.DbType = DbType.DateTime2;
        parameter.Value = value.HasValue ? (object)value.Value.UtcDateTime : DBNull.Value;
    }

    public override DateTimeOffset? Parse(object value)
    {
        if (value is null || value is DBNull)
            return null;

        return new DateTimeOffset(DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc));
    }
}
