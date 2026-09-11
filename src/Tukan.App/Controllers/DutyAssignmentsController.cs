using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using BOBER.Core.Diagnostics;
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

    public int ShiftNumber => _shiftNumber;

    public int CurrentYear { get; } = DateTime.Today.Year;

    public async Task<HashSet<int>> GetWorkDaysForMonthAsync(int year, int month)
    {
        var allWorkDays = await _services.Bober.Calendar.GetWorkDaysAsync(_shiftNumber, year);
        return allWorkDays
            .Where(date => date.Month == month)
            .Select(date => date.Day)
            .ToHashSet();
    }

    public async Task<IReadOnlyList<DutyAssignmentsRowViewModel>> BuildRowsAsync(int year, int month)
    {
        var totalStopwatch = PerformanceDiagnostics.Start();
        var dataStopwatch = PerformanceDiagnostics.Start();
        // Ta sama kolejność co w grafiku służb (KolejnoscFunkcjonariuszy z BOBER).
        var personnel = await _services.Bober.Funkcjonariusze.GetByZmianaAsync(_shiftNumber);
        var uwagi = await _services.Bober.ObsadaFunkcji.GetUwagiMonthAsync(_shiftNumber, year, month);
        var orders = await GetOrdersForMonthAsync(year, month);
        PerformanceDiagnostics.Log(
            "ObsadaFunkcji.LoadMonth",
            "Dane",
            dataStopwatch,
            personnel.Count + uwagi.Count + orders.Count);

        var processingStopwatch = PerformanceDiagnostics.Start();
        var uwagiLookup = uwagi
            .GroupBy(u => u.FunkcjonariuszId)
            .ToDictionary(g => g.Key, g => g.Last().Tresc);

        var rows = personnel
            .Select((person, index) => new DutyAssignmentsRowViewModel
            {
                Numer = index + 1,
                FunkcjonariuszId = person.Id,
                ImieNazwisko = person.PelneImieNazwisko,
                UwagaMiesieczna = uwagiLookup.TryGetValue(person.Id, out var uwaga) ? uwaga : string.Empty
            })
            .ToList();

        var rowsById = personnel.Zip(rows).ToDictionary(pair => pair.First.Id, pair => pair.Second);
        var rowsByName = BuildNameLookup(personnel, rows);

        foreach (var order in orders)
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

        PerformanceDiagnostics.Log("ObsadaFunkcji.LoadMonth", "Przetwarzanie", processingStopwatch, rows.Count);
        PerformanceDiagnostics.Log("ObsadaFunkcji.LoadMonth", "Calkowity", totalStopwatch, rows.Count);
        return rows;
    }

    /// <summary>
    /// Rozkazy zmiany w miesiącu (robocze i zatwierdzone) — obsada z SŁUŻBY po „Zapisz”.
    /// </summary>
    private async Task<IReadOnlyList<RozkazDzienny>> GetOrdersForMonthAsync(int year, int month)
    {
        var orders = await _services.Skrybek.Rozkaz.GetForDutyAssignmentsAsync(year, _shiftNumber);
        return orders
            .Where(order => order.Data.Month == month)
            .OrderBy(order => order.Data)
            .ToList();
    }

    public async Task SetUwagaMiesiecznaAsync(
        int funkcjonariuszId,
        int year,
        int month,
        string tresc,
        CancellationToken cancellationToken = default)
    {
        var trimmed = tresc?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
        {
            await _services.Bober.ObsadaFunkcji.ClearUwagaMiesiecznaAsync(
                funkcjonariuszId, _shiftNumber, year, month, cancellationToken);
            return;
        }

        await _services.Bober.ObsadaFunkcji.SetUwagaMiesiecznaAsync(
            funkcjonariuszId, _shiftNumber, year, month, trimmed, cancellationToken);
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
            AddIfMissing(lookup, $"{person.Stopien} {person.Imie} {person.Nazwisko}".Trim(), row);
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
