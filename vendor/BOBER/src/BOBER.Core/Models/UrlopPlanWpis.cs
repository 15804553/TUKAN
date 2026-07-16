namespace BOBER.Core.Models;

public sealed class UrlopPlanWpis
{
    public int Id { get; set; }
    public int FunkcjonariuszId { get; set; }
    public int ZmianaId { get; set; }
    public int Rok { get; set; }
    public int Miesiac { get; set; }
    public int Dzien { get; set; }
    public string TypUrlopu { get; set; } = string.Empty;

    public DateOnly Data => new(Rok, Miesiac, Dzien);
}
