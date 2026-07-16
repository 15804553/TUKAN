using BOBER.Core.Models;

namespace BOBER.Services.GrafikNurkowy;

public interface IGrafikNurkowyService
{
    Task<string> ResolveFilePathAsync(int rok, int miesiac, CancellationToken cancellationToken = default);

    Task<GrafikNurkowySyncResult> GenerateOrUpdateAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GrafikNurkowyWiersz>> LoadPreviewAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task<GrafikNurkowyZatwierdzenie?> GetZatwierdzenieAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task ZatwierdzAsync(
        int rok,
        int miesiac,
        string zatwierdzonyPrzez,
        CancellationToken cancellationToken = default);

    Task CofnijZatwierdzenieAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);

    Task<bool> IsZatwierdzonyAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default);
}
