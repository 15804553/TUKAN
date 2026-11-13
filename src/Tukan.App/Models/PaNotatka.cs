namespace Tukan.App.Models;

/// <summary>Notatka pod ręką dla konta PA: telefon, numer albo zapisek.</summary>
public sealed class PaNotatka
{
    public const int MaksDlugoscNaglowka = 200;
    public const int MaksDlugoscTresci = 8000;

    public int Id { get; init; }

    /// <summary>Data utworzenia, a po edycji — data ostatniego zapisu.</summary>
    public DateTime DataModyfikacji { get; init; }

    public string Naglowek { get; init; } = string.Empty;

    public string Tresc { get; init; } = string.Empty;

    public string DataWyswietlana => DataModyfikacji.ToString("dd.MM.yyyy HH:mm");
}
