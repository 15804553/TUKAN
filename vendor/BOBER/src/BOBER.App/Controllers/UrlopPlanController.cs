using System.Windows.Media;

using BOBER.Core.Constants;

using BOBER.Core.Models;

using BOBER.Services;



namespace BOBER.App.Controllers;



public sealed class UrlopPlanController(AppServices services, int zmianaId, string nazwaZmiany)

{

    public int ZmianaId { get; } = zmianaId;

    public string NazwaZmiany { get; } = nazwaZmiany;

    public int DefaultPlanYear => DateTime.Today.Year + 1;



    private IReadOnlyList<Funkcjonariusz>? _funkcjonariusze;

    private Dictionary<string, string>? _kolory;

    public int MaxUrlopowNaSluzbie { get; private set; } = UrlopPlanInstructions.DefaultMaxUrlopowNaSluzbie;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _funkcjonariusze = await services.Funkcjonariusze.GetByZmianaAsync(ZmianaId, cancellationToken);
        var kolory = await services.Kolory.GetAllAsync(cancellationToken);
        _kolory = kolory.ToDictionary(k => k.KluczRoli, k => k.KolorHex, StringComparer.OrdinalIgnoreCase);
        MaxUrlopowNaSluzbie = await services.Settings.GetMaxUrlopowNaSluzbieAsync(ZmianaId, cancellationToken);
    }



    public IReadOnlyList<Funkcjonariusz> GetFunkcjonariusze() => _funkcjonariusze ?? [];



    public async Task<HashSet<int>> GetWorkDaysForMonthAsync(

        int rok,

        int miesiac,

        CancellationToken cancellationToken = default)

    {

        var allWorkDays = await services.Calendar.GetWorkDaysAsync(ZmianaId, rok, cancellationToken);

        return allWorkDays

            .Where(d => d.Month == miesiac)

            .Select(d => d.Day)

            .ToHashSet();

    }



    public SolidColorBrush GetDzienSluzbyBrush()
    {
        var klucz = RoleKeys.KalendarzKluczForZmiana(ZmianaId);
        var defaultHex = RoleKeys.GetDefaultKolorHex(klucz);
        var hex = GetKolorHex(klucz, RoleKeys.DomyslneKoloryKalendarza);
        var fallback = ParseColor(defaultHex, Color.FromRgb(0xFF, 0xFF, 0x00));
        return ParseBrush(hex, fallback);
    }



    public async Task<IReadOnlyList<UrlopPlanWpis>> GetYearAsync(int rok, CancellationToken ct = default) =>

        await services.UrlopPlan.GetYearAsync(ZmianaId, rok, ct);



    public async Task<IReadOnlyList<UrlopPlanWpis>> GetMonthAsync(int rok, int miesiac, CancellationToken ct = default) =>

        await services.UrlopPlan.GetMonthAsync(ZmianaId, rok, miesiac, ct);



    public Task SetWpisAsync(int fid, int rok, int miesiac, int dzien, string typ, CancellationToken ct = default) =>

        services.UrlopPlan.SetWpisAsync(fid, ZmianaId, rok, miesiac, dzien, typ, ct);



    public Task ClearWpisAsync(int fid, int rok, int miesiac, int dzien, CancellationToken ct = default) =>

        services.UrlopPlan.ClearWpisAsync(fid, ZmianaId, rok, miesiac, dzien, ct);



    public Task<IReadOnlyList<UrlopPlanValidationIssue>> ValidateAsync(int rok, CancellationToken ct = default) =>

        services.UrlopPlan.ValidateAsync(ZmianaId, rok, ct);



    public Task<UrlopPlanSyncResult> ApplyToGrafikAsync(int rok, CancellationToken ct = default) =>

        services.UrlopPlan.ApplyToGrafikAsync(ZmianaId, rok, ct);



    public Task ImportFromExcelAsync(int rok, string path, CancellationToken ct = default) =>

        services.UrlopPlan.ImportFromExcelAsync(ZmianaId, rok, path, ct);



    public async Task ExportToExcelAsync(int rok, string path, CancellationToken ct = default)

    {

        if (_funkcjonariusze is null)

            await LoadAsync(ct);



        var wpisy = await services.UrlopPlan.GetYearAsync(ZmianaId, rok, ct);

        services.UrlopPlan.ExportToExcel(ZmianaId, rok, _funkcjonariusze ?? [], wpisy, path);

    }



    public Task ClearUrlopPlanHalfYearAsync(int rok, int polrocze, CancellationToken ct = default) =>

        services.UrlopPlan.ClearHalfYearAsync(ZmianaId, rok, polrocze, ct);



    public Task ClearYearAsync(int rok, CancellationToken ct = default) =>

        services.UrlopPlan.ClearYearAsync(ZmianaId, rok, ct);



    private string GetKolorHex(string klucz, IReadOnlyDictionary<string, string> domyslne)

    {

        if (_kolory is not null && _kolory.TryGetValue(klucz, out var hex))

            return hex;

        return domyslne.TryGetValue(klucz, out var defaultHex) ? defaultHex : RoleKeys.GetDefaultKolorHex(klucz);

    }



    private static Color ParseColor(string hex, Color fallback)

    {

        try

        {

            return (Color)ColorConverter.ConvertFromString(hex)!;

        }

        catch

        {

            return fallback;

        }

    }



    private static SolidColorBrush ParseBrush(string hex, Color fallback)

    {

        try

        {

            var color = (Color)ColorConverter.ConvertFromString(hex)!;

            return new SolidColorBrush(color);

        }

        catch

        {

            return new SolidColorBrush(fallback);

        }

    }

}


