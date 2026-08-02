using Chomik.Core.GeneralView;
using Chomik.Core.Models;
using Chomik.Services;

namespace Chomik.App.Controllers;

public sealed class SettingsController(AppServices services)
{
    public bool CanManagePermissionTypes =>
        services.Auth.CurrentUser?.CanManagePermissionTypes ?? false;

    public bool CanManageSettings =>
        services.Auth.CurrentUser?.CanManageSettings ?? false;

    public bool CanCustomizeGeneralViewColumns =>
        services.Auth.CurrentUser?.CanCustomizeGeneralViewColumns ?? false;

    public bool ShowUprawnieniaAlertColumnOption =>
        services.Auth.CurrentUser?.IsShiftScoped ?? false;

    public bool ShowZmianaColumnInSettings =>
        services.Auth.CurrentUser?.CanViewAllShifts ?? false;

    public bool ShowInneUwagiColumnInSettings =>
        services.Auth.CurrentUser?.IsShiftScoped ?? false;

    public IReadOnlyList<GeneralViewColumnId> GetSelectableGeneralViewColumns() =>
        GeneralViewColumnPreferences.OptionalColumns
            .Where(id => id != GeneralViewColumnId.UprawnieniaAlert || ShowUprawnieniaAlertColumnOption)
            .Where(id => id != GeneralViewColumnId.Zmiana || ShowZmianaColumnInSettings)
            .Where(id => id != GeneralViewColumnId.InneUwagi || ShowInneUwagiColumnInSettings)
            .ToList();

    public Task<IReadOnlyList<TypUprawnienia>> LoadTypyUprawnienAsync(
        CancellationToken cancellationToken = default) =>
        services.UprawnieniaSlownik.GetAllAsync(cancellationToken);

    public Task<int> AddTypUprawnieniaAsync(
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken = default)
    {
        var user = RequireUser();
        return AddTypUprawnieniaCoreAsync(user, nazwa, podtyp, wymagaDaty, cancellationToken);
    }

    private async Task<int> AddTypUprawnieniaCoreAsync(
        Core.Security.SessionUser user,
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken)
    {
        var id = await services.UprawnieniaSlownik.AddAsync(user, nazwa, podtyp, wymagaDaty, cancellationToken);
        InvalidateDictionaries();
        return id;
    }

    public Task UpdateTypUprawnieniaAsync(
        int id,
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken = default)
    {
        var user = RequireUser();
        return UpdateTypUprawnieniaCoreAsync(user, id, nazwa, podtyp, wymagaDaty, cancellationToken);
    }

    private async Task UpdateTypUprawnieniaCoreAsync(
        Core.Security.SessionUser user,
        int id,
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken)
    {
        await services.UprawnieniaSlownik.UpdateAsync(user, id, nazwa, podtyp, wymagaDaty, cancellationToken);
        InvalidateDictionaries();
    }

    public Task<IReadOnlyList<SlownikItem>> LoadStopnieAsync(CancellationToken cancellationToken = default) =>
        services.PersonelSlowniki.GetStopnieAsync(cancellationToken);

    public Task<IReadOnlyList<SlownikItem>> LoadStanowiskaAsync(CancellationToken cancellationToken = default) =>
        services.PersonelSlowniki.GetStanowiskaAsync(cancellationToken);

    public Task<IReadOnlyList<TypOdznaczenia>> LoadTypyOdznaczenAsync(
        CancellationToken cancellationToken = default) =>
        services.PersonelSlowniki.GetTypyOdznaczenAsync(cancellationToken);

    public async Task<int> AddStopienAsync(string nazwa, CancellationToken cancellationToken = default)
    {
        var id = await services.PersonelSlowniki.AddStopienAsync(RequireUser(), nazwa, cancellationToken);
        InvalidateDictionaries();
        return id;
    }

    public async Task UpdateStopienAsync(int id, string nazwa, CancellationToken cancellationToken = default)
    {
        await services.PersonelSlowniki.UpdateStopienAsync(RequireUser(), id, nazwa, cancellationToken);
        InvalidateDictionaries();
    }

    public async Task DeleteStopienAsync(int id, CancellationToken cancellationToken = default)
    {
        await services.PersonelSlowniki.DeleteStopienAsync(RequireUser(), id, cancellationToken);
        InvalidateDictionaries();
    }

    public async Task<int> AddStanowiskoAsync(string nazwa, CancellationToken cancellationToken = default)
    {
        var id = await services.PersonelSlowniki.AddStanowiskoAsync(RequireUser(), nazwa, cancellationToken);
        InvalidateDictionaries();
        return id;
    }

    public async Task UpdateStanowiskoAsync(int id, string nazwa, CancellationToken cancellationToken = default)
    {
        await services.PersonelSlowniki.UpdateStanowiskoAsync(RequireUser(), id, nazwa, cancellationToken);
        InvalidateDictionaries();
    }

    public async Task DeleteStanowiskoAsync(int id, CancellationToken cancellationToken = default)
    {
        await services.PersonelSlowniki.DeleteStanowiskoAsync(RequireUser(), id, cancellationToken);
        InvalidateDictionaries();
    }

    public async Task<int> AddTypOdznaczeniaAsync(string nazwa, CancellationToken cancellationToken = default)
    {
        var id = await services.PersonelSlowniki.AddTypOdznaczeniaAsync(RequireUser(), nazwa, cancellationToken);
        InvalidateDictionaries();
        return id;
    }

    public async Task UpdateTypOdznaczeniaAsync(int id, string nazwa, CancellationToken cancellationToken = default)
    {
        await services.PersonelSlowniki.UpdateTypOdznaczeniaAsync(RequireUser(), id, nazwa, cancellationToken);
        InvalidateDictionaries();
    }

    public async Task DeleteTypOdznaczeniaAsync(int id, CancellationToken cancellationToken = default)
    {
        await services.PersonelSlowniki.DeleteTypOdznaczeniaAsync(RequireUser(), id, cancellationToken);
        InvalidateDictionaries();
    }

    public Task<GeneralViewColumnPreferences> GetGeneralViewColumnPreferencesAsync(
        CancellationToken cancellationToken = default)
    {
        var login = RequireUser().Login;
        return services.Settings.GetGeneralViewColumnPreferencesAsync(login, cancellationToken);
    }

    public Task SaveGeneralViewColumnPreferencesAsync(
        GeneralViewColumnPreferences preferences,
        CancellationToken cancellationToken = default) =>
        services.Settings.SaveGeneralViewColumnPreferencesAsync(RequireUser(), preferences, cancellationToken);

    private Core.Security.SessionUser RequireUser() =>
        services.Auth.CurrentUser
        ?? throw new InvalidOperationException("Brak zalogowanego użytkownika.");

    private void InvalidateDictionaries() => services.Funkcjonariusze.InvalidateDictionariesCache();
}
