using Chomik.Core.Models;

namespace Chomik.Data.Repositories;

public interface ISlownikRepository
{
    Task<IReadOnlyList<SlownikItem>> GetStopnieAsync(CancellationToken cancellationToken = default);
    Task<int> InsertStopienAsync(string nazwa, CancellationToken cancellationToken = default);
    Task UpdateStopienAsync(int id, string nazwa, CancellationToken cancellationToken = default);
    Task DeleteStopienAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountFunkcjonariuszeByStopienAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlownikItem>> GetStanowiskaAsync(CancellationToken cancellationToken = default);
    Task<int> InsertStanowiskoAsync(string nazwa, CancellationToken cancellationToken = default);
    Task UpdateStanowiskoAsync(int id, string nazwa, CancellationToken cancellationToken = default);
    Task DeleteStanowiskoAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountFunkcjonariuszeByStanowiskoAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TypUprawnienia>> GetTypyUprawnienAsync(CancellationToken cancellationToken = default);
    Task<int> InsertTypUprawnieniaAsync(
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken = default);

    Task UpdateTypUprawnieniaAsync(
        int id,
        string nazwa,
        string? podtyp,
        bool wymagaDaty,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TypOdznaczenia>> GetTypyOdznaczenAsync(CancellationToken cancellationToken = default);
    Task<int> InsertTypOdznaczeniaAsync(string nazwa, CancellationToken cancellationToken = default);
    Task UpdateTypOdznaczeniaAsync(int id, string nazwa, CancellationToken cancellationToken = default);
    Task DeleteTypOdznaczeniaAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountOdznaczeniaAssignmentsAsync(int id, CancellationToken cancellationToken = default);
}
