using BOBER.Core.Models;
using BOBER.Services;

namespace BOBER.App.Controllers;

public sealed class GrafikNurkowyController(AppServices services)
{
    public int DefaultYear => DateTime.Today.Year;

    public Task<IReadOnlyList<GrafikNurkowyWiersz>> LoadPreviewAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.GrafikNurkowy.LoadPreviewAsync(rok, miesiac, cancellationToken);

    public Task<GrafikNurkowyZatwierdzenie?> GetZatwierdzenieAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.GrafikNurkowy.GetZatwierdzenieAsync(rok, miesiac, cancellationToken);

    public Task<GrafikNurkowySyncResult> GenerateOrUpdateAsync(
        int zmianaId,
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.GrafikNurkowy.GenerateOrUpdateAsync(zmianaId, rok, miesiac, cancellationToken);

    public Task ZatwierdzAsync(
        int rok,
        int miesiac,
        string zatwierdzonyPrzez,
        CancellationToken cancellationToken = default) =>
        services.GrafikNurkowy.ZatwierdzAsync(rok, miesiac, zatwierdzonyPrzez, cancellationToken);

    public Task CofnijZatwierdzenieAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.GrafikNurkowy.CofnijZatwierdzenieAsync(rok, miesiac, cancellationToken);

    public Task<string> GetExportPathAsync(CancellationToken cancellationToken = default) =>
        services.Settings.GetExportPathGrafikNurkowyAsync(cancellationToken);

    public Task<string> ResolveFilePathAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default) =>
        services.GrafikNurkowy.ResolveFilePathAsync(rok, miesiac, cancellationToken);
}
