using Chomik.Core.Models;
using Chomik.Core.Security;

namespace Chomik.Services.Security;

public interface IUserAccountService
{
    Task<IReadOnlyList<UserAccount>> GetManageableUsersAsync(
        SessionUser manager,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        SessionUser manager,
        int userId,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task ResetToDefaultAsync(
        SessionUser manager,
        int userId,
        CancellationToken cancellationToken = default);
}
