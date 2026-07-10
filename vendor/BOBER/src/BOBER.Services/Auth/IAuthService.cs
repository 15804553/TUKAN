using BOBER.Core.Models;

namespace BOBER.Services.Auth;

public interface IAuthService
{
    Task<IReadOnlyList<string>> GetLoginsAsync(CancellationToken cancellationToken = default);
    bool TryAuthenticate(string login, string password, out string errorMessage);
    SessionInfo? CurrentSession { get; }
    void Logout();
}
