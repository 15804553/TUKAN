using Chomik.Core.Security;

namespace Chomik.Services.Security;

public interface IAuthService
{
    SessionUser? CurrentUser { get; }
    Task<IReadOnlyList<string>> GetAvailableLoginsAsync(CancellationToken cancellationToken = default);
    bool TryLogin(string login, string? password, out string? errorMessage);
    void Logout();
}
