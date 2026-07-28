using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using Tukan.App.Services;
using Tukan.App.ViewModels;
using BoberFunkcjonariusz = BOBER.Core.Models.Funkcjonariusz;

namespace Tukan.App.Controllers;

public sealed class DutyAssignmentsController
{
    private readonly TukanAppServices _services;
    private readonly int _shiftNumber;

    public DutyAssignmentsController(TukanAppServices services, int shiftNumber, string shiftName)
    {
        _services = services;
        _shiftNumber = shiftNumber;
        ShiftName = shiftName;
    }

    public string ShiftName { get; }

    public int CurrentYear { get; } = DateTime.Today.Year;

    public async Task<HashSet<int>> GetWorkDaysForMonthAsync(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var workDays = new HashSet<int>();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            if (await _services.Skrybek.Personnel.CzyDzienSluzbyAsync(_shiftNumber, date))
            {
                workDays.Add(day);
            }
        }

        return workDays;
    }

    public async Task<IReadOnlyList<DutyAssignmentsRowViewModel>> BuildRowsAsync(int year, int month)
    {
        // Ta sama kolejność co w grafiku służb (KolejnoscFunkcjonariuszy z BOBER).
        var personnel = await _services.Bober.Funkcjonariusze.GetByZmianaAsync(_shiftNumber);
        var rows = personnel
            .Select((person, index) => new DutyAssignmentsRowViewModel
            {
                Numer = index + 1,
                ImieNazwisko = person.PelneImieNazwisko
            })
            .ToList();

        var rowsById = personnel.Zip(rows).ToDictionary(pair => pair.First.Id, pair => pair.Second);
        var rowsByName = BuildNameLookup(personnel, rows);

        foreach (var order in await GetApprovedOrdersForMonthAsync(year, month))
        {
            foreach (var assignment in order.Sluzba)
            {
                var code = MapRoleCode(assignment.Stanowisko);
                if (string.IsNullOrEmpty(code))
                {
                    continue;
                }

                var row = FindRow(rowsById, rowsByName, assignment);
                row?.AddAssignment(order.Data.Day, code);
            }
        }

        return rows;
    }

    private async Task<IReadOnlyList<RozkazDzienny>> GetApprovedOrdersForMonthAsync(int year, int month)
    {
        var summaries = await _services.Skrybek.Rozkaz.GetByRokAsync(year);
        var matchingOrders = summaries
            .Where(order => order.Status == StatusRozkazu.Zatwierdzony)
            .Where(order => order.ZmianaId == _shiftNumber)
            .Where(order => order.Data.Month == month)
            .OrderBy(order => order.Data)
            .ToList();

        var fullOrders = new List<RozkazDzienny>(matchingOrders.Count);
        foreach (var summary in matchingOrders)
        {
            var fullOrder = await _services.Skrybek.Rozkaz.GetByIdAsync(summary.Id);
            if (fullOrder is not null)
            {
                fullOrders.Add(fullOrder);
            }
        }

        return fullOrders;
    }

    private static Dictionary<string, DutyAssignmentsRowViewModel> BuildNameLookup(
        IReadOnlyList<BoberFunkcjonariusz> personnel,
        IReadOnlyList<DutyAssignmentsRowViewModel> rows)
    {
        var lookup = new Dictionary<string, DutyAssignmentsRowViewModel>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < personnel.Count; index++)
        {
            var person = personnel[index];
            var row = rows[index];
            AddIfMissing(lookup, person.PelneImieNazwisko, row);
            AddIfMissing(lookup, $"{person.Stopien} {person.Nazwisko}".Trim(), row);
            AddIfMissing(lookup, person.Nazwisko, row);
        }

        return lookup;
    }

    private static void AddIfMissing(
        IDictionary<string, DutyAssignmentsRowViewModel> lookup,
        string value,
        DutyAssignmentsRowViewModel row)
    {
        var normalized = NormalizeName(value);
        if (!string.IsNullOrEmpty(normalized) && !lookup.ContainsKey(normalized))
        {
            lookup[normalized] = row;
        }
    }

    private static DutyAssignmentsRowViewModel? FindRow(
        IReadOnlyDictionary<int, DutyAssignmentsRowViewModel> rowsById,
        IReadOnlyDictionary<string, DutyAssignmentsRowViewModel> rowsByName,
        PozycjaSluzby assignment)
    {
        if (assignment.FunkcjonariuszId is int personId
            && rowsById.TryGetValue(personId, out var rowById))
        {
            return rowById;
        }

        var normalizedName = NormalizeName(assignment.Nazwisko);
        return string.IsNullOrEmpty(normalizedName)
            ? null
            : rowsByName.GetValueOrDefault(normalizedName);
    }

    private static string NormalizeName(string? value) =>
        string.Join(' ', (value ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static string MapRoleCode(StanowiskoSluzby position) => position switch
    {
        StanowiskoSluzby.DowodcaZmiany => "DZ",
        StanowiskoSluzby.DyzurnyPAJRG => "PA",
        StanowiskoSluzby.SzefZmiany => "SZ",
        StanowiskoSluzby.Garazomistrz => "GA",
        StanowiskoSluzby.DowodcaDzialanRatowniczychSGRWN => "KPP",
        StanowiskoSluzby.Bosman => "BO",
        StanowiskoSluzby.Sonarzysta => "SO",
        StanowiskoSluzby.PodoficerDyzurny => "PD",
        StanowiskoSluzby.StrazakDyzurny => "SD",
        _ => string.Empty
    };
}
