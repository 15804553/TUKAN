using Chomik.Core.Models;

namespace Chomik.Data.Repositories;

public interface IUserRepository
{
    Task<UserAccount?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAccount>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpdatePasswordAsync(int userId, string hash, string salt, CancellationToken cancellationToken = default);
}
