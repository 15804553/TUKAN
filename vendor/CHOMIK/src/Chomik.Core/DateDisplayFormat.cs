namespace Chomik.Core;

public static class DateDisplayFormat
{
    public const string Pattern = "dd.MM.yyyy";

    public static string Format(DateTime value) => value.ToString(Pattern);

    public static string Format(DateTime? value) => value.HasValue ? Format(value.Value) : string.Empty;
}
