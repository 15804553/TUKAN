namespace SKRYBEK.Core.Models;

public sealed class Ustawienie
{
    public string Klucz { get; set; } = string.Empty;
    public string Wartosc { get; set; } = string.Empty;
}

public static class UstawieniaKlucze
{
    public const string SciezkaBoberBazy = "SciezkaBoberBazy";
    public const string SciezkaChomikBazy = "SciezkaChomikBazy";
    public const string NrJRG = "NrJRG";
    public const string OstatniBackup = "OstatniBackup";
    public const string CzestotliwoscBackupu = "CzestotliwoscBackupu";
    public const string SciezkaBackupu = "SciezkaBackupu";
    public const string RetencjaBackupuMiesiace = "RetencjaBackupuMiesiace";
}

/// <summary>Wartości klucza <see cref="UstawieniaKlucze.RetencjaBackupuMiesiace"/>.</summary>
public static class RetencjaBackupu
{
    public const int DomyslnaMiesiecy = 6;

    public static readonly int[] DozwoloneMiesiace = [1, 3, 6, 9, 12];

    public static int Normalizuj(int? miesiace) =>
        miesiace is > 0 && DozwoloneMiesiace.Contains(miesiace.Value)
            ? miesiace.Value
            : DomyslnaMiesiecy;

    public static int Normalizuj(string? wartosc) =>
        int.TryParse(wartosc, out var parsed) ? Normalizuj(parsed) : DomyslnaMiesiecy;

    public static string Etykieta(int miesiace) =>
        Normalizuj(miesiace) switch
        {
            1 => "1 miesiąc",
            3 => "3 miesiące",
            6 => "6 miesięcy",
            9 => "9 miesięcy",
            12 => "12 miesięcy",
            var n => $"{n} miesięcy"
        };
}

/// <summary>Wartości klucza <see cref="UstawieniaKlucze.CzestotliwoscBackupu"/>.</summary>
public static class CzestotliwoscBackupu
{
    public const string Codziennie = "Codziennie";
    public const string CoTydzien = "CoTydzien";
    public const string CoMiesiac = "CoMiesiac";

    public const string Domyslna = CoMiesiac;
}
