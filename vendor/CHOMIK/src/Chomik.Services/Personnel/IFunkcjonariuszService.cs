using Chomik.Core.Models;
using Chomik.Core.Security;

namespace Chomik.Services.Personnel;

public interface IFunkcjonariuszService
{
    Task<IReadOnlyList<FunkcjonariuszListItem>> GetFilteredAsync(
        SessionUser user,
        FunkcjonariuszFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Funkcjonariusz>> GetEditableAsync(
        SessionUser user,
        CancellationToken cancellationToken = default);

    Task<int> GetNextNumerPorzadkowyAsync(
        SessionUser user,
        CancellationToken cancellationToken = default);

    Task<Funkcjonariusz?> GetByIdForEditAsync(
        SessionUser user,
        int id,
        CancellationToken cancellationToken = default);

    Task<Funkcjonariusz?> GetForGeneralViewProfileAsync(
        SessionUser user,
        int id,
        CancellationToken cancellationToken = default);

    Task<int> SaveAsync(
        SessionUser user,
        Funkcjonariusz entity,
        IReadOnlyList<int> typyUprawnienIds,
        IReadOnlyDictionary<int, DateTime?> datyWaznosci,
        IReadOnlyDictionary<int, DateTime> datyNadaniaOdznaczen,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(SessionUser user, int id, CancellationToken cancellationToken = default);

    Task SaveGeneralViewNumerZmianyAsync(
        SessionUser user,
        int funkcjonariuszId,
        int numerZmiany,
        CancellationToken cancellationToken = default);

    Task SaveGeneralViewStopienAsync(
        SessionUser user,
        int funkcjonariuszId,
        int stopienId,
        CancellationToken cancellationToken = default);

    Task SaveGeneralViewTerminyMedyczneAsync(
        SessionUser user,
        int funkcjonariuszId,
        DateTime? badaniaOkresoweDo,
        DateTime? komoraDymowaDo,
        DateTime? kppDo,
        CancellationToken cancellationToken = default);

    Task SaveGeneralViewUprawnienieWazneDoAsync(
        SessionUser user,
        int uprawnieniePrzypisanieId,
        DateTime? wazneDo,
        CancellationToken cancellationToken = default);

    Task<PersonnelDictionaries> GetDictionariesAsync(CancellationToken cancellationToken = default);

    void InvalidateDictionariesCache();

    Task<IReadOnlyList<string>> GetPersonnelNamesForExportAsync(
        SessionUser user,
        int? numerZmiany,
        CancellationToken cancellationToken = default);
}

public sealed class FunkcjonariuszListItem
{
    public required Funkcjonariusz Entity { get; init; }

    public IReadOnlyList<UprawnieniePrzypisanie> Uprawnienia { get; init; } = [];

    public bool ShowSensitiveFields { get; init; }

    public string UprawnieniaSkrot { get; init; } = string.Empty;
    public string OdznaczeniaSkrot { get; init; } = string.Empty;
}

public sealed class PersonnelDictionaries
{
    public IReadOnlyList<SlownikItem> Stopnie { get; init; } = [];
    public IReadOnlyList<SlownikItem> Stanowiska { get; init; } = [];
    public IReadOnlyList<TypUprawnienia> TypyUprawnien { get; init; } = [];
    public IReadOnlyList<TypOdznaczenia> TypyOdznaczen { get; init; } = [];
}
