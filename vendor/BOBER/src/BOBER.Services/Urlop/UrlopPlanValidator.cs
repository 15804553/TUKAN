using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services.Grafik;

namespace BOBER.Services.Urlop;

public sealed class UrlopPlanValidator
{
    private const int MaxWypoczynkowy = UrlopPlanInstructions.LimitWypoczynkowy;
    private const int MaxDodatkowy = UrlopPlanInstructions.LimitDodatkowy;
    private const int MaxRodzicielski = UrlopPlanInstructions.LimitRodzicielski;
    private const int MinDodatkowyPart = UrlopPlanInstructions.MinCzescDodatkowy;
    private const int MaxCzesciDodatkowy = UrlopPlanInstructions.MaxCzesciDodatkowy;
    private const int MaxCzesciRodzicielski = UrlopPlanInstructions.MaxCzesciRodzicielski;
    private const int MaxWakacjeWypoczynkowy = UrlopPlanInstructions.LimitWakacjeWypoczynkowy;

    public IReadOnlyList<UrlopPlanValidationIssue> Validate(
        int zmianaId,
        int rok,
        IReadOnlyList<UrlopPlanWpis> wpisy,
        IReadOnlyDictionary<int, string> nazwiska,
        Func<int, DateOnly, bool> isWorkDay,
        int maxNaSluzbie)
    {
        var issues = new List<UrlopPlanValidationIssue>();
        var blocked = PolishHolidayCalendar.GetBlockedDates(rok);

        var byPerson = wpisy
            .GroupBy(w => w.FunkcjonariuszId)
            .ToDictionary(g => g.Key, g => g.OrderBy(w => w.Data).ToList());

        foreach (var (fid, personWpisy) in byPerson)
        {
            var name = nazwiska.TryGetValue(fid, out var n) ? n : $"ID {fid}";
            ValidatePersonBlocks(personWpisy, fid, name, rok, isWorkDay, issues);
            ValidateAnnualLimits(personWpisy, fid, name, issues);
            ValidateSummerLimit(personWpisy, fid, name, issues);
            ValidateAdditionalParts(personWpisy, fid, name, issues);
            ValidateRodzicielskiParts(personWpisy, fid, name, issues);
        }

        ValidateDailyShiftLimit(zmianaId, rok, wpisy, isWorkDay, maxNaSluzbie, issues);
        ValidateBlockedHolidays(wpisy, nazwiska, blocked, issues);

        return issues;
    }

    private static void ValidatePersonBlocks(
        IReadOnlyList<UrlopPlanWpis> wpisy,
        int fid,
        string name,
        int rok,
        Func<int, DateOnly, bool> isWorkDay,
        List<UrlopPlanValidationIssue> issues)
    {
        if (wpisy.Count == 0)
            return;

        var blocks = GetContiguousBlocks(wpisy);
        foreach (var block in blocks)
        {
            var start = block[0].Data;
            var end = block[^1].Data;

            if (!isWorkDay(0, start))
            {
                issues.Add(new UrlopPlanValidationIssue
                {
                    RuleId = "R2",
                    FunkcjonariuszId = fid,
                    Data = start,
                    Message = $"{name}: urlop powinien zaczynać się w dniu służby ({start:dd.MM.yyyy})."
                });
            }

            var dayAfter = end.AddDays(1);
            if (dayAfter.Year == rok && !isWorkDay(0, dayAfter))
            {
                issues.Add(new UrlopPlanValidationIssue
                {
                    RuleId = "R2",
                    FunkcjonariuszId = fid,
                    Data = end,
                    Message = $"{name}: urlop powinien kończyć się w dniu przed służbą ({end:dd.MM.yyyy})."
                });
            }

            if (block.Count % 3 != 0)
            {
                issues.Add(new UrlopPlanValidationIssue
                {
                    RuleId = "R1",
                    FunkcjonariuszId = fid,
                    Data = start,
                    Message = $"{name}: blok urlopu ({start:dd.MM}–{end:dd.MM}) nie mieści się w interwale 3-dniowym służb."
                });
            }
        }
    }

    private static void ValidateAnnualLimits(
        IReadOnlyList<UrlopPlanWpis> wpisy,
        int fid,
        string name,
        List<UrlopPlanValidationIssue> issues)
    {
        var wCount = wpisy.Count(w => w.TypUrlopu == UrlopTypy.Wypoczynkowy);
        var dCount = wpisy.Count(w => w.TypUrlopu == UrlopTypy.Dodatkowy);
        var rCount = wpisy.Count(w => w.TypUrlopu == UrlopTypy.Rodzicielski);

        if (wCount > MaxWypoczynkowy)
        {
            issues.Add(new UrlopPlanValidationIssue
            {
                RuleId = "R3",
                FunkcjonariuszId = fid,
                Message = $"{name}: przekroczono limit {MaxWypoczynkowy} dni wypoczynkowych (zaplanowano {wCount})."
            });
        }

        if (dCount > MaxDodatkowy)
        {
            issues.Add(new UrlopPlanValidationIssue
            {
                RuleId = "R4",
                FunkcjonariuszId = fid,
                Message = $"{name}: przekroczono limit {MaxDodatkowy} dni dodatkowych (zaplanowano {dCount})."
            });
        }

        if (rCount > MaxRodzicielski)
        {
            issues.Add(new UrlopPlanValidationIssue
            {
                RuleId = "R9",
                FunkcjonariuszId = fid,
                Message = $"{name}: przekroczono limit {MaxRodzicielski} dni urlopu rodzicielskiego (zaplanowano {rCount})."
            });
        }
    }

