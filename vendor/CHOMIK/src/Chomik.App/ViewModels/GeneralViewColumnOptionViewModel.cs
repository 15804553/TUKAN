using Chomik.Core.GeneralView;

namespace Chomik.App.ViewModels;

public sealed class GeneralViewColumnOptionViewModel
{
    public required GeneralViewColumnId ColumnId { get; init; }

    public required string Label { get; init; }

    public bool IsSelected { get; set; }
}
