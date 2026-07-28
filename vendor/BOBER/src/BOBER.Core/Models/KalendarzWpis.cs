namespace BOBER.Core.Models;

/// <summary>Notatka DCA w kalendarzu — jedna na dzień × zmianę.</summary>
public sealed class KalendarzWpis
{
    public int Id { get; set; }
    public DateOnly Data { get; set; }
    public int ZmianaId { get; set; }
    public KalendarzTypWpisu TypWpisu { get; set; } = KalendarzTypWpisu.Dca;
    public int? AutorZmianaId { get; set; }
    public string Tresc { get; set; } = string.Empty;
    public string AutorLogin { get; set; } = string.Empty;
    public DateTime DataUtworzenia { get; set; }
    public DateTime DataModyfikacji { get; set; }
    public KalendarzOdczyt? Odczyt { get; set; }
}
