namespace BOBER.Services.Urlop;

/// <summary>Święta Wielkanocne i Bożego Narodzenia — reguła R8 planu urlopów.</summary>
public static class PolishHolidayCalendar
{
    public static IReadOnlySet<DateOnly> GetBlockedDates(int year)
    {
        var dates = new HashSet<DateOnly>();

        foreach (var day in GetEasterHolidayRange(year))
            dates.Add(day);

        dates.Add(new DateOnly(year, 12, 24));
        dates.Add(new DateOnly(year, 12, 25));
        dates.Add(new DateOnly(year, 12, 26));

        return dates;
    }

    public static bool IsBlocked(DateOnly date) => GetBlockedDates(date.Year).Contains(date);

    private static IEnumerable<DateOnly> GetEasterHolidayRange(int year)
    {
        var easter = ComputeEasterSunday(year);
        yield return easter.AddDays(-2);
        yield return easter.AddDays(-1);
        yield return easter;
        yield return easter.AddDays(1);
    }

    /// <summary>Algorytm Meeusa/Jonesa/Butchera — niedziela Wielkanocna.</summary>
    public static DateOnly ComputeEasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }
}
