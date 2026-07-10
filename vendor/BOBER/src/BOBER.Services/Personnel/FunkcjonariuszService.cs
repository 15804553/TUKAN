using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Data.Repositories;

namespace BOBER.Services.Personnel;

public sealed class FunkcjonariuszService(
    IChomikRepository chomikRepository,
    IKolejnoscRepository kolejnoscRepository) : IFunkcjonariuszService
{
    public async Task<IReadOnlyList<Funkcjonariusz>> GetByZmianaAsync(
        int zmianaId,
        CancellationToken cancellationToken = default)
    {
        var wszyscy = await chomikRepository.GetByZmianaAsync(zmianaId, cancellationToken);
        var kolejnosc = await kolejnoscRepository.GetByZmianaAsync(zmianaId, cancellationToken);
        var positionMap = kolejnosc.ToDictionary(k => k.FunkcjonariuszId, k => k.Pozycja);

        return wszyscy
            .OrderBy(f => positionMap.TryGetValue(f.Id, out var pos) ? pos : int.MaxValue)
            .ThenBy(f => f.Id)
            .ToList();
    }

}

