using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services.Urlop;

namespace BOBER.Services.Tests.Urlop;

public sealed class UrlopPlanValidatorTests
{
    private readonly UrlopPlanValidator _validator = new();
    private const int DefaultMaxNaSluzbie = UrlopPlanInstructions.DefaultMaxUrlopowNaSluzbie;

    [Fact]
    public void Validate_ExceedsWypoczynkowyLimit_ReturnsR3()
    {
        var wpisy = Enumerable.Range(1, 21)
            .Select(d => new UrlopPlanWpis
            {
                FunkcjonariuszId = 1,
                Rok = 2026,
                Miesiac = 1,
                Dzien = d,
                TypUrlopu = UrlopTypy.Wypoczynkowy
            })
            .ToList();

        var issues = _validator.Validate(1, 2026, wpisy,
            new Dictionary<int, string> { [1] = "Test Test" },
            (_, _) => false,
            DefaultMaxNaSluzbie);

        Assert.Contains(issues, i => i.RuleId == "R3");
    }

    [Fact]
    public void Validate_ExceedsRodzicielskiLimit_ReturnsR9()
    {
        var wpisy = Enumerable.Range(1, 64)
            .Select(d =>
            {
                var date = new DateOnly(2026, 1, 1).AddDays(d - 1);
                return new UrlopPlanWpis
                {
                    FunkcjonariuszId = 1,
                    Rok = date.Year,
                    Miesiac = date.Month,
                    Dzien = date.Day,
                    TypUrlopu = UrlopTypy.Rodzicielski
                };
            })
            .ToList();

        var issues = _validator.Validate(1, 2026, wpisy,
            new Dictionary<int, string> { [1] = "Test Test" },
            (_, _) => false,
            DefaultMaxNaSluzbie);

        Assert.Contains(issues, i => i.RuleId == "R9" && i.Message.Contains("63"));
    }

    [Fact]
    public void Validate_TooManyRodzicielskiParts_ReturnsR9()
    {
        // 6 osobnych części po 3 dni (R1), łącznie 18 ≤ 63
        var wpisy = new List<UrlopPlanWpis>();
        for (var part = 0; part < 6; part++)
        {
            var start = new DateOnly(2026, 1, 1).AddDays(part * 10);
            for (var i = 0; i < 3; i++)
            {
                var date = start.AddDays(i);
                wpisy.Add(new UrlopPlanWpis
                {
                    FunkcjonariuszId = 1,
                    Rok = date.Year,
                    Miesiac = date.Month,
                    Dzien = date.Day,
                    TypUrlopu = UrlopTypy.Rodzicielski
                });
            }
        }

        var issues = _validator.Validate(1, 2026, wpisy,
            new Dictionary<int, string> { [1] = "Test Test" },
            (_, _) => false,
            DefaultMaxNaSluzbie);

        Assert.Contains(issues, i => i.RuleId == "R9" && i.Message.Contains("5 części"));
    }

    [Fact]
    public void Validate_BlockedChristmasDay_ReturnsR8()
    {
        var wpisy = new List<UrlopPlanWpis>
        {
            new()
            {
                FunkcjonariuszId = 1,
                Rok = 2026,
                Miesiac = 12,
                Dzien = 25,
                TypUrlopu = UrlopTypy.Wypoczynkowy
            }
        };

        var issues = _validator.Validate(1, 2026, wpisy,
            new Dictionary<int, string> { [1] = "Test Test" },
            (_, _) => false,
            DefaultMaxNaSluzbie);

        Assert.Contains(issues, i => i.RuleId == "R8");
    }

    [Fact]
    public void Validate_TooManyOnWorkDay_ReturnsR7()
    {
        var workDay = new DateOnly(2026, 3, 10);
        var wpisy = Enumerable.Range(1, 6)
            .Select(id => new UrlopPlanWpis
            {
                FunkcjonariuszId = id,
                Rok = workDay.Year,
                Miesiac = workDay.Month,
                Dzien = workDay.Day,
                TypUrlopu = UrlopTypy.Wypoczynkowy
            })
            .ToList();

        var issues = _validator.Validate(1, 2026, wpisy,
            wpisy.ToDictionary(w => w.FunkcjonariuszId, w => $"Osoba {w.FunkcjonariuszId}"),
            (_, date) => date == workDay,
            DefaultMaxNaSluzbie);

        Assert.Contains(issues, i => i.RuleId == "R7");
    }

    [Fact]
    public void Validate_ValidBlockEndingDayBeforeService_NoR2EndError()
    {
        var workDays = new HashSet<DateOnly>
        {
            new(2026, 7, 1),
            new(2026, 7, 16)
        };

        var wpisy = Enumerable.Range(1, 15)
            .Select(d => new UrlopPlanWpis
            {
                FunkcjonariuszId = 1,
                Rok = 2026,
                Miesiac = 7,
                Dzien = d,
                TypUrlopu = UrlopTypy.Wypoczynkowy
            })
            .ToList();

        var issues = _validator.Validate(1, 2026, wpisy,
            new Dictionary<int, string> { [1] = "Wcisło Antoni" },
            (_, date) => workDays.Contains(date),
            DefaultMaxNaSluzbie);

        Assert.DoesNotContain(issues, i =>
            i.RuleId == "R2" && i.Message.Contains("kończyć się", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_BlockEndingOnWorkDay_ReturnsR2EndError()
    {
        var workDays = new HashSet<DateOnly>
        {
            new(2026, 7, 1),
            new(2026, 7, 16)
        };

        var wpisy = Enumerable.Range(1, 16)
            .Select(d => new UrlopPlanWpis
            {
                FunkcjonariuszId = 1,
                Rok = 2026,
                Miesiac = 7,
                Dzien = d,
                TypUrlopu = UrlopTypy.Wypoczynkowy
            })
            .ToList();

        var issues = _validator.Validate(1, 2026, wpisy,
            new Dictionary<int, string> { [1] = "Wcisło Antoni" },
            (_, date) => workDays.Contains(date),
            DefaultMaxNaSluzbie);

        Assert.Contains(issues, i =>
            i.RuleId == "R2" && i.Message.Contains("kończyć się", StringComparison.Ordinal));
    }
}
