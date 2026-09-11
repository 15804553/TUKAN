using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Core.Oznaczenia;
using BOBER.Data.Repositories;

namespace BOBER.Services.Oznaczenia;

public interface IOznaczeniaService
{
    Task<IReadOnlyList<OznaczenieGrafiku>> GetAllAsync(
        int zmianaId,
        CancellationToken cancellationToken = default);

    Task ReloadAsync(int zmianaId, CancellationToken cancellationToken = default);

    Task SaveAsync(
        int zmianaId,
        IReadOnlyList<OznaczenieGrafiku> items,
        CancellationToken cancellationToken = default);

    Task<int> CountWpisowZKodemAsync(
        string kod,
        int zmianaId,
        CancellationToken cancellationToken = default);
}

public sealed class OznaczeniaService(IOznaczeniaGrafikuRepository repository) : IOznaczeniaService
{
    public async Task<IReadOnlyList<OznaczenieGrafiku>> GetAllAsync(
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        var items = await repository.GetAllAsync(zmianaId, cancellationToken);
        if (items.Count == 0)
        {
            return OznaczeniaGrafikuSeed.CreateDefaults()
                .Select(o =>
                {
                    o.ZmianaId = zmianaId;
                    return o;
                })
                .ToList();
        }

        return items;
    }

    public async Task ReloadAsync(int zmianaId, CancellationToken cancellationToken = default)
    {
        var items = await GetAllAsync(zmianaId, cancellationToken);
        OznaczeniaLookup.Set(items);
    }

    public async Task SaveAsync(
        int zmianaId,
        IReadOnlyList<OznaczenieGrafiku> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
            item.ZmianaId = zmianaId;

        await repository.SaveAllAsync(zmianaId, items, cancellationToken);
        OznaczeniaLookup.Set(items);
    }

    public Task<int> CountWpisowZKodemAsync(
        string kod,
        int zmianaId,
        CancellationToken cancellationToken = default) =>
        repository.CountWpisowZKodemAsync(kod, zmianaId, cancellationToken);
}
