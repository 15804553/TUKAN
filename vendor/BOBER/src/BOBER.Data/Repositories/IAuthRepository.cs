using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IAuthRepository
{
    Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdatePasswordAsync(int userId, string hash, string salt, CancellationToken cancellationToken = default);
}
