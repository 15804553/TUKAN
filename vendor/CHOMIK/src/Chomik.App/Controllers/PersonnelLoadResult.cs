using Chomik.App.ViewModels;

namespace Chomik.App.Controllers;

public sealed class PersonnelLoadResult
{
    public required IReadOnlyList<FunkcjonariuszRowViewModel> Rows { get; init; }

    public double DatabaseSeconds { get; init; }

    public double MappingSeconds { get; init; }

    public bool FromCache { get; init; }
}
