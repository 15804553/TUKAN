using BOBER.Core.Constants;
using BOBER.Core.Models;

namespace BOBER.Services.Tests.Kolory;

public sealed class KoloryLookupTests
{
    [Fact]
    public void IsAktywny_WhenMissing_ReturnsTrue()
    {
        var map = KoloryLookup.Index([]);

        Assert.True(KoloryLookup.IsAktywny(map, RoleKeys.DowodcaZmiany));
        Assert.True(KoloryLookup.IsAktywny(null, RoleKeys.WolnaSluzba));
    }

    [Fact]
    public void GetHexIfAktywny_WhenDisabled_ReturnsNull()
    {
        var map = KoloryLookup.Index(
        [
            new KolorStanowiska
            {
                KluczRoli = RoleKeys.WolnaSluzba,
                KolorHex = "#6A5C00",
                Aktywny = false
            },
            new KolorStanowiska
            {
                KluczRoli = RoleKeys.Kierowca,
                KolorHex = "#BFBFBF",
                Aktywny = true
            }
        ]);

        Assert.Null(KoloryLookup.GetHexIfAktywny(map, RoleKeys.WolnaSluzba));
        Assert.Equal("#BFBFBF", KoloryLookup.GetHexIfAktywny(map, RoleKeys.Kierowca));
        Assert.Null(KoloryLookup.GetHexIfAktywny(map, RoleKeys.DowodcaZmiany));
    }

    [Fact]
    public void NieaktywneKlucze_ReturnsOnlyDisabled()
    {
        var keys = KoloryLookup.NieaktywneKlucze(
        [
            new KolorStanowiska { KluczRoli = RoleKeys.Kierowca, KolorHex = "#BFBFBF", Aktywny = true },
            new KolorStanowiska { KluczRoli = RoleKeys.NurekCzcionka, KolorHex = "#F80808", Aktywny = false }
        ]);

        Assert.Single(keys);
        Assert.Contains(RoleKeys.NurekCzcionka, keys);
    }
}
