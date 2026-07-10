using System.Data.Common;

namespace BOBER.Data;

internal static class OleDbReaderExtensions
{
    public static int GetFieldInt32(this DbDataReader reader, int ordinal) =>
        Convert.ToInt32(reader.GetValue(ordinal));

    public static int? GetNullableFieldInt32(this DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));

    public static bool GetFieldBoolean(this DbDataReader reader, int ordinal) =>
        Convert.ToBoolean(reader.GetValue(ordinal));

    public static DateTime? GetNullableFieldDateTime(this DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));
}
