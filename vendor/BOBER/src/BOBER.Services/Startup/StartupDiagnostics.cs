using System.IO;
using BOBER.Data;
using BOBER.Services.Logging;

namespace BOBER.Services.Startup;

/// <summary>
/// Sprawdzenia środowiska uruchomieniowego — brakujące pliki, ścieżki baz.
/// Wyniki trafiają do BOBER.log (nie blokują startu, poza krytyczną bazą BOBER).
/// </summary>
public static class StartupDiagnostics
{
    public static void Run(BoberDatabaseOptions boberOptions, ChomikDatabaseOptions chomikOptions)
    {
        BoberLog.Information("Start BOBER, katalog: {BaseDir}", AppContext.BaseDirectory);

        var boberPath = boberOptions.GetFullPath();
        if (File.Exists(boberPath))
            BoberLog.Information("Baza BOBER OK: {Path}", boberPath);
        else
            BoberLog.Error("Brak pliku bazy BOBER: {Path}", boberPath);

        var chomikPath = chomikOptions.FilePath;
        if (string.IsNullOrWhiteSpace(chomikPath))
        {
            BoberLog.Warning(null, "Ścieżka ChomikDB nie jest ustawiona");
            return;
        }

        if (File.Exists(chomikPath))
            BoberLog.Information("Baza Chomik OK: {Path}", chomikPath);
        else
            BoberLog.Warning(null, "Brak pliku bazy Chomik — logowanie i personel mogą nie działać: {Path}", chomikPath);

        if (!IsAceProviderLikelyInstalled())
            BoberLog.Warning(null,
                "Nie wykryto typowego wpisu rejestru ACE OLEDB — zainstaluj Microsoft Access Database Engine (64-bit zgodny z aplikacją)");
    }

    private static bool IsAceProviderLikelyInstalled()
    {
        try
        {
            var views = new[] { @"SOFTWARE\Microsoft\Office\16.0\Access Connectivity Engine\Engines\ACE",
                @"SOFTWARE\WOW6432Node\Microsoft\Office\16.0\Access Connectivity Engine\Engines\ACE" };
            foreach (var view in views)
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(view);
                if (key is not null)
                    return true;
            }
        }
        catch (Exception ex)
        {
            BoberLog.Warning(ex, "Nie udało się sprawdzić rejestru ACE");
        }

        return false;
    }
}
