namespace Chomik.Core.Models;

public sealed class FunkcjonariuszListQuery
{
    public int? NumerZmiany { get; init; }

    public string? SearchTerm { get; init; }

    /// <summary>Pusta lista oznacza brak filtra.</summary>
    public IReadOnlyList<UprawnienieTypFiltr> Uprawnienia { get; init; } = [];

    public SposobLaczeniaUprawnien LaczenieUprawnien { get; init; } = SposobLaczeniaUprawnien.Lub;
}
