using BOBER.Services;

namespace BOBER.App.Controllers;

public sealed class LoginController(AppServices services)
{
    public async Task<IReadOnlyList<string>> GetLoginsAsync(CancellationToken cancellationToken = default) =>
        await services.Auth.GetLoginsAsync(cancellationToken);

    public bool TryAuthenticate(string login, string password, out string errorMessage) =>
        services.Auth.TryAuthenticate(login, password, out errorMessage);
}
