using BOBER.Core.Models;

namespace BOBER.Core.Constants;

/// <summary>Stałe układu i nazewnictwa dokumentu Excel „Grafik nurkowy”.</summary>
public static class GrafikNurkowyConstants
{
    public const string JednostkaSgrwn = "SGRW-N \"KRAKÓW\"";
    public const string JednostkaPsp = "KM PSP KRAKÓW";
    public const string PodsumowanieEtykieta = "JRG 4 Kraków - A";
    public const string FileNamePrefix = "Grafiki nurków SGRW-N Kraków";

    public const string WartoscWPracy = "1";
    public const string WartoscUrlop = "U";

    public const string FunkcjaKpp = "KPP";
    public const string FunkcjaNurek = "nurek";
    public const string FunkcjaMlodszyNurek = "mł.nurek";

    public const int TitleRow = 1;
    public const int HeaderRow = 2;
    public const int FirstDataRow = 3;
    public const int ColJednostkaPsp = 1;
    public const int ColImieNazwisko = 2;
    public const int ColFunkcja = 3;
    public const int FirstDayCol = 4;

    public const string ColorJednostkaPsp = "#99CCFF";
    public const string ColorZmiana1 = "#FFFF00";
    public const string ColorZmiana2 = "#FF99CC";
    public const string ColorZmiana3 = "#99CCFF";
    public const string ColorPodsumowanie = "#BFBFBF";
    public const string ColorWartoscCzcionka = "#FF0000";
    public const string ColorBiale = "#FFFFFF";
    public const string ColorBrakGotowosci = "#FF0000";

    public static readonly string[] MonthNames =
    [
        "", "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
        "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"
    ];

    public static string BuildFileName(int miesiac, int rok) =>
        $"{FileNamePrefix} {MonthNames[miesiac]} {rok}.xlsx";

    public static string BuildSheetName(int miesiac, int rok) =>
        $"{MonthNames[miesiac]} {rok}";

    public static string BuildTitle(int miesiac, int rok) =>
        $"Grafik dyżurów nurków SGRWN \"Małopolska\" na miesiąc {MonthNames[miesiac]} {rok} r.";

    /// <summary>Kolor nagłówka dnia jak we wzorcu: 1=róż, 2=niebieski, 3=żółty, potem cykl.</summary>
    public static string ColorForDayHeader(int day) => ((day - 1) % 3) switch
    {
        0 => ColorZmiana2,
        1 => ColorZmiana3,
        _ => ColorZmiana1
    };

    /// <summary>Kolor tła wiersza osoby wg numeru zmiany (legenda zm. I/II/III).</summary>
    public static string ColorForZmiana(int zmianaId) => zmianaId switch
    {
        1 => ColorZmiana1,
        2 => ColorZmiana2,
        3 => ColorZmiana3,
        _ => ColorZmiana1
    };

    public static string ResolveFunkcja(Funkcjonariusz f)
    {
        if (f.MaUprawnieniaKPP)
            return FunkcjaKpp;

        if (f.NazwyUprawnien.Any(IsMlodszyNurekLabel))
            return FunkcjaMlodszyNurek;

        return FunkcjaNurek;
    }

    /// <summary>Etykieta uprawnienia typu mł.nurek / młodszy nurek (nie zwykły „Nurek”).</summary>
    public static bool IsMlodszyNurekLabel(string label) =>
        label.Contains("Nurek", StringComparison.OrdinalIgnoreCase)
        && (label.Contains("młodszy", StringComparison.OrdinalIgnoreCase)
            || label.Contains("mlodszy", StringComparison.OrdinalIgnoreCase)
            || label.Contains("mł.", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Mapuje wpis z grafiku służb na wartość w grafiku nurkowym.
    /// Pusta komórka = w pracy → „1”; urlop → „U”; pozostałe statusy → brak wartości.
    /// </summary>
    public static string? MapFromGrafikWpis(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return WartoscWPracy;

        if (typWpisu.Trim().Equals(GrafikWpisTypy.Urlop, StringComparison.OrdinalIgnoreCase))
            return WartoscUrlop;

        return null;
    }
}
