using Chomik.Core.Models;
using Chomik.Core.Security;
using Chomik.Data.Repositories;

namespace Chomik.Services.Settings;

/// <summary>CRUD słowników stopni, stanowisk i odznaczeń (DCA).</summary>
public sealed class PersonelSlownikiService(ISlownikRepository slownikRepository)
{
    public Task<IReadOnlyList<SlownikItem>> GetStopnieAsync(CancellationToken cancellationToken = default) =>
        slownikRepository.GetStopnieAsync(cancellationToken);

    public Task<IReadOnlyList<SlownikItem>> GetStanowiskaAsync(CancellationToken cancellationToken = default) =>
        slownikRepository.GetStanowiskaAsync(cancellationToken);

    public Task<IReadOnlyList<TypOdznaczenia>> GetTypyOdznaczenAsync(CancellationToken cancellationToken = default) =>
        slownikRepository.GetTypyOdznaczenAsync(cancellationToken);

    public Task<int> AddStopienAsync(SessionUser user, string nazwa, CancellationToken cancellationToken = default) =>
        AddNazwaAsync(
            user,
            nazwa,
            "stopnia",
            () => slownikRepository.GetStopnieAsync(cancellationToken),
            n => slownikRepository.InsertStopienAsync(n, cancellationToken));

    public Task UpdateStopienAsync(
        SessionUser user,
        int id,
        string nazwa,
        CancellationToken cancellationToken = default) =>
        UpdateNazwaAsync(
            user,
            id,
            nazwa,
            "stopnia",
            () => slownikRepository.GetStopnieAsync(cancellationToken),
            (itemId, n) => slownikRepository.UpdateStopienAsync(itemId, n, cancellationToken));

    public async Task DeleteStopienAsync(SessionUser user, int id, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(user);
        var usage = await slownikRepository.CountFunkcjonariuszeByStopienAsync(id, cancellationToken);
        if (usage > 0)
        {
            throw new InvalidOperationException(
                $"Nie można usunąć stopnia — jest przypisany do {usage} funkcjonariuszy. Najpierw zmień stopień w kartotekach.");
        }

        await slownikRepository.DeleteStopienAsync(id, cancellationToken);
    }

    public Task<int> AddStanowiskoAsync(SessionUser user, string nazwa, CancellationToken cancellationToken = default) =>
        AddNazwaAsync(
            user,
            nazwa,
            "stanowiska",
            () => slownikRepository.GetStanowiskaAsync(cancellationToken),
            n => slownikRepository.InsertStanowiskoAsync(n, cancellationToken));

    public Task UpdateStanowiskoAsync(
        SessionUser user,
        int id,
        string nazwa,
        CancellationToken cancellationToken = default) =>
        UpdateNazwaAsync(
            user,
            id,
            nazwa,
            "stanowiska",
            () => slownikRepository.GetStanowiskaAsync(cancellationToken),
            (itemId, n) => slownikRepository.UpdateStanowiskoAsync(itemId, n, cancellationToken));

    public async Task DeleteStanowiskoAsync(SessionUser user, int id, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(user);
        var usage = await slownikRepository.CountFunkcjonariuszeByStanowiskoAsync(id, cancellationToken);
        if (usage > 0)
        {
            throw new InvalidOperationException(
                $"Nie można usunąć stanowiska — jest przypisane do {usage} funkcjonariuszy. Najpierw zmień stanowisko w kartotekach.");
        }

        await slownikRepository.DeleteStanowiskoAsync(id, cancellationToken);
    }

    public Task<int> AddTypOdznaczeniaAsync(
        SessionUser user,
        string nazwa,
        CancellationToken cancellationToken = default) =>
        AddOdznaczenieAsync(user, nazwa, cancellationToken);

    public Task UpdateTypOdznaczeniaAsync(
        SessionUser user,
        int id,
        string nazwa,
        CancellationToken cancellationToken = default) =>
        UpdateOdznaczenieAsync(user, id, nazwa, cancellationToken);

    public async Task DeleteTypOdznaczeniaAsync(
        SessionUser user,
        int id,
        CancellationToken cancellationToken = default)
    {
        EnsureCanManage(user);
        var usage = await slownikRepository.CountOdznaczeniaAssignmentsAsync(id, cancellationToken);
        if (usage > 0)
        {
            throw new InvalidOperationException(
                $"Nie można usunąć odznaczenia — jest przypisane u {usage} funkcjonariuszy. Najpierw odznacz w kartotekach.");
        }

        await slownikRepository.DeleteTypOdznaczeniaAsync(id, cancellationToken);
    }

    private async Task<int> AddNazwaAsync(
        SessionUser user,
        string nazwa,
        string entityGenitive,
        Func<Task<IReadOnlyList<SlownikItem>>> loadExisting,
        Func<string, Task<int>> insert)
    {
        EnsureCanManage(user);
        var normalized = NormalizeNazwa(nazwa, entityGenitive);
        var existing = await loadExisting();
        if (existing.Any(i => i.Nazwa.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Taka nazwa {entityGenitive} już istnieje w słowniku.");
        }

        return await insert(normalized);
    }

    private async Task UpdateNazwaAsync(
        SessionUser user,
        int id,
        string nazwa,
        string entityGenitive,
        Func<Task<IReadOnlyList<SlownikItem>>> loadExisting,
        Func<int, string, Task> update)
    {
        EnsureCanManage(user);
        var normalized = NormalizeNazwa(nazwa, entityGenitive);
        var existing = await loadExisting();
        if (existing.Any(i =>
                i.Id != id && i.Nazwa.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Inna pozycja ma już taką nazwę {entityGenitive}.");
        }

        await update(id, normalized);
    }

    private async Task<int> AddOdznaczenieAsync(
        SessionUser user,
        string nazwa,
        CancellationToken cancellationToken)
    {
        EnsureCanManage(user);
        var normalized = NormalizeNazwa(nazwa, "odznaczenia");
        var existing = await slownikRepository.GetTypyOdznaczenAsync(cancellationToken);
        if (existing.Any(i => i.Nazwa.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Takie odznaczenie / medal już istnieje w słowniku.");
        }

        return await slownikRepository.InsertTypOdznaczeniaAsync(normalized, cancellationToken);
    }

    private async Task UpdateOdznaczenieAsync(
        SessionUser user,
        int id,
        string nazwa,
        CancellationToken cancellationToken)
    {
        EnsureCanManage(user);
        var normalized = NormalizeNazwa(nazwa, "odznaczenia");
        var existing = await slownikRepository.GetTypyOdznaczenAsync(cancellationToken);
        if (existing.Any(i =>
                i.Id != id && i.Nazwa.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Inne odznaczenie / medal ma już taką nazwę.");
        }

        await slownikRepository.UpdateTypOdznaczeniaAsync(id, normalized, cancellationToken);
    }

    private static void EnsureCanManage(SessionUser user)
    {
        if (!user.CanManageSettings)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do zarządzania słownikami personelu.");
        }
    }

    private static string NormalizeNazwa(string nazwa, string entityGenitive)
    {
        if (string.IsNullOrWhiteSpace(nazwa))
        {
            throw new InvalidOperationException($"Nazwa {entityGenitive} jest wymagana.");
        }

        return nazwa.Trim();
    }
}
