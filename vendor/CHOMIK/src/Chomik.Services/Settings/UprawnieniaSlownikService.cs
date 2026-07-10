using Chomik.Core.Models;
using Chomik.Core.Security;
using Chomik.Data.Repositories;

namespace Chomik.Services.Settings;

public sealed class UprawnieniaSlownikService(ISlownikRepository slownikRepository)
{
    public Task<IReadOnlyList<TypUprawnienia>> GetAllAsync(CancellationToken cancellationToken = default) =>
        slownikRepository.GetTypyUprawnienAsync(cancellationToken);

    public async Task<int> AddAsync(
        SessionUser user,
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken = default)
    {
        if (!user.CanManagePermissionTypes)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do zarządzania słownikiem uprawnień.");
        }

        if (string.IsNullOrWhiteSpace(nazwa))
        {
            throw new InvalidOperationException("Nazwa uprawnienia / kursu jest wymagana.");
        }

        var normalizedNazwa = nazwa.Trim();
        var normalizedPodtyp = string.IsNullOrWhiteSpace(podtyp) ? null : podtyp.Trim();
        var existing = await slownikRepository.GetTypyUprawnienAsync(cancellationToken);
        if (existing.Any(t =>
                t.Nazwa.Equals(normalizedNazwa, StringComparison.OrdinalIgnoreCase) &&
                (normalizedPodtyp is null
                    ? string.IsNullOrWhiteSpace(t.Podtyp)
                    : normalizedPodtyp.Equals(t.Podtyp, StringComparison.OrdinalIgnoreCase))))
        {
            throw new InvalidOperationException("Takie uprawnienie / kurs już istnieje w słowniku.");
        }

        return await slownikRepository.InsertTypUprawnieniaAsync(
            normalizedNazwa,
            normalizedPodtyp,
            wymagaDaty,
            cancellationToken);
    }

    public async Task UpdateAsync(
        SessionUser user,
        int id,
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken = default)
    {
        if (!user.CanManagePermissionTypes)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do zarządzania słownikiem uprawnień.");
        }

        if (string.IsNullOrWhiteSpace(nazwa))
        {
            throw new InvalidOperationException("Nazwa uprawnienia / kursu jest wymagana.");
        }

        var normalizedNazwa = nazwa.Trim();
        var normalizedPodtyp = string.IsNullOrWhiteSpace(podtyp) ? null : podtyp.Trim();
        var existing = await slownikRepository.GetTypyUprawnienAsync(cancellationToken);
        if (existing.Any(t =>
                t.Id != id
                && t.Nazwa.Equals(normalizedNazwa, StringComparison.OrdinalIgnoreCase)
                && (normalizedPodtyp is null
                    ? string.IsNullOrWhiteSpace(t.Podtyp)
                    : normalizedPodtyp.Equals(t.Podtyp, StringComparison.OrdinalIgnoreCase))))
        {
            throw new InvalidOperationException("Inne uprawnienie / kurs ma już taką nazwę i podtyp.");
        }

        await slownikRepository.UpdateTypUprawnieniaAsync(
            id,
            normalizedNazwa,
            normalizedPodtyp,
            wymagaDaty,
            cancellationToken);
    }
}
