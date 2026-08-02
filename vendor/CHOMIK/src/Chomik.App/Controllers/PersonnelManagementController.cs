using Chomik.Core.Models;
using Chomik.Services;
using Chomik.Services.Personnel;

namespace Chomik.App.Controllers;

public sealed class PersonnelManagementController(AppServices services)
{
    public int ShiftNumber =>
        services.Auth.CurrentUser?.ShiftNumber
        ?? throw new InvalidOperationException("Brak zalogowanego użytkownika zmiany.");

    public bool CanViewSensitiveData =>
        services.Auth.CurrentUser?.CanViewSensitiveData ?? false;

    public Task<IReadOnlyList<Funkcjonariusz>> LoadPersonnelAsync(CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser!;
        return services.Funkcjonariusze.GetEditableAsync(user, cancellationToken);
    }

    public Task<PersonnelDictionaries> GetDictionariesAsync(CancellationToken cancellationToken = default) =>
        services.Funkcjonariusze.GetDictionariesAsync(cancellationToken);

    public Task<int> GetNextNumerPorzadkowyAsync(CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser!;
        return services.Funkcjonariusze.GetNextNumerPorzadkowyAsync(user, cancellationToken);
    }

    public Task<Funkcjonariusz?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser!;
        return services.Funkcjonariusze.GetByIdForEditAsync(user, id, cancellationToken);
    }

    public Task<int> SaveAsync(
        Funkcjonariusz entity,
        IReadOnlyList<int> typyUprawnienIds,
        IReadOnlyDictionary<int, DateTime?> datyWaznosci,
        IReadOnlyDictionary<int, DateTime> datyNadaniaOdznaczen,
        CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser!;
        return services.Funkcjonariusze.SaveAsync(
            user, entity, typyUprawnienIds, datyWaznosci, datyNadaniaOdznaczen, cancellationToken);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = services.Auth.CurrentUser!;
        return services.Funkcjonariusze.DeleteAsync(user, id, cancellationToken);
    }
}
