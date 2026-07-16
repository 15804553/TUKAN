using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Data.Repositories;
using BOBER.Services.Grafik;
using BOBER.Services.Personnel;
using BOBER.Services.Settings;

namespace BOBER.Services.Urlop;

public sealed class UrlopPlanService(
    IUrlopPlanRepository urlopPlanRepository,
    IGrafikRepository grafikRepository,
    ShiftCalendarEngine calendar,
    IFunkcjonariuszService funkcjonariusze,
    ISettingsService settings,
    UrlopPlanValidator validator,
    UrlopPlanExcelService excelService) : IUrlopPlanService
{
    public Task<IReadOnlyList<UrlopPlanWpis>> GetYearAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default) =>
        urlopPlanRepository.GetByZmianaAndYearAsync(zmianaId, rok, cancellationToken);

    public Task<IReadOnlyList<UrlopPlanWpis>> GetMonthAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        urlopPlanRepository.GetByZmianaAndMonthAsync(zmianaId, rok, miesiac, cancellationToken);

    public Task SetWpisAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        string typUrlopu,
        CancellationToken cancellationToken = default) =>
        urlopPlanRepository.UpsertAsync(new UrlopPlanWpis
        {
            FunkcjonariuszId = funkcjonariuszId,
            ZmianaId = zmianaId,
            Rok = rok,
            Miesiac = miesiac,
            Dzien = dzien,
            TypUrlopu = UrlopTypy.Normalize(typUrlopu)
        }, cancellationToken);

    public Task ClearWpisAsync(
        int funkcjonariuszId,
        int zmianaId,
        int rok,
        int miesiac,
        int dzien,
        CancellationToken cancellationToken = default) =>
        urlopPlanRepository.DeleteAsync(funkcjonariuszId, zmianaId, rok, miesiac, dzien, cancellationToken);

    public Task ClearHalfYearAsync(
        int zmianaId,
        int rok,
        int polrocze,
        CancellationToken cancellationToken = default) =>
        urlopPlanRepository.DeleteByHalfYearAsync(zmianaId, rok, polrocze, cancellationToken);

    public Task ClearYearAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default) =>
        urlopPlanRepository.DeleteByYearAsync(zmianaId, rok, cancellationToken);

    public async Task<IReadOnlyList<UrlopPlanValidationIssue>> ValidateAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default)
    {
        var wpisy = await urlopPlanRepository.GetByZmianaAndYearAsync(zmianaId, rok, cancellationToken);
        var osoby = await funkcjonariusze.GetByZmianaAsync(zmianaId, cancellationToken);
        var nazwiska = osoby.ToDictionary(f => f.Id, f => UrlopNameMatcher.ToExcelFormat(f.Imie, f.Nazwisko));

        var workDays = await calendar.GetWorkDaysAsync(zmianaId, rok, cancellationToken);
        var workDaySet = workDays.ToHashSet();

        bool IsWorkDay(int zid, DateOnly date) =>
            workDaySet.Contains(date);

        var maxNaSluzbie = await settings.GetMaxUrlopowNaSluzbieAsync(zmianaId, cancellationToken);
        return validator.Validate(zmianaId, rok, wpisy, nazwiska, IsWorkDay, maxNaSluzbie);
    }

    public async Task<UrlopPlanSyncResult> ApplyToGrafikAsync(
        int zmianaId,
        int rok,
        CancellationToken cancellationToken = default)
    {
        var planWpisy = await urlopPlanRepository.GetByZmianaAndYearAsync(zmianaId, rok, cancellationToken);
        var grafikWpisy = await grafikRepository.GetByZmianaAndYearAsync(zmianaId, rok, cancellationToken);
        var grafikLookup = grafikWpisy
            .GroupBy(w => (w.FunkcjonariuszId, w.Miesiac, w.Dzien))
            .ToDictionary(g => g.Key, g => g.Last());

        var osoby = await funkcjonariusze.GetByZmianaAsync(zmianaId, cancellationToken);
        var nazwiska = osoby.ToDictionary(f => f.Id, f => UrlopNameMatcher.ToExcelFormat(f.Imie, f.Nazwisko));

        var applied = 0;
        var updated = 0;
        var skipped = 0;
        var skippedDetails = new List<string>();

        foreach (var wpis in planWpisy)
        {
            var key = (wpis.FunkcjonariuszId, wpis.Miesiac, wpis.Dzien);
            if (grafikLookup.TryGetValue(key, out var existing) && !existing.IsAuto)
            {
                skipped++;
                var name = nazwiska.GetValueOrDefault(wpis.FunkcjonariuszId, $"ID {wpis.FunkcjonariuszId}");
                skippedDetails.Add($"{name} — {wpis.Dzien:00}.{wpis.Miesiac:00}.{rok} (ręczny wpis: {existing.TypWpisu})");
                continue;
            }

            var isUpdate = grafikLookup.ContainsKey(key);
            await grafikRepository.UpsertAsync(new GrafikWpis
            {
                FunkcjonariuszId = wpis.FunkcjonariuszId,
                ZmianaId = zmianaId,
                Rok = rok,
                Miesiac = wpis.Miesiac,
                Dzien = wpis.Dzien,
                TypWpisu = GrafikWpisTypy.Urlop,
                IsAuto = true
            }, cancellationToken);

            if (isUpdate)
                updated++;
            else
                applied++;
        }

        return new UrlopPlanSyncResult
        {
            AppliedCount = applied,
            UpdatedCount = updated,
            SkippedManualCount = skipped,
            SkippedManualDetails = skippedDetails
        };
    }

    public async Task ImportFromExcelAsync(
        int zmianaId,
        int rok,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var osoby = await funkcjonariusze.GetByZmianaAsync(zmianaId, cancellationToken);
        var imported = excelService.Import(filePath, rok, osoby);
        var existing = await urlopPlanRepository.GetByZmianaAndYearAsync(zmianaId, rok, cancellationToken);

        var merged = existing
            .Where(w => imported.All(i =>
                i.FunkcjonariuszId != w.FunkcjonariuszId
                || i.Miesiac != w.Miesiac
                || i.Dzien != w.Dzien))
            .Concat(imported.Select(w => new UrlopPlanWpis
            {
                FunkcjonariuszId = w.FunkcjonariuszId,
                ZmianaId = zmianaId,
                Rok = rok,
                Miesiac = w.Miesiac,
                Dzien = w.Dzien,
                TypUrlopu = w.TypUrlopu
            }))
            .ToList();

        await urlopPlanRepository.ReplaceYearAsync(zmianaId, rok, merged, cancellationToken);
    }

    public void ExportToExcel(
        int zmianaId,
        int rok,
        IReadOnlyList<Funkcjonariusz> funkcjonariusze,
        IReadOnlyList<UrlopPlanWpis> wpisy,
        string filePath) =>
        excelService.Export(zmianaId, rok, funkcjonariusze, wpisy, filePath);
}
