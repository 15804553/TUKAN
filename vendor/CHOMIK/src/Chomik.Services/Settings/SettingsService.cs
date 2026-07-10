using Chomik.Core.GeneralView;
using Chomik.Core.Security;
using Chomik.Data.Repositories;

namespace Chomik.Services.Settings;

public sealed class SettingsService(ISettingsRepository settingsRepository) : ISettingsService
{
    public async Task<GeneralViewColumnPreferences> GetGeneralViewColumnPreferencesAsync(
        string login,
        CancellationToken cancellationToken = default)
    {
        var raw = await settingsRepository.GetAsync(
            ISettingsService.GeneralViewColumnsKey(login),
            cancellationToken);
        return GeneralViewColumnPreferences.Deserialize(raw);
    }

    public async Task SaveGeneralViewColumnPreferencesAsync(
        SessionUser user,
        GeneralViewColumnPreferences preferences,
        CancellationToken cancellationToken = default)
    {
        if (!user.CanCustomizeGeneralViewColumns)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do zapisu widoczności kolumn widoku ogólnego.");
        }

        await settingsRepository.SetAsync(
            ISettingsService.GeneralViewColumnsKey(user.Login),
            preferences.Serialize(),
            cancellationToken);
    }
}
