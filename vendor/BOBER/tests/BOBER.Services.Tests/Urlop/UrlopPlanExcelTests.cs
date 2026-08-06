using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services.Urlop;

namespace BOBER.Services.Tests.Urlop;

public sealed class UrlopNameMatcherTests
{
    [Fact]
    public void Normalize_IgnoresWordOrder()
    {
        var a = UrlopNameMatcher.Normalize("Kowalski Jan");
        var b = UrlopNameMatcher.Normalize("Jan Kowalski");
        Assert.Equal(a, b);
    }

    [Fact]
    public void BuildLookup_MatchesExcelNameFormat()
    {
        var lookup = UrlopNameMatcher.BuildLookup([(1, "Jan", "Kowalski")]);
        Assert.True(UrlopNameMatcher.TryMatch("Kowalski Jan", lookup, out var id));
        Assert.Equal(1, id);
    }
}

public sealed class PolishHolidayCalendarTests
{
    [Fact]
    public void GetBlockedDates_IncludesChristmas()
    {
        var blocked = PolishHolidayCalendar.GetBlockedDates(2026);
        Assert.Contains(new DateOnly(2026, 12, 25), blocked);
        Assert.Contains(new DateOnly(2026, 12, 24), blocked);
    }

    [Fact]
    public void ComputeEasterSunday_2026_IsApril5()
    {
        Assert.Equal(new DateOnly(2026, 4, 5), PolishHolidayCalendar.ComputeEasterSunday(2026));
    }
}

public sealed class UrlopPlanExcelServiceTests
{
    [Fact]
    public void ExportImport_RoundTrip_PreservesVacationCodes()
    {
        var service = new UrlopPlanExcelService();
        var path = Path.Combine(Path.GetTempPath(), $"urlop_test_{Guid.NewGuid():N}.xlsx");
        var funkcjonariusze = new List<Funkcjonariusz>
        {
            new() { Id = 1, Imie = "Jan", Nazwisko = "Kowalski" }
        };
        var wpisy = new List<UrlopPlanWpis>
        {
            new() { FunkcjonariuszId = 1, Rok = 2026, Miesiac = 1, Dzien = 5, TypUrlopu = UrlopTypy.Wypoczynkowy },
            new() { FunkcjonariuszId = 1, Rok = 2026, Miesiac = 1, Dzien = 6, TypUrlopu = UrlopTypy.Dodatkowy },
            new() { FunkcjonariuszId = 1, Rok = 2026, Miesiac = 1, Dzien = 7, TypUrlopu = UrlopTypy.Rodzicielski }
        };

        try
        {
            service.Export(1, 2026, funkcjonariusze, wpisy, path);
            var imported = service.Import(path, 2026, funkcjonariusze);

            Assert.Equal(3, imported.Count);
            Assert.Contains(imported, w => w.Miesiac == 1 && w.Dzien == 5 && w.TypUrlopu == UrlopTypy.Wypoczynkowy);
            Assert.Contains(imported, w => w.Miesiac == 1 && w.Dzien == 6 && w.TypUrlopu == UrlopTypy.Dodatkowy);
            Assert.Contains(imported, w => w.Miesiac == 1 && w.Dzien == 7 && w.TypUrlopu == UrlopTypy.Rodzicielski);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
