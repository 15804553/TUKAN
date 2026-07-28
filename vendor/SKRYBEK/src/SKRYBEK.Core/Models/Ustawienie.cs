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
}

/// <summary>Wartości klucza <see cref="UstawieniaKlucze.CzestotliwoscBackupu"/>.</summary>
public static class CzestotliwoscBackupu
{
    public const string Codziennie = "Codziennie";
    public const string CoTydzien = "CoTydzien";
    public const string CoMiesiac = "CoMiesiac";

    public const string Domyslna = CoMiesiac;
}
