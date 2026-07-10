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
        var user = services.Auth.CurrentUser
            ?? throw new InvalidOperationException("Brak zalogowanego użytkownika.");
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
        services.Funkcjonariusze.InvalidateDictionariesCache();
        return id;
    }

    public Task UpdateTypUprawnieniaAsync(
        int id,
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser
            ?? throw new InvalidOperationException("Brak zalogowanego użytkownika.");
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
        services.Funkcjonariusze.InvalidateDictionariesCache();
    }

    public Task<GeneralViewColumnPreferences> GetGeneralViewColumnPreferencesAsync(
        CancellationToken cancellationToken = default)
    {
        var login = services.Auth.CurrentUser?.Login
            ?? throw new InvalidOperationException("Brak zalogowanego użytkownika.");
        return services.Settings.GetGeneralViewColumnPreferencesAsync(login, cancellationToken);
    }

    public Task SaveGeneralViewColumnPreferencesAsync(
        GeneralViewColumnPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser
            ?? throw new InvalidOperationException("Brak zalogowanego użytkownika.");
        return services.Settings.SaveGeneralViewColumnPreferencesAsync(user, preferences, cancellationToken);
    }
}
