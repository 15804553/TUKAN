using System.IO;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;
using SKRYBEK.Data.Database;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Backup;

namespace SKRYBEK.App.Tests;

public class BackupIntegrationTests : IAsyncLifetime
{
    private const string TestPassword = "5359";

    private string _root = null!;
    private string _dbPath = null!;
    private string _backupDir = null!;
    private BackupService _backup = null!;
    private UstawieniaRepository _ustawienia = null!;

    public async Task InitializeAsync()
    {
        _root = Path.Combine(Path.GetTempPath(), "TukanBackupTest_" + Guid.NewGuid().ToString("N"));
        _backupDir = Path.Combine(_root, "Backup");
        _dbPath = Path.Combine(_root, "TukanDatabase.accdb");
        Directory.CreateDirectory(_backupDir);

        var factory = new SkrybekConnectionFactory(_dbPath, TestPassword);
        var bootstrapper = new DatabaseBootstrapper(factory);
        await bootstrapper.EnsureCreatedAsync();

        _ustawienia = new UstawieniaRepository(factory);
        _backup = new BackupService(factory, _ustawienia);

        await _ustawienia.SetAsync(UstawieniaKlucze.SciezkaBackupu, _backupDir);
        await _ustawienia.SetAsync(UstawieniaKlucze.CzestotliwoscBackupu, CzestotliwoscBackupu.Codziennie);
        await _ustawienia.SetAsync(UstawieniaKlucze.RetencjaBackupuMiesiace, "6");
        await _ustawienia.SetAsync(UstawieniaKlucze.OstatniBackup, string.Empty);
    }

    public Task DisposeAsync()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Pliki mogą być chwilowo zablokowane przez ACE.
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task SprawdzIWykonajBackupAsync_tworzy_plik_bck_i_aktualizuje_ostatni_backup()
    {
        var wykonano = await _backup.SprawdzIWykonajBackupAsync();
        Assert.True(wykonano);

        var pliki = Directory.GetFiles(_backupDir, "*.bck");
        Assert.Single(pliki);

        var ostatni = await _ustawienia.GetAsync(UstawieniaKlucze.OstatniBackup);
        Assert.False(string.IsNullOrWhiteSpace(ostatni));
        Assert.True(DateTime.TryParse(ostatni, out _));
    }

    [Fact]
    public async Task SprawdzIWykonajBackupAsync_pomija_gdy_backup_aktualny()
    {
        await _backup.SprawdzIWykonajBackupAsync();

        var wykonanoPonownie = await _backup.SprawdzIWykonajBackupAsync();
        Assert.False(wykonanoPonownie);
    }

    [Fact]
    public async Task UsunPrzeterminowaneKopieAsync_usuwa_stare_pliki_bck()
    {
        var stary = Path.Combine(_backupDir, "TukanDatabase_2025-01-01.bck");
        await File.WriteAllTextAsync(stary, "stara kopia");
        File.SetLastWriteTime(stary, DateTime.Now.AddMonths(-7));

        var swiezy = Path.Combine(_backupDir, "TukanDatabase_swiezy.bck");
        await File.WriteAllTextAsync(swiezy, "swieza kopia");

        await _backup.UsunPrzeterminowaneKopieAsync(6);

        Assert.False(File.Exists(stary));
        Assert.True(File.Exists(swiezy));
    }

    [Fact]
    public async Task SprawdzIWykonajBackupAsync_blokada_zapobiega_rownoleglemu_backupowi()
    {
        await _ustawienia.SetAsync(UstawieniaKlucze.OstatniBackup, string.Empty);

        var task1 = Task.Run(() => _backup.SprawdzIWykonajBackupAsync());
        var task2 = Task.Run(() => _backup.SprawdzIWykonajBackupAsync());

        var wyniki = await Task.WhenAll(task1, task2);
        Assert.Equal(1, wyniki.Count(r => r));

        var ostatni = await _ustawienia.GetAsync(UstawieniaKlucze.OstatniBackup);
        Assert.False(string.IsNullOrWhiteSpace(ostatni));
    }
}
