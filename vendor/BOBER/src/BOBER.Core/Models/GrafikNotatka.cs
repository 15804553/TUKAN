namespace BOBER.Core.Models;

/// <summary>Notatka przypisana do kolumny dnia w grafiku służb (per zmiana / miesiąc).</summary>
public sealed class GrafikNotatka
{
    public int Id { get; set; }
    public int ZmianaId { get; set; }
    public int Rok { get; set; }
    public int Miesiac { get; set; }
    public int Dzien { get; set; }
    public string Tresc { get; set; } = string.Empty;
}
