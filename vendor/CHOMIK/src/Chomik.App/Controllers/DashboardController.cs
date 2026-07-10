using System.Diagnostics;
using Chomik.App.Export;
using Chomik.App.ViewModels;
using Chomik.Core;
using Chomik.Core.GeneralView;
using Chomik.Core.Enums;
using Chomik.Core.Models;
using Chomik.Services;
using Chomik.Services.Personnel;

namespace Chomik.App.Controllers;

public sealed class DashboardController(AppServices services)
{
    private string? _personnelCacheKey;
    private IReadOnlyList<FunkcjonariuszRowViewModel>? _personnelCache;

    public bool CanEditPersonnel => services.Auth.CurrentUser?.CanEditPersonnel ?? false;

    public bool ShowUprawnieniaAlerts => services.Auth.CurrentUser?.IsShiftScoped ?? false;

    public bool CanManagePasswords => services.Auth.CurrentUser is { CanResetShiftPasswords: true }
        or { CanResetAllPasswords: true };

    public bool CanFilterByShift => services.Auth.CurrentUser?.CanViewAllShifts ?? false;

    public bool CanManageSettings => services.Auth.CurrentUser?.CanManageSettings ?? false;

    public bool CanCustomizeGeneralViewColumns =>
        services.Auth.CurrentUser?.CanCustomizeGeneralViewColumns ?? false;

    public bool ShowSettingsNavButton => services.Auth.CurrentUser?.ShowSettingsNavButton ?? false;

    public bool CanViewSensitiveColumns => services.Auth.CurrentUser?.CanViewSensitiveData ?? false;

    public bool CanEditGeneralViewDates => services.Auth.CurrentUser?.CanEditGeneralViewDates ?? false;

    public bool CanEditGeneralViewStopien => services.Auth.CurrentUser?.CanEditGeneralViewStopien ?? false;

    public bool IsPaUser => services.Auth.CurrentUser?.IsPaUser ?? false;

    public bool ShowGeneralViewNavButton => services.Auth.CurrentUser?.ShowGeneralViewNavButton ?? true;

    public bool HideTelefonColumn => services.Auth.CurrentUser?.HideTelefonInGeneralView ?? false;

    public bool HideGeneralViewShiftColumn => services.Auth.CurrentUser?.HideGeneralViewShiftColumn ?? false;

    public bool ShowInneUwagiColumn => services.Auth.CurrentUser?.IsShiftScoped ?? false;

    public bool CanEditGeneralViewShift => services.Auth.CurrentUser?.CanEditGeneralViewShift ?? false;

    public bool CanOpenPersonnelProfile => services.Auth.CurrentUser?.IsDcaJrgUser ?? false;

    public bool IsAdministrator => services.Auth.CurrentUser?.IsAdministrator ?? false;

    public bool CanViewGeneralView => services.Auth.CurrentUser?.CanViewGeneralView ?? true;

    public bool CanCreatePersonnelList => services.Auth.CurrentUser?.CanCreatePersonnelList ?? false;

    public bool ExportPersonnelListUsesOwnShiftOnly =>
        services.Auth.CurrentUser?.IsShiftScoped == true && !IsPaUser;

    public int? PersonnelListExportShiftNumber => services.Auth.CurrentUser?.ShiftNumber;

