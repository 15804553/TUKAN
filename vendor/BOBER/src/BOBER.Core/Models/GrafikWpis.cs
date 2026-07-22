namespace BOBER.Core.Models;

public sealed class GrafikWpis
{
    public int Id { get; set; }
    public int FunkcjonariuszId { get; set; }
    public int ZmianaId { get; set; }
    public int Rok { get; set; }
    public int Miesiac { get; set; }
    public int Dzien { get; set; }

    /// <summary>Kody nieobecności w pracy zmiany: D, WS, U, Del, S, C (+ opcjonalnie Oddał „/”). Brak wpisu = w pracy.</summary>
    public string TypWpisu { get; set; } = string.Empty;

    /// <summary>Czy wpis pochodzi z automatycznego generowania (nie jest ręczny).</summary>
    public bool IsAuto { get; set; }

    public DateOnly Data => new(Rok, Miesiac, Dzien);
}
