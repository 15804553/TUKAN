using ClosedXML.Excel;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services.Export;

namespace BOBER.Services.Tests.Export;

public sealed class GrafikSluzbExportMarksTests
{
    [Theory]
    [InlineData("S", "S")]
    [InlineData("C", "C")]
    [InlineData("Del", "Del")]
    [InlineData("D", "D")]
    [InlineData("U", "U")]
    [InlineData("U/", "U—")]
    [InlineData("D/", "D—")]
    [InlineData("WS/", "—")]
    [InlineData("WS", "")]
    [InlineData("U.", "U•")]
    [InlineData("WS.", "•")]
    [InlineData("?", "?")]
    public void TekstWyswietlany_NoweZnaczkiDoEksportu(string kod, string expected)
    {
        Assert.Equal(expected, GrafikWpisTypy.TekstWyswietlany(kod));
    }

    [Fact]
    public void ExportMonth_ZapisujeWszystkieNoweZnaczkiWKomorkach()
    {
        var funkcjonariusze = new List<Funkcjonariusz>
        {
            new() { Id = 1, Imie = "Anna", Nazwisko = "Nowak", Stanowisko = "Strażak" },
            new() { Id = 2, Imie = "Jan", Nazwisko = "Kowalski", Stanowisko = "Strażak" },
            new() { Id = 3, Imie = "Piotr", Nazwisko = "Wiśniewski", Stanowisko = "Strażak" },
            new() { Id = 4, Imie = "Ewa", Nazwisko = "Zielińska", Stanowisko = "Strażak" },
            new() { Id = 5, Imie = "Adam", Nazwisko = "Wójcik", Stanowisko = "Strażak" },
            new() { Id = 6, Imie = "Olga", Nazwisko = "Kamińska", Stanowisko = "Strażak" },
            new() { Id = 7, Imie = "Marek", Nazwisko = "Lewandowski", Stanowisko = "Strażak" },
        };

        // Dzień 1 (kolumna 3): każdy funkcjonariusz ma inny kod do sprawdzenia.
        var wpisy = new List<GrafikWpis>
        {
            Wpis(1, 1, "S"),
            Wpis(2, 1, "C"),
            Wpis(3, 1, "U/"),   // Oddaje
            Wpis(4, 1, "U."),   // chętna oddać
            Wpis(5, 1, "?"),    // potrzebuje wolne
            Wpis(6, 1, "WS."),  // WS + kropka
            Wpis(7, 1, "WS/"),  // WS + Oddaje
        };

        var path = Path.Combine(Path.GetTempPath(), $"tukan-grafik-export-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ExportService().ExportMonth(
                path,
                rok: 2026,
                miesiac: 7,
                funkcjonariusze,
                wpisy,
                stanZmiany: 7,
                stanMinimalny: 4,
                kolory: DefaultKolory(),
                workDays: [1]);

            Assert.True(File.Exists(path));

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);

            // Wiersz danych zaczyna się od 3; kolumna dnia 1 = 3.
            Assert.Equal("S", CellText(ws, 3, 3));
            Assert.Equal("C", CellText(ws, 4, 3));
            Assert.Equal("U—", CellText(ws, 5, 3));
            Assert.Equal("U•", CellText(ws, 6, 3));
            Assert.Equal("?", CellText(ws, 7, 3));
            Assert.Equal("•", CellText(ws, 8, 3));
            Assert.Equal("—", CellText(ws, 9, 3));

            // WS ma tło nieobecności także przy kropce / Oddaje.
            Assert.True(ws.Cell(8, 3).Style.Fill.BackgroundColor.ColorType != XLColorType.Theme);
            Assert.True(ws.Cell(9, 3).Style.Fill.BackgroundColor.ColorType != XLColorType.Theme);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportMonth_Podsumowanie_NieLiczyPytajnikaAniOddajeJakoNieobecnosci()
    {
        var funkcjonariusze = new List<Funkcjonariusz>
        {
            new() { Id = 1, Imie = "A", Nazwisko = "Jeden", Stanowisko = "Strażak" },
            new() { Id = 2, Imie = "B", Nazwisko = "Dwa", Stanowisko = "Strażak" },
            new() { Id = 3, Imie = "C", Nazwisko = "Trzy", Stanowisko = "Strażak" },
        };

        var wpisy = new List<GrafikWpis>
        {
            Wpis(1, 1, "?"),
            Wpis(2, 1, "U/"),
            Wpis(3, 1, "S"),
        };

        var path = Path.Combine(Path.GetTempPath(), $"tukan-grafik-sum-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ExportService().ExportMonth(
                path, 2026, 7, funkcjonariusze, wpisy,
                stanZmiany: 3, stanMinimalny: 2,
                kolory: DefaultKolory(),
                workDays: [1]);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);

            // sumBase = 3 funkcjonariuszy + 3 = 6; "Wolne miejsca" w wierszu 6, kolumna 3
            // nieobecny tylko S (1 osoba): wolne = 3 - 2 - 1 = 0
            Assert.Equal(0, ws.Cell(6, 3).GetValue<int>());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static IReadOnlyDictionary<string, string> DefaultKolory()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in RoleKeys.DomyslneKolory)
            map[kv.Key] = kv.Value;
        foreach (var kv in RoleKeys.DomyslneKoloryWpisow)
            map[kv.Key] = kv.Value;
        foreach (var kv in RoleKeys.DomyslneKoloryEksportu)
            map[kv.Key] = kv.Value;
        return map;
    }

    private static GrafikWpis Wpis(int fid, int dzien, string typ) => new()
    {
        FunkcjonariuszId = fid,
        ZmianaId = 1,
        Rok = 2026,
        Miesiac = 7,
        Dzien = dzien,
        TypWpisu = typ
    };

    private static string CellText(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        return cell.IsEmpty() ? string.Empty : cell.GetString();
    }
}
