namespace Chomik.Core.Models;

public sealed class FunkcjonariuszFilter
{
    public int? NumerZmiany { get; set; }

    /// <summary>Pusta lista oznacza brak filtra.</summary>
    public IReadOnlyList<UprawnienieTypFiltr> Uprawnienia { get; set; } = [];

    public SposobLaczeniaUprawnien LaczenieUprawnien { get; set; } = SposobLaczeniaUprawnien.Lub;

    public string? Szukaj { get; set; }
}
