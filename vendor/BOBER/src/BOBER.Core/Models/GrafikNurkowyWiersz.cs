namespace BOBER.Core.Models;

/// <summary>Wiersz podglądu grafiku nurkowego (jedna osoba).</summary>
public sealed class GrafikNurkowyWiersz
{
    public string ImieNazwisko { get; set; } = string.Empty;
    public string Funkcja { get; set; } = string.Empty;
    public int? ZmianaId { get; set; }
    public Dictionary<int, string> Dni { get; set; } = new();
}
