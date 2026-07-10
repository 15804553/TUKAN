using Chomik.Core.GeneralView;
using Chomik.Core.Security;

namespace Chomik.Services.Settings;

public interface ISettingsService
{
    static string GeneralViewColumnsKey(string login) => $"GeneralViewColumns:{login}";

    Task<GeneralViewColumnPreferences> GetGeneralViewColumnPreferencesAsync(
        string login,
        CancellationToken cancellationToken = default);

    Task SaveGeneralViewColumnPreferencesAsync(
        SessionUser user,
        GeneralViewColumnPreferences preferences,
        CancellationToken cancellationToken = default);
}