    private static void ValidateSummerLimit(
        IReadOnlyList<UrlopPlanWpis> wpisy,
        int fid,
        string name,
        List<UrlopPlanValidationIssue> issues)
    {
        var summer = wpisy.Count(w =>
            w.TypUrlopu == UrlopTypy.Wypoczynkowy
            && w.Miesiac is >= 6 and <= 9);

        if (summer > MaxWakacjeWypoczynkowy)
        {
            issues.Add(new UrlopPlanValidationIssue
            {
                RuleId = "R6",
                FunkcjonariuszId = fid,
                Message = $"{name}: w sezonie czerwiec–wrzesień zaplanowano {summer} dni wypoczynkowych (max {MaxWakacjeWypoczynkowy})."
            });
        }
    }

    private static void ValidateAdditionalParts(
        IReadOnlyList<UrlopPlanWpis> wpisy,
        int fid,
        string name,
        List<UrlopPlanValidationIssue> issues)
    {
        var dodatkowe = wpisy
            .Where(w => w.TypUrlopu == UrlopTypy.Dodatkowy)
            .OrderBy(w => w.Data)
            .ToList();

        if (dodatkowe.Count == 0)
            return;

        var parts = GetContiguousBlocks(dodatkowe);
        if (parts.Count > MaxCzesciDodatkowy)
        {
            issues.Add(new UrlopPlanValidationIssue
            {
                RuleId = "R4",
                FunkcjonariuszId = fid,
                Message = $"{name}: urlop dodatkowy można podzielić maksymalnie na {MaxCzesciDodatkowy} części (zaplanowano {parts.Count})."
            });
        }

        foreach (var part in parts.Where(p => p.Count < MinDodatkowyPart))
        {
            issues.Add(new UrlopPlanValidationIssue
            {
                RuleId = "R4",
                FunkcjonariuszId = fid,
                Data = part[0].Data,
                Message = $"{name}: część urlopu dodatkowego ({part.Count} dni) jest krótsza niż {MinDodatkowyPart} dni."
            });
        }
    }

    private static void ValidateRodzicielskiParts(
        IReadOnlyList<UrlopPlanWpis> wpisy,
        int fid,
        string name,
        List<UrlopPlanValidationIssue> issues)
    {
        var rodzicielskie = wpisy
            .Where(w => w.TypUrlopu == UrlopTypy.Rodzicielski)
            .OrderBy(w => w.Data)
            .ToList();

        if (rodzicielskie.Count == 0)
            return;

        var parts = GetContiguousBlocks(rodzicielskie);
        if (parts.Count > MaxCzesciRodzicielski)
        {
            issues.Add(new UrlopPlanValidationIssue
            {
                RuleId = "R9",
                FunkcjonariuszId = fid,
                Message = $"{name}: urlop rodzicielski można podzielić maksymalnie na {MaxCzesciRodzicielski} części (zaplanowano {parts.Count})."
            });
        }
    }

    private static void ValidateDailyShiftLimit(
        int zmianaId,
        int rok,
        IReadOnlyList<UrlopPlanWpis> wpisy,
        Func<int, DateOnly, bool> isWorkDay,
        int maxNaSluzbie,
        List<UrlopPlanValidationIssue> issues)
    {
        var byDate = wpisy.GroupBy(w => w.Data);
        foreach (var group in byDate)
        {
            if (!isWorkDay(zmianaId, group.Key))
                continue;

            var count = group.Select(w => w.FunkcjonariuszId).Distinct().Count();
            if (count > maxNaSluzbie)
            {
                issues.Add(new UrlopPlanValidationIssue
                {
                    RuleId = "R7",
                    Data = group.Key,
                    Message = $"W dniu służby {group.Key:dd.MM.yyyy} zaplanowano {count} osób na urlopie (max {maxNaSluzbie})."
                });
            }
        }
    }

    private static void ValidateBlockedHolidays(
        IReadOnlyList<UrlopPlanWpis> wpisy,
        IReadOnlyDictionary<int, string> nazwiska,
        IReadOnlySet<DateOnly> blocked,
        List<UrlopPlanValidationIssue> issues)
    {
        foreach (var wpis in wpisy.Where(w => blocked.Contains(w.Data)))
        {
            var name = nazwiska.TryGetValue(wpis.FunkcjonariuszId, out var n) ? n : $"ID {wpis.FunkcjonariuszId}";
            issues.Add(new UrlopPlanValidationIssue
            {
                RuleId = "R8",
                FunkcjonariuszId = wpis.FunkcjonariuszId,
                Data = wpis.Data,
                Message = $"{name}: nie planujemy urlopów w święta Wielkanocne i Bożego Narodzenia ({wpis.Data:dd.MM.yyyy})."
            });
        }
    }

    private static List<List<UrlopPlanWpis>> GetContiguousBlocks(IReadOnlyList<UrlopPlanWpis> wpisy)
    {
        var blocks = new List<List<UrlopPlanWpis>>();
        if (wpisy.Count == 0)
            return blocks;

        var current = new List<UrlopPlanWpis> { wpisy[0] };
        for (var i = 1; i < wpisy.Count; i++)
        {
            if (wpisy[i].Data == wpisy[i - 1].Data.AddDays(1))
            {
                current.Add(wpisy[i]);
            }
            else
            {
                blocks.Add(current);
                current = [wpisy[i]];
            }
        }

        blocks.Add(current);
        return blocks;
    }
}
