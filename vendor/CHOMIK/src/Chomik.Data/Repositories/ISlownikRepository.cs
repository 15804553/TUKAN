using Chomik.Core.Models;

namespace Chomik.Data.Repositories;

public interface ISlownikRepository
{
    Task<IReadOnlyList<SlownikItem>> GetStopnieAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SlownikItem>> GetStanowiskaAsync(CancellationToken cancellationToken = default);
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
}
