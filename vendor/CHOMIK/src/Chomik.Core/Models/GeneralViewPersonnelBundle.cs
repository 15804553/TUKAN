namespace Chomik.Core.Models;

public sealed class GeneralViewPersonnelBundle
{
    public required IReadOnlyList<Funkcjonariusz> Personnel { get; init; }

    public required IReadOnlyDictionary<int, IReadOnlyList<UprawnieniePrzypisanie>> UprawnieniaByPersonId { get; init; }
}
