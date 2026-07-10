namespace Chomik.Core.Models;

public sealed class FunkcjonariuszListQuery
{
    public int? NumerZmiany { get; init; }

    public string? SearchTerm { get; init; }

    public string? UprawnienieNazwa { get; init; }

    public string? UprawnieniePodtyp { get; init; }
}
