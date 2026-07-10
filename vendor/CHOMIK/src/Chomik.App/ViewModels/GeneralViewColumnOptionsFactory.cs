using Chomik.Core.GeneralView;

namespace Chomik.App.ViewModels;

public static class GeneralViewColumnOptionsFactory
{
    public static List<GeneralViewColumnOptionViewModel> Create(
        IReadOnlyList<GeneralViewColumnId> selectableColumns,
        GeneralViewColumnPreferences preferences) =>
        selectableColumns
            .Select(id => new GeneralViewColumnOptionViewModel
            {
                ColumnId = id,
                Label = GeneralViewColumnLabels.GetLabel(id),
                IsSelected = preferences.IsVisible(id)
            })
            .ToList();

    public static GeneralViewColumnPreferences ToPreferences(
        IEnumerable<GeneralViewColumnOptionViewModel> options) =>
        new(options.Where(o => o.IsSelected).Select(o => o.ColumnId));
}
