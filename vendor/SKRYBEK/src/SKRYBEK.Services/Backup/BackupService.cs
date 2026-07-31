using System.Data.OleDb;
using System.Globalization;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Backup;

public sealed class BackupService
{
    private readonly SkrybekConnectionFactory _factory;
    private readonly UstawieniaRepository _ustawienia;

    public BackupService(SkrybekConnectionFactory factory, UstawieniaRepository ustawienia)
    {
        _factory    = factory;
        _ustawienia = ustawienia;
    }

    public string PobierzKatalogBackupu()
    {
        var srcPath = _factory.DatabasePath;
        return Path.Combine(
            Path.GetDirectoryName(srcPath) ?? AppContext.BaseDirectory,
            "BACKUP");
    }

    public async Task<bool> SprawdzIWykonajBackupAsync()
    {
        var czestotliwosc = await PobierzCzestotliwoscAsync();
        var ostatni = await _ustawienia.GetAsync(UstawieniaKlucze.OstatniBackup);
        var teraz = DateTime.Now;

        if (!string.IsNullOrEmpty(ostatni) &&
            DateTime.TryParse(ostatni, out var ostatniDt) &&
            CzyBackupAktualny(ostatniDt, teraz, czestotliwosc))
        {
            return false;
        }

        await WykonajBackupAsync(czestotliwosc);
        await _ustawienia.SetAsync(UstawieniaKlucze.OstatniBackup, teraz.ToString("yyyy-MM-dd HH:mm:ss"));
        return true;
    }

    public Task WykonajBackupAsync() => WykonajBackupAsync(null);

    public async Task WykonajBackupAsync(string? czestotliwosc)
    {
        czestotliwosc ??= await PobierzCzestotliwoscAsync();

        var srcPath = _factory.DatabasePath;
        if (!File.Exists(srcPath))
            throw new FileNotFoundException("Baza danych SKRYBEK nie znaleziona.", srcPath);

        var backupDir = PobierzKatalogBackupu();
        Directory.CreateDirectory(backupDir);

        var dstPath = Path.Combine(backupDir, ZbudujNazwePlikuBackupu(DateTime.Now, czestotliwosc));
        File.Copy(srcPath, dstPath, overwrite: true);
        SkrybekLog.Info($"Backup bazy danych: {dstPath}");
    }

    /// <summary>
    /// Przywraca bazę z pliku .bck. Przed nadpisaniem tworzy kopię bieżącej bazy.
    /// Wymaga zamknięcia połączeń OleDb — po odzyskaniu zalecany restart aplikacji.
    /// </summary>
    public async Task OdzyskajZBackupuAsync(string sciezkaBackupu)
    {
        if (string.IsNullOrWhiteSpace(sciezkaBackupu) || !File.Exists(sciezkaBackupu))
            throw new FileNotFoundException("Nie znaleziono pliku backupu.", sciezkaBackupu);

        if (!sciezkaBackupu.EndsWith(".bck", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Plik backupu musi mieć rozszerzenie .bck.");

        var dstPath = _factory.DatabasePath;
        if (!File.Exists(dstPath))
            throw new FileNotFoundException("Baza danych SKRYBEK nie znaleziona.", dstPath);

        var backupDir = PobierzKatalogBackupu();
        Directory.CreateDirectory(backupDir);

        var bazaNazwa = Path.GetFileNameWithoutExtension(dstPath);
        var kopiaBezpieczenstwa = Path.Combine(
            backupDir,
            $"{bazaNazwa}_przed_odzyskiem_{DateTime.Now:yyyy-MM-dd_HHmmss}.bck");

        ZwolnijPolaczeniaOleDb();

        File.Copy(dstPath, kopiaBezpieczenstwa, overwrite: true);
        File.Copy(sciezkaBackupu, dstPath, overwrite: true);

        SkrybekLog.Info($"Odzyskano bazę z backupu: {sciezkaBackupu} (kopia bezpieczeństwa: {kopiaBezpieczenstwa})");
        await Task.CompletedTask;
    }

    public async Task<string> PobierzCzestotliwoscAsync()
    {
        var wartosc = await _ustawienia.GetAsync(
            UstawieniaKlucze.CzestotliwoscBackupu,
            CzestotliwoscBackupu.Domyslna);

        return NormalizujCzestotliwosc(wartosc);
    }

    public static string NormalizujCzestotliwosc(string? wartosc) =>
        wartosc switch
        {
            CzestotliwoscBackupu.Codziennie => CzestotliwoscBackupu.Codziennie,
            CzestotliwoscBackupu.CoTydzien => CzestotliwoscBackupu.CoTydzien,
            CzestotliwoscBackupu.CoMiesiac => CzestotliwoscBackupu.CoMiesiac,
            _ => CzestotliwoscBackupu.Domyslna
        };

    public static string OpisCzestotliwosci(string czestotliwosc) =>
        NormalizujCzestotliwosc(czestotliwosc) switch
        {
            CzestotliwoscBackupu.Codziennie => "Program automatycznie tworzy backup codziennie przy starcie.",
            CzestotliwoscBackupu.CoTydzien => "Program automatycznie tworzy backup raz w tygodniu przy starcie.",
            _ => "Program automatycznie tworzy backup raz w miesiącu przy starcie."
        };

    internal static bool CzyBackupAktualny(DateTime ostatni, DateTime teraz, string czestotliwosc) =>
        NormalizujCzestotliwosc(czestotliwosc) switch
        {
            CzestotliwoscBackupu.Codziennie => ostatni.Date >= teraz.Date,
            CzestotliwoscBackupu.CoTydzien => PoczatekTygodnia(ostatni) >= PoczatekTygodnia(teraz),
            _ => new DateTime(ostatni.Year, ostatni.Month, 1) >= new DateTime(teraz.Year, teraz.Month, 1)
        };

    private static DateTime PoczatekTygodnia(DateTime data)
    {
        // Poniedziałek jako początek tygodnia (PL).
        var offset = ((int)data.DayOfWeek + 6) % 7;
        return data.Date.AddDays(-offset);
    }

    private string ZbudujNazwePlikuBackupu(DateTime kiedy, string czestotliwosc)
    {
        var bazaNazwa = Path.GetFileNameWithoutExtension(_factory.DatabasePath);
        if (string.IsNullOrWhiteSpace(bazaNazwa))
            bazaNazwa = "TukanDatabase";

        return NormalizujCzestotliwosc(czestotliwosc) switch
        {
            CzestotliwoscBackupu.Codziennie => $"{bazaNazwa}_{kiedy:yyyy-MM-dd}.bck",
            CzestotliwoscBackupu.CoTydzien => $"{bazaNazwa}_{kiedy:yyyy}-W{ISOWeek.GetWeekOfYear(kiedy):D2}.bck",
            _ => $"{bazaNazwa}_{kiedy:yyyy-MM}.bck"
        };
    }

    private static void ZwolnijPolaczeniaOleDb()
    {
        OleDbConnection.ReleaseObjectPool();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        OleDbConnection.ReleaseObjectPool();
    }
}
