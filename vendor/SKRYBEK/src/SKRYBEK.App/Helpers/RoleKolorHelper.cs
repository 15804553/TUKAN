using System.Data.OleDb;
using System.IO;
using System.Windows.Media;
using BOBER.Core.Constants;
using SKRYBEK.Core.Chomik;
using SKRYBEK.Core.Models;
using SKRYBEK.Data.Connections;

namespace SKRYBEK.App.Helpers;

/// <summary>
/// Kolory ról funkcjonariuszy w TUKAN — to samo źródło co grafik:
/// domyślne <see cref="RoleKeys"/> + nadpisania z tabeli KoloryStanowisk we wspólnej bazie.
/// </summary>
public static class RoleKolorHelper
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> KoloryRoli = new(WczytajKoloryRoli);

    public static Brush DomyslnyForeground =>
        new SolidColorBrush(KontrastowyTekst(Color.FromRgb(0x2D, 0x2D, 0x2D)));

    /// <summary>Stały czarny tekst na liście personelu — niezależnie od roli i uprawnień.</summary>
    public static Brush KolorCzcionkiListy => Brushes.Black;

    public static Brush WyznaczKolorTla(Funkcjonariusz osoba) =>
        new SolidColorBrush(PobierzKolorTlaRoli(WyznaczKluczRoliTla(osoba)));

    /// <summary>
    /// Kolor tekstu w comboboxach — kontrast do tła roli; wyróżnienie nurka jak w grafiku.
    /// </summary>
    public static Brush WyznaczKolorForeground(Funkcjonariusz osoba)
    {
        if (CzyNurek(osoba))
            return new SolidColorBrush(PobierzHexKlucza(RoleKeys.NurekCzcionka));

        var tlo = PobierzKolorTlaRoli(WyznaczKluczRoliTla(osoba));
        return new SolidColorBrush(KontrastowyTekst(tlo));
    }

    /// <summary>
    /// Obramowanie wyróżniające nurka na liście dostępnego personelu.
    /// Dla pozostałych — przezroczysta, przy stałej grubości 2 px bez przesuwania layoutu.
    /// </summary>
    public static Brush WyznaczKolorObramowaniaNurek(Funkcjonariusz osoba) =>
        CzyNurek(osoba)
            ? new SolidColorBrush(PobierzHexKlucza(RoleKeys.NurekCzcionka))
            : Brushes.Transparent;

    public static bool CzyNurek(Funkcjonariusz osoba) =>
        osoba.MaUprawnieniaNumek || osoba.MaUprawnieniaKPP;

    /// <summary>
    /// Rola tła kafelka — jak w grafiku: stanowisko (i uprawnienia kierowcy), nie „Dowodzenie przy akcji”.
    /// Nurek nie zmienia tła (tylko czcionka / obramowanie).
    /// </summary>
    private static string WyznaczKluczRoliTla(Funkcjonariusz osoba)
    {
        var sid = osoba.StanowiskoId;

        if (ChomikSlowniki.StanowiskaDowodcyZmiany.Contains(sid)
            || PasujeStanowisko(osoba, "dowódca zmiany", "dowodca zmiany"))
            return RoleKeys.DowodcaZmiany;

        if (ChomikSlowniki.StanowiskaDowodcySekcji.Contains(sid)
            || PasujeStanowisko(osoba, "dowódca sekcji", "dowodca sekcji",
                "zastępca dowódcy zmiany", "zastepca dowodcy zmiany"))
            return RoleKeys.DowodcaSekcji;

        if (sid == ChomikSlowniki.StanowiskoDowodcaZastepu
            || PasujeStanowisko(osoba, "dowódca zastępu", "dowodca zastepu"))
            return RoleKeys.DowodcaZastepu;

        if (osoba.MaUprawnieniaKierowca || PasujeStanowisko(osoba, "kierowca"))
            return RoleKeys.Kierowca;

        return RoleKeys.Zwykly;
    }

    private static bool PasujeStanowisko(Funkcjonariusz osoba, params string[] fragmenty)
    {
        if (string.IsNullOrWhiteSpace(osoba.Stanowisko))
            return false;

        var nazwa = osoba.Stanowisko.ToLowerInvariant();
        foreach (var fragment in fragmenty)
        {
            if (nazwa.Contains(fragment))
                return true;
        }

        return false;
    }

    private static Color PobierzKolorTlaRoli(string klucz) =>
        PobierzHexKlucza(klucz);

    private static Color PobierzHexKlucza(string klucz)
    {
        if (KoloryRoli.Value.TryGetValue(klucz, out var hex))
            return ParsujKolor(hex);

        return ParsujKolor(RoleKeys.GetDefaultKolorHex(klucz));
    }

    private static Color KontrastowyTekst(Color tlo)
    {
        var luminance = (0.299 * tlo.R + 0.587 * tlo.G + 0.114 * tlo.B) / 255;
        return luminance > 0.55
            ? Color.FromRgb(0x1E, 0x1E, 0x1E)
            : Color.FromRgb(0xE0, 0xE0, 0xE0);
    }

    private static IReadOnlyDictionary<string, string> WczytajKoloryRoli()
    {
        var kolory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var klucz in RoleKeys.WszystkieKolory)
            kolory[klucz] = RoleKeys.GetDefaultKolorHex(klucz);

        foreach (var (klucz, hex) in WczytajKoloryZBazy())
            kolory[klucz] = hex;

        return kolory;
    }

    private static IEnumerable<KeyValuePair<string, string>> WczytajKoloryZBazy()
    {
        var wynik = new List<KeyValuePair<string, string>>();
        try
        {
            if (ServiceProvider.Services is null)
                return wynik;

            var path = ServiceProvider.Services.BoberDb.DatabasePath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return wynik;

            var factory = new BoberConnectionFactory(path);
            using var connection = factory.Create();
            connection.Open();

            using var command = new OleDbCommand(
                "SELECT KluczRoli, KolorHex FROM KoloryStanowisk", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var klucz = reader.GetString(0);
                var hex = reader.GetString(1);
                if (!string.IsNullOrWhiteSpace(klucz) && !string.IsNullOrWhiteSpace(hex))
                    wynik.Add(new KeyValuePair<string, string>(klucz, hex));
            }
        }
        catch
        {
            // fallback do RoleKeys
        }

        return wynik;
    }

    private static Color ParsujKolor(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex)!;
        }
        catch
        {
            return Colors.White;
        }
    }
}
