using Chomik.Services;

namespace Chomik.App.Controllers;

public sealed class LoginController(AppServices services)
{
    public Task<IReadOnlyList<string>> GetLoginsAsync(CancellationToken cancellationToken = default) =>
        services.Auth.GetAvailableLoginsAsync(cancellationToken);

    public bool TryAuthenticate(string login, string password, out string errorMessage)
    {
        if (services.Auth.TryLogin(login, password, out var error))
        {
            errorMessage = string.Empty;
            return true;
        }

        errorMessage = error ?? "Logowanie nie powiodło się.";
        return false;
    }
}
