using BOBER.Core.Models;

namespace BOBER.Services.Personnel;

public interface IFunkcjonariuszService
{
    Task<IReadOnlyList<Funkcjonariusz>> GetByZmianaAsync(int zmianaId, CancellationToken cancellationToken = default);
}
