namespace Chomik.Core.Models;

public sealed class FunkcjonariuszLoadOptions
{
    public static FunkcjonariuszLoadOptions Full { get; } = new();

    public static FunkcjonariuszLoadOptions ForGeneralView(bool includeSensitiveRelations) => new()
    {
        IncludeUprawnienia = true,
        IncludeOdznaczenia = includeSensitiveRelations
    };

    public bool IncludeUprawnienia { get; init; } = true;

    public bool IncludeOdznaczenia { get; init; } = true;
}
