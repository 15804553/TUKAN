using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IGrafikNurkowyRepository
{
    Task<GrafikNurkowyZatwierdzenie?> GetAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task SetZatwierdzenieAsync(
        int rok,
        int miesiac,
        bool zatwierdzony,
        string? zatwierdzonyPrzez,
        CancellationToken cancellationToken = default);
}
