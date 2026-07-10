using Chomik.Core.Constants;
using Chomik.Core.Enums;
using Chomik.Core.Models;
using Chomik.Core.Security;
using Chomik.Data.Repositories;

namespace Chomik.Services.Security;

public sealed class UserAccountService(IUserRepository userRepository) : IUserAccountService
{
    public async Task<IReadOnlyList<UserAccount>> GetManageableUsersAsync(
        SessionUser manager,
        CancellationToken cancellationToken = default)
    {
        var all = await userRepository.GetAllAsync(cancellationToken);

        if (manager.CanResetAllPasswords)
        {
            return all
                .Where(u => u.Role is not UserRole.Pa and not UserRole.Administrator)
                .OrderBy(u => u.Login, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (manager.CanResetShiftPasswords)
        {
            return all
                .Where(u => u.Role is UserRole.Zmiana1 or UserRole.Zmiana2 or UserRole.Zmiana3)
                .OrderBy(u => u.Login, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return [];
    }

    public async Task ChangePasswordAsync(
        SessionUser manager,
        int userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ValidateManagerAccess(manager);
        var user = await GetUserOrThrowAsync(userId, manager, cancellationToken);
        EnsurePasswordChangeAllowed(manager, user);

        if (user.Role == UserRole.Pa && !string.IsNullOrEmpty(newPassword))
        {
            throw new InvalidOperationException("Konto PA nie może mieć hasła.");
        }

        if (user.Role != UserRole.Pa && string.IsNullOrWhiteSpace(newPassword))
        {
            throw new InvalidOperationException("Hasło nie może być puste.");
        }

        if (user.Role == UserRole.Pa)
        {
            await userRepository.UpdatePasswordAsync(userId, string.Empty, string.Empty, cancellationToken);
            return;
        }

        var (hash, salt) = PasswordHasher.HashPassword(newPassword);
        await userRepository.UpdatePasswordAsync(userId, hash, salt, cancellationToken);
    }

    public async Task ResetToDefaultAsync(
        SessionUser manager,
        int userId,
        CancellationToken cancellationToken = default)
    {
        ValidateManagerAccess(manager);
        var user = await GetUserOrThrowAsync(userId, manager, cancellationToken);
        EnsurePasswordChangeAllowed(manager, user);
        var defaultPassword = DefaultCredentials.DefaultPasswords[user.Role];

        if (defaultPassword is null)
        {
            await userRepository.UpdatePasswordAsync(userId, string.Empty, string.Empty, cancellationToken);
            return;
        }

        var (hash, salt) = PasswordHasher.HashPassword(defaultPassword);
        await userRepository.UpdatePasswordAsync(userId, hash, salt, cancellationToken);
    }

    private static void ValidateManagerAccess(SessionUser manager)
    {
        if (!manager.CanResetShiftPasswords && !manager.CanResetAllPasswords)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do zarządzania hasłami.");
        }
    }

    private static void EnsurePasswordChangeAllowed(SessionUser manager, UserAccount target)
    {
        if (manager.IsDcaJrgUser
            && target.Login.Equals(manager.Login, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Użytkownik DCA JRG nie może zmieniać ani resetować własnego hasła.");
        }

        if (manager.CanResetAllPasswords
            && target.Role is UserRole.Pa or UserRole.Administrator)
        {
            throw new UnauthorizedAccessException("Administrator nie może zarządzać hasłem konta PA ani Administrator.");
        }
    }

    private async Task<UserAccount> GetUserOrThrowAsync(
        int userId,
        SessionUser manager,
        CancellationToken cancellationToken)
    {
        var users = await GetManageableUsersAsync(manager, cancellationToken);
        var user = users.FirstOrDefault(u => u.Id == userId)
            ?? throw new UnauthorizedAccessException("Nie można zarządzać tym użytkownikiem.");
        return user;
    }
}
