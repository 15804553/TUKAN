namespace SKRYBEK.Core.Models;

public sealed class PozycjaSamochodu
{
    public int Id { get; set; }
    public int RozkazId { get; set; }
    public int SamochodId { get; set; }
    public int Pozycja { get; set; }
    public int? FunkcjonariuszId { get; set; }
    public string Nazwisko { get; set; } = string.Empty;

    /// <summary>
    /// Nazwa pojazdu zamrożona przy zapisie/zatwierdzeniu rozkazu.
    /// Chroni zablokowany meldunek przed zmianami w ustawieniach pojazdów.
    /// </summary>
    public string NazwaSamochodu { get; set; } = string.Empty;
}
