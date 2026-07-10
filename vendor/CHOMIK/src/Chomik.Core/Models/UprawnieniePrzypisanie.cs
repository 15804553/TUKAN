namespace Chomik.Core.Models;

public sealed class UprawnieniePrzypisanie
{
    public int Id { get; set; }
    public int FunkcjonariuszId { get; set; }
    public string Nazwa { get; set; } = string.Empty;
    public string? Podtyp { get; set; }
    public DateTime? WazneDo { get; set; }
    public string? Uwagi { get; set; }
}
