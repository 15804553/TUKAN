namespace Chomik.Core.GeneralView;

public static class GeneralViewColumnLabels
{
    public static string GetLabel(GeneralViewColumnId columnId) =>
        columnId switch
        {
            GeneralViewColumnId.Zmiana => "Zmiana",
            GeneralViewColumnId.Stanowisko => "Stanowisko",
            GeneralViewColumnId.UprawnieniaAlert => "Uwaga (uprawnienia)",
            GeneralViewColumnId.Wstepienie => "Wstąpienie do służby",
            GeneralViewColumnId.Badania => "Badania okresowe do",
            GeneralViewColumnId.Komora => "Komora dymowa do",
            GeneralViewColumnId.Kpp => "KPP do",
            GeneralViewColumnId.Uprawnienia => "Uprawnienia",
            GeneralViewColumnId.Dodatek => "Dodatek motywacyjny",
            GeneralViewColumnId.Awans => "Awans (stopień)",
            GeneralViewColumnId.Odznaczenia => "Odznaczenia",
            GeneralViewColumnId.InneUwagi => "Inne uwagi",
            _ => columnId.ToString()
        };
}
