namespace Chomik.Core.Models;

public sealed class OdznaczeniePrzypisanie
{
    public int Id { get; set; }
    public int FunkcjonariuszId { get; set; }
    public int TypOdznaczeniaId { get; set; }
    public string Nazwa { get; set; } = string.Empty;
    public DateTime DataNadania { get; set; }
}