    public async Task<IReadOnlyList<ShiftFilterOption>> GetShiftFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!CanFilterByShift && services.Auth.CurrentUser?.ShiftNumber is int ownShift)
        {
            return [new ShiftFilterOption { Label = $"Zmiana {ownShift}", Value = ownShift }];
        }

        var options = new List<ShiftFilterOption>
        {
            new() { Label = "Wszystkie zmiany", Value = null },
            new() { Label = "Zmiana 1", Value = 1 },
            new() { Label = "Zmiana 2", Value = 2 },
            new() { Label = "Zmiana 3", Value = 3 }
        };

        return options;
    }

    public async Task<IReadOnlyList<SlownikItem>> GetStopnieAsync(
        CancellationToken cancellationToken = default)
    {
        var dictionaries = await services.Funkcjonariusze.GetDictionariesAsync(cancellationToken);
        return dictionaries.Stopnie;
    }

    public async Task<IReadOnlyList<PermissionFilterOption>> GetPermissionFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var dictionaries = await services.Funkcjonariusze.GetDictionariesAsync(cancellationToken);
        var options = new List<PermissionFilterOption>
        {
            new() { Label = "Wszystkie uprawnienia" }
        };

        options.AddRange(
            dictionaries.TypyUprawnien
                .OrderBy(t => t.Nazwa)
                .ThenBy(t => t.Podtyp)
                .Select(t => new PermissionFilterOption
                {
                    Label = t.Etykieta,
                    Nazwa = t.Nazwa,
                    Podtyp = t.Podtyp
                }));

        return options;
    }

    public void InvalidatePersonnelCache()
    {
        _personnelCacheKey = null;
        _personnelCache = null;
    }

    public async Task<PersonnelLoadResult> LoadPersonnelAsync(
        FunkcjonariuszRowFilter filter,
        CancellationToken cancellationToken = default)
    {
        if (!CanViewGeneralView)
        {
            throw new UnauthorizedAccessException("Brak dostępu do widoku personelu.");
        }

        var cacheKey = BuildPersonnelCacheKey(filter);
        if (_personnelCacheKey == cacheKey && _personnelCache is not null)
        {
            return new PersonnelLoadResult
            {
                Rows = _personnelCache,
                DatabaseSeconds = 0,
                MappingSeconds = 0,
                FromCache = true
            };
        }

        var user = services.Auth.CurrentUser
            ?? throw new InvalidOperationException("Brak zalogowanego użytkownika.");

        var dbStopwatch = Stopwatch.StartNew();
        var items = await services.Funkcjonariusze.GetFilteredAsync(
            user,
            new FunkcjonariuszFilter
            {
                NumerZmiany = filter.NumerZmiany,
                UprawnienieNazwa = filter.UprawnienieNazwa,
                UprawnieniePodtyp = filter.UprawnieniePodtyp,
                Szukaj = filter.Szukaj
            },
            cancellationToken).ConfigureAwait(false);
        dbStopwatch.Stop();

        var mapStopwatch = Stopwatch.StartNew();
        var rows = await Task.Run(() => MapPersonnelRows(items, filter), cancellationToken).ConfigureAwait(false);
        mapStopwatch.Stop();

        _personnelCacheKey = cacheKey;
        _personnelCache = rows;
        return new PersonnelLoadResult
        {
            Rows = rows,
            DatabaseSeconds = dbStopwatch.Elapsed.TotalSeconds,
            MappingSeconds = mapStopwatch.Elapsed.TotalSeconds,
            FromCache = false
        };
    }

    private static string BuildPersonnelCacheKey(FunkcjonariuszRowFilter filter) =>
        $"{filter.NumerZmiany}|{filter.UprawnienieNazwa}|{filter.UprawnieniePodtyp}|{filter.Szukaj}";

    private List<FunkcjonariuszRowViewModel> MapPersonnelRows(
        IReadOnlyList<FunkcjonariuszListItem> items,
        FunkcjonariuszRowFilter filter)
    {
        var hasSelectedPermission = !string.IsNullOrWhiteSpace(filter.UprawnienieNazwa);
        var selectedNazwa = filter.UprawnienieNazwa?.Trim();
        var selectedPodtyp = filter.UprawnieniePodtyp?.Trim();

        return items.Select(item =>
        {
            var f = item.Entity;
            UprawnieniePrzypisanie? wybraneUprawnienie = null;
            if (hasSelectedPermission)
            {
                wybraneUprawnienie = item.Uprawnienia
                    .FirstOrDefault(u =>
                        u.Nazwa.Equals(selectedNazwa, StringComparison.OrdinalIgnoreCase) &&
                        (string.IsNullOrWhiteSpace(selectedPodtyp)
                            ? string.IsNullOrWhiteSpace(u.Podtyp)
                            : selectedPodtyp.Equals(u.Podtyp, StringComparison.OrdinalIgnoreCase)));
            }

            var uprawnieniaAlert = ShowUprawnieniaAlerts
                ? UprawnieniaAlertEvaluator.Evaluate(item.Uprawnienia)
                : UprawnieniaAlertSummary.Empty;

            return new FunkcjonariuszRowViewModel
            {
                FunkcjonariuszId = f.Id,
                WybraneUprawnieniePrzypisanieId = wybraneUprawnienie?.Id,
                NumerZmiany = f.NumerZmiany,
                CanEditZmiana = CanEditGeneralViewShift,
                CanEditStopien = CanEditGeneralViewStopien,
                StopienId = f.StopienId,
                Stopien = f.Stopien,
                PelneImieNazwisko = f.PelneImieNazwisko,
                Stanowisko = f.Stanowisko,
                Telefon = f.Telefon,
                DataWstepieniaDoSluzby = f.DataWstepieniaDoSluzby,
                StazLat = f.StazLat ?? StazCalculator.CalculateServiceYears(f.DataWstepieniaDoSluzby),
                BadaniaOkresoweDo = f.BadaniaOkresoweDo,
                KomoraDymowaDo = f.KomoraDymowaDo,
                KppDo = f.KppDo,
                UprawnieniaSkrot = item.UprawnieniaSkrot,
                HasUprawnieniaAlert = uprawnieniaAlert.HasAlert,
                UprawnieniaAlertSeverity = uprawnieniaAlert.Severity,
                UprawnieniaAlertTooltip = uprawnieniaAlert.Tooltip,
                WybraneUprawnienieWazneDo = wybraneUprawnienie?.WazneDo,
                DodatekMotywacyjny = item.ShowSensitiveFields && f.DodatekMotywacyjny.HasValue
                    ? f.DodatekMotywacyjny.Value.ToString("N2")
                    : null,
                DataAwansuStopien = item.ShowSensitiveFields && f.DataAwansuStopien.HasValue
                    ? DateDisplayFormat.Format(f.DataAwansuStopien)
                    : null,
                OdznaczeniaSkrot = item.ShowSensitiveFields ? item.OdznaczeniaSkrot : null,
                InneUwagi = string.IsNullOrWhiteSpace(f.InformacjaDodatkowa) ? null : f.InformacjaDodatkowa
            };
        }).ToList();
    }

    public Task SaveStopienAsync(
        FunkcjonariuszRowViewModel row,
        int stopienId,
        CancellationToken cancellationToken = default) =>
        services.Funkcjonariusze.SaveGeneralViewStopienAsync(
            services.Auth.CurrentUser
                ?? throw new InvalidOperationException("Brak zalogowanego użytkownika."),
            row.FunkcjonariuszId,
            stopienId,
            cancellationToken);

    public Task SaveNumerZmianyAsync(
        FunkcjonariuszRowViewModel row,
        int numerZmiany,
        CancellationToken cancellationToken = default) =>
        services.Funkcjonariusze.SaveGeneralViewNumerZmianyAsync(
            services.Auth.CurrentUser
                ?? throw new InvalidOperationException("Brak zalogowanego użytkownika."),
            row.FunkcjonariuszId,
            numerZmiany,
            cancellationToken);

    public Task SaveTerminyMedyczneAsync(
        FunkcjonariuszRowViewModel row,
        CancellationToken cancellationToken = default) =>
        services.Funkcjonariusze.SaveGeneralViewTerminyMedyczneAsync(
            services.Auth.CurrentUser
                ?? throw new InvalidOperationException("Brak zalogowanego użytkownika."),
            row.FunkcjonariuszId,
            row.BadaniaOkresoweDo,
            row.KomoraDymowaDo,
            row.KppDo,
            cancellationToken);

    public Task SaveUprawnienieWazneDoAsync(
        FunkcjonariuszRowViewModel row,
        CancellationToken cancellationToken = default)
    {
        if (row.WybraneUprawnieniePrzypisanieId is not int uprawnienieId)
        {
            throw new InvalidOperationException("Brak wybranego uprawnienia do zapisu.");
        }

        return services.Funkcjonariusze.SaveGeneralViewUprawnienieWazneDoAsync(
            services.Auth.CurrentUser
                ?? throw new InvalidOperationException("Brak zalogowanego użytkownika."),
            uprawnienieId,
            row.WybraneUprawnienieWazneDo,
            cancellationToken);
    }

    public Task<IReadOnlyList<string>> GetPersonnelNamesForExportAsync(
        int? numerZmiany,
        CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser
            ?? throw new InvalidOperationException("Brak zalogowanego użytkownika.");

        return services.Funkcjonariusze.GetPersonnelNamesForExportAsync(user, numerZmiany, cancellationToken);
    }

    public static void ExportPersonnelListToExcel(IReadOnlyList<string> fullNames, string filePath) =>
        PersonnelListExcelExporter.Export(fullNames, filePath);

    public Task<GeneralViewColumnPreferences> GetGeneralViewColumnPreferencesAsync(
        CancellationToken cancellationToken = default)
    {
        var login = services.Auth.CurrentUser?.Login
            ?? throw new InvalidOperationException("Brak zalogowanego użytkownika.");
        return services.Settings.GetGeneralViewColumnPreferencesAsync(login, cancellationToken);
    }

    public async Task<PersonnelProfileViewModel?> GetPersonnelProfileAsync(
        int funkcjonariuszId,
        CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser
            ?? throw new InvalidOperationException("Brak zalogowanego użytkownika.");

        var entity = await services.Funkcjonariusze.GetForGeneralViewProfileAsync(
            user,
            funkcjonariuszId,
            cancellationToken);

        return entity is null ? null : PersonnelProfileMapper.ToViewModel(entity);
    }

    public void Logout() => services.Auth.Logout();
}
