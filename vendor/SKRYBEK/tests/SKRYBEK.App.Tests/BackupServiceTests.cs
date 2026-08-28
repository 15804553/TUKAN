using SKRYBEK.Core.Models;
using SKRYBEK.Services.Backup;

namespace SKRYBEK.App.Tests;

public class RetencjaBackupuTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(6, 6)]
    [InlineData(9, 9)]
    [InlineData(12, 12)]
    [InlineData(2, 6)]
    [InlineData(0, 6)]
    [InlineData(null, 6)]
    public void Normalizuj_int_zwraca_dozwolona_wartosc_lub_domyslna(int? input, int expected) =>
        Assert.Equal(expected, RetencjaBackupu.Normalizuj(input));

    [Theory]
    [InlineData("6", 6)]
    [InlineData("12", 12)]
    [InlineData("nie", 6)]
    [InlineData(null, 6)]
    public void Normalizuj_string_zwraca_dozwolona_wartosc_lub_domyslna(string? input, int expected) =>
        Assert.Equal(expected, RetencjaBackupu.Normalizuj(input));
}

public class BackupHarmonogramTests
{
    [Fact]
    public void Codziennie_backup_z_dzisiaj_jest_aktualny()
    {
        var dzis = new DateTime(2026, 8, 28, 15, 0, 0);
        Assert.True(BackupService.CzyBackupAktualny(dzis, dzis, CzestotliwoscBackupu.Codziennie));
    }

    [Fact]
    public void Codziennie_backup_z_wczoraj_wymaga_nowego()
    {
        var wczoraj = new DateTime(2026, 8, 27, 23, 59, 0);
        var dzis = new DateTime(2026, 8, 28, 8, 0, 0);
        Assert.False(BackupService.CzyBackupAktualny(wczoraj, dzis, CzestotliwoscBackupu.Codziennie));
    }

    [Fact]
    public void CoMiesiac_backup_z_biezacego_miesiaca_jest_aktualny()
    {
        var start = new DateTime(2026, 8, 5, 10, 0, 0);
        var pozniej = new DateTime(2026, 8, 28, 10, 0, 0);
        Assert.True(BackupService.CzyBackupAktualny(start, pozniej, CzestotliwoscBackupu.CoMiesiac));
    }
}
