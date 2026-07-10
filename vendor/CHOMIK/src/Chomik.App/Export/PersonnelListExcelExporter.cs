using ClosedXML.Excel;

namespace Chomik.App.Export;

public static class PersonnelListExcelExporter
{
    private const string ColumnHeader = "Imię i nazwisko";

    public static void Export(IReadOnlyList<string> fullNames, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Lista osób");

        worksheet.Cell(1, 1).Value = ColumnHeader;
        worksheet.Cell(1, 1).Style.Font.Bold = true;

        for (var row = 0; row < fullNames.Count; row++)
        {
            worksheet.Cell(row + 2, 1).Value = fullNames[row];
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}
