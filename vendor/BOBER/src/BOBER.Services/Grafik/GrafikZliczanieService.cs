using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Data.Repositories;

namespace BOBER.Services.Grafik;

public interface IGrafikZliczanieService
{
    Task<IReadOnlyList<GrafikZliczanieWiersz>> GetAllAsync(
        int zmianaId,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        int zmianaId,
        IReadOnlyList<GrafikZliczanieWiersz> items,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GrafikZliczanieSlownikPozycja>> GetTypyUprawnienAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GrafikZliczanieSlownikPozycja>> GetStanowiskaAsync(
        CancellationToken cancellationToken = default);
}

public sealed class GrafikZliczanieService(
    IGrafikZliczanieRepository repository,
    IChomikRepository chomik,
    IUstawieniaRepository ustawienia) : IGrafikZliczanieService
{
    private static string FlagaZasiania(int zmianaId) => $"GrafikZliczanieZasiane_{zmianaId}";

    public async Task<IReadOnlyList<GrafikZliczanieWiersz>> GetAllAsync(
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        var items = await repository.GetAllAsync(zmianaId, cancellationToken);
        if (items.Count > 0)
            return items;

        var zasiane = await ustawienia.GetAsync(FlagaZasiania(zmianaId), cancellationToken);
        if (string.Equals(zasiane, "1", StringComparison.Ordinal))
            return items;

        var seed = await BuildSeedAsync(zmianaId, cancellationToken);
        await repository.SaveAllAsync(zmianaId, seed, cancellationToken);
        await ustawienia.SetAsync(FlagaZasiania(zmianaId), "1", cancellationToken);
        return await repository.GetAllAsync(zmianaId, cancellationToken);
    }

    public async Task SaveAsync(
        int zmianaId,
        IReadOnlyList<GrafikZliczanieWiersz> items,
        CancellationToken cancellationToken = default)
    {
        short kolejnosc = 1;
        foreach (var item in items)
        {
            item.ZmianaId = zmianaId;
            item.Kolejnosc = kolejnosc++;
        }

        await repository.SaveAllAsync(zmianaId, items, cancellationToken);
        await ustawienia.SetAsync(FlagaZasiania(zmianaId), "1", cancellationToken);
    }

    public Task<IReadOnlyList<GrafikZliczanieSlownikPozycja>> GetTypyUprawnienAsync(
        CancellationToken cancellationToken = default) =>
        chomik.GetTypyUprawnienAsync(cancellationToken);

    public Task<IReadOnlyList<GrafikZliczanieSlownikPozycja>> GetStanowiskaAsync(
        CancellationToken cancellationToken = default) =>
        chomik.GetStanowiskaAsync(cancellationToken);

    private async Task<IReadOnlyList<GrafikZliczanieWiersz>> BuildSeedAsync(
        int zmianaId,
        CancellationToken cancellationToken)
    {
        var uprawnienia = await chomik.GetTypyUprawnienAsync(cancellationToken);
        var stanowiska = await chomik.GetStanowiskaAsync(cancellationToken);

        var uprawnieniaMap = uprawnienia.ToDictionary(
            u => (u.Nazwa, Podtyp: u.Podtyp ?? string.Empty),
            u => u.Id);
        var stanowiskaMap = stanowiska.ToDictionary(s => s.Nazwa, s => s.Id, StringComparer.OrdinalIgnoreCase);

        return GrafikZliczanieSeed.CreateDefaults(uprawnieniaMap, stanowiskaMap, zmianaId);
    }
}
