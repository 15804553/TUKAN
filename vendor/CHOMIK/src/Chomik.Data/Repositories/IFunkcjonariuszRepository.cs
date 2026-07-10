using Chomik.Core.Models;

namespace Chomik.Data.Repositories;

public interface IFunkcjonariuszRepository
{
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Funkcjonariusz>> GetAllAsync(
        FunkcjonariuszLoadOptions? loadOptions = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Funkcjonariusz>> GetListAsync(
        FunkcjonariuszListQuery query,
        FunkcjonariuszLoadOptions? loadOptions = null,
        CancellationToken cancellationToken = default);

    Task<GeneralViewPersonnelBundle> LoadGeneralViewBundleAsync(
        FunkcjonariuszListQuery query,
        bool includeSensitiveRelations,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPersonnelFullNamesAsync(
        FunkcjonariuszListQuery query,
        CancellationToken cancellationToken = default);
    Task<Funkcjonariusz?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> GetNextNumerPorzadkowyAsync(int numerZmiany, CancellationToken cancellationToken = default);

    Task<bool> IsNumerPorzadkowyTakenAsync(
        int numerZmiany,
        int numerPorzadkowy,
        int excludeFunkcjonariuszId = 0,
        CancellationToken cancellationToken = default);

    Task<int> InsertAsync(Funkcjonariusz entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Funkcjonariusz entity, CancellationToken cancellationToken = default);
    Task UpdateNumerZmianyAsync(int funkcjonariuszId, int numerZmiany, CancellationToken cancellationToken = default);

    Task UpdateStopienIdAsync(int funkcjonariuszId, int stopienId, CancellationToken cancellationToken = default);

    Task UpdateTerminyMedyczneAsync(
        int funkcjonariuszId,
        DateTime? badaniaOkresoweDo,
        DateTime? komoraDymowaDo,
        DateTime? kppDo,
        CancellationToken cancellationToken = default);

    Task UpdateUprawnienieWazneDoAsync(
        int uprawnieniePrzypisanieId,
        DateTime? wazneDo,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task ReplaceUprawnieniaAsync(
        int funkcjonariuszId,
        IReadOnlyList<int> typUprawnieniaIds,
        IReadOnlyDictionary<int, DateTime?> datyWaznosci,
        CancellationToken cancellationToken = default);

    Task ReplaceOdznaczeniaAsync(
        int funkcjonariuszId,
        IReadOnlyDictionary<int, DateTime> datyNadania,
        CancellationToken cancellationToken = default);
}
