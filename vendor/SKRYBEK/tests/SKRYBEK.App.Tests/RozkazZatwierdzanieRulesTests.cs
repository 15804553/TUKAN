using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.App.Tests;

public sealed class RozkazZatwierdzanieRulesTests
{
    private static RozkazDzienny Rozkaz(int id, StatusRozkazu status) => new()
    {
        Id = id,
        Status = status,
        Rok = 2026,
        NumerRozkazu = id
    };

    [Fact]
    public void CzyZatwierdzicWszystkie_GdySaRobocze_ZwracaTrue()
    {
        var lista = new[]
        {
            Rozkaz(1, StatusRozkazu.Zatwierdzony),
            Rozkaz(2, StatusRozkazu.Roboczy)
        };

        Assert.True(RozkazZatwierdzanieRules.CzyZatwierdzicWszystkie(lista));
        Assert.Single(RozkazZatwierdzanieRules.FiltrujDoZatwierdzenia(lista));
    }

    [Fact]
    public void CzyZatwierdzicWszystkie_GdyWszystkieZatwierdzone_ZwracaFalse()
    {
        var lista = new[]
        {
            Rozkaz(1, StatusRozkazu.Zatwierdzony),
            Rozkaz(2, StatusRozkazu.Zatwierdzony)
        };

        Assert.False(RozkazZatwierdzanieRules.CzyZatwierdzicWszystkie(lista));
        Assert.Equal(2, RozkazZatwierdzanieRules.FiltrujDoOdblokowania(lista).Count);
    }

    [Fact]
    public void FiltrujDoOdblokowania_PustaLista_ZwracaPusta()
    {
        Assert.Empty(RozkazZatwierdzanieRules.FiltrujDoOdblokowania([]));
        // Brak roboczych → tryb odblokowania (nawet gdy lista pusta).
        Assert.False(RozkazZatwierdzanieRules.CzyZatwierdzicWszystkie([]));
    }
}
