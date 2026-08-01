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
    [InlineData("U/", "U")]
    [InlineData("D/", "D")]
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

            // Wiersz danych zaczyna się od 3; kolumna dnia 1 = 3 (D/N/K | Imię | dzień…).
            Assert.Equal("S", CellText(ws, 3, 3));
            Assert.Equal("C", CellText(ws, 4, 3));
            Assert.Equal("U", CellText(ws, 5, 3));
            Assert.Equal("U•", CellText(ws, 6, 3));
            Assert.Equal("?", CellText(ws, 7, 3));
            Assert.Equal("•", CellText(ws, 8, 3));
            Assert.Equal("—", CellText(ws, 9, 3));

            // Oddaje przy U → przekreślenie; przy WS → „—” bez przekreślenia
            Assert.True(ws.Cell(5, 3).Style.Font.Strikethrough);
            Assert.False(ws.Cell(9, 3).Style.Font.Strikethrough);
            Assert.False(ws.Cell(3, 3).Style.Font.Strikethrough);

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

            // sumBase = 3 funkcjonariuszy + 3 = 6; "Wolne miejsca" w wierszu 6, kolumna dnia 1 = 3
            // nieobecny tylko S (1 osoba): wolne = 3 - 2 - 1 = 0
            Assert.Equal(0, ws.Cell(6, 3).GetValue<int>());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportMonth_OznaczeniaRol_WWaskiejKolumniePrzedNazwiskiem()
    {
        var funkcjonariusze = new List<Funkcjonariusz>
        {
            new() { Id = 1, Imie = "Anna", Nazwisko = "Nowak", Stanowisko = "Dowódca zmiany" },
            new()
            {
                Id = 2, Imie = "Jan", Nazwisko = "Kowalski", Stanowisko = "Strażak",
                NazwyUprawnien = ["Nurek"]
            },
            new()
            {
                Id = 3, Imie = "Piotr", Nazwisko = "Wiśniewski", Stanowisko = "Kierowca",
                NazwyUprawnien = ["Prawo jazdy kat. C"]
            },
            new()
            {
                Id = 4, Imie = "Ewa", Nazwisko = "Zielińska", Stanowisko = "Dowódca sekcji",
                NazwyUprawnien = ["Nurek", "Prawo jazdy kat. C+E"]
            },
            new() { Id = 5, Imie = "Adam", Nazwisko = "Wójcik", Stanowisko = "Strażak" },
        };

        var path = Path.Combine(Path.GetTempPath(), $"tukan-grafik-marks-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ExportService().ExportMonth(
                path, 2026, 7, funkcjonariusze, wpisy: [],
                stanZmiany: 5, stanMinimalny: 3,
                kolory: DefaultKolory(),
                workDays: [1]);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);

            // Kolumna 1 = D/N/K, kolumna 2 = imię i nazwisko (bez Lp.)
            Assert.Equal("D", CellText(ws, 3, 1));
            Assert.Equal("Anna Nowak", CellText(ws, 3, 2));
            Assert.Equal("N", CellText(ws, 4, 1));
            Assert.Equal("Jan Kowalski", CellText(ws, 4, 2));
            Assert.Equal("K", CellText(ws, 5, 1));
            Assert.Equal("Piotr Wiśniewski", CellText(ws, 5, 2));
            Assert.Equal("DNK", CellText(ws, 6, 1));
            Assert.Equal("Ewa Zielińska", CellText(ws, 6, 2));
            Assert.Equal(string.Empty, CellText(ws, 7, 1));
            Assert.Equal("Adam Wójcik", CellText(ws, 7, 2));

            Assert.Equal(8.0, ws.Cell(3, 1).Style.Font.FontSize);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportMonth_SzerokoscNazwiska_DopasowanaDoTresciWBudzecieA4()
    {
        var funkcjonariusze = new List<Funkcjonariusz>
        {
            new()
            {
                Id = 1,
                Imie = "Aleksander",
                Nazwisko = "Chrząszczyżewoszyński",
                Stanowisko = "Strażak"
            },
            new() { Id = 2, Imie = "Jan", Nazwisko = "Kot", Stanowisko = "Strażak" },
        };

        var workDays = Enumerable.Range(1, 11).ToList();
        var path = Path.Combine(Path.GetTempPath(), $"tukan-grafik-width-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ExportService().ExportMonth(
                path, 2026, 7, funkcjonariusze, wpisy: [],
                stanZmiany: 2, stanMinimalny: 1,
                kolory: DefaultKolory(),
                workDays: workDays);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);

            var nameWidth = ws.Column(2).Width;
            Assert.True(nameWidth > 18, $"Oczekiwano szerszej kolumny nazwiska dla długiego wpisu, jest {nameWidth}");
            Assert.True(nameWidth <= 28);

            var total = ws.Column(1).Width + nameWidth
                + Enumerable.Range(3, workDays.Count).Sum(c => ws.Column(c).Width);
            Assert.True(total <= 132.01, $"Suma szerokości {total} przekracza budżet A4");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Theory]
    [InlineData("Dowódca zmiany", null, "D")]
    [InlineData("Dowódca sekcji", null, "D")]
    [InlineData("Zastępca dowódcy zmiany", null, "D")]
    [InlineData("Dowódca zastępu", null, "D")]
    [InlineData("Strażak", new[] { "Nurek" }, "N")]
    [InlineData("Strażak", new[] { "Kierownik prac podwodnych" }, "N")]
    [InlineData("Kierowca", null, "K")]
    [InlineData("Strażak", new[] { "Prawo jazdy kat. C" }, "K")]
    [InlineData("Dowódca zmiany", new[] { "Nurek", "Prawo jazdy kat. C" }, "DNK")]
    [InlineData("Strażak", null, "")]
    public void FormatExportRoleMarks_BudujeOznaczenia(string stanowisko, string[]? uprawnienia, string expected)
    {
        var f = new Funkcjonariusz
        {
            Stanowisko = stanowisko,
            NazwyUprawnien = uprawnienia?.ToList() ?? []
        };
        Assert.Equal(expected, RoleClassifier.FormatExportRoleMarks(f));
    }

    [Fact]
    public void ExportMonth_LessColor_BialeWierszeCzarnaCzcionkaZolteTylkoWs()
    {
        var funkcjonariusze = new List<Funkcjonariusz>
        {
            new() { Id = 1, Imie = "Anna", Nazwisko = "Nowak", Stanowisko = "Dowódca zmiany" },
            new()
            {
                Id = 2, Imie = "Jan", Nazwisko = "Kowalski", Stanowisko = "Strażak",
                NazwyUprawnien = ["Nurek"]
            },
            new()
            {
                Id = 3, Imie = "Piotr", Nazwisko = "Wiśniewski", Stanowisko = "Kierowca",
                NazwyUprawnien = ["Prawo jazdy kat. C"]
            },
        };

        var wpisy = new List<GrafikWpis>
        {
            Wpis(1, 1, "WS"),
            Wpis(2, 1, "D"),
            Wpis(3, 1, "Del"),
        };

        var path = Path.Combine(Path.GetTempPath(), $"tukan-grafik-lesscolor-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ExportService().ExportMonth(
                path, 2026, 7, funkcjonariusze, wpisy,
                stanZmiany: 3, stanMinimalny: 2,
                kolory: DefaultKolory(),
                workDays: [1],
                lessColor: true);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);

            var white = XLColor.FromHtml("#FFFFFF");
            var black = XLColor.FromHtml("#000000");
            var wsYellow = XLColor.FromHtml(RoleKeys.DomyslneKoloryWpisow[RoleKeys.WolnaSluzba]);

            // Dowódca: białe tło, czarna czcionka (bez pomarańczu roli)
            Assert.Equal(white, ws.Cell(3, 1).Style.Fill.BackgroundColor);
            Assert.Equal(black, ws.Cell(3, 1).Style.Font.FontColor);
            Assert.Equal(white, ws.Cell(3, 2).Style.Fill.BackgroundColor);
            Assert.Equal(black, ws.Cell(3, 2).Style.Font.FontColor);

            // Nurek: czarna czcionka (bez czerwieni uprawnień)
            Assert.Equal(white, ws.Cell(4, 2).Style.Fill.BackgroundColor);
            Assert.Equal(black, ws.Cell(4, 2).Style.Font.FontColor);
            Assert.Equal(black, ws.Cell(4, 1).Style.Font.FontColor);

            // Kierowca: białe tło (bez szarości roli)
            Assert.Equal(white, ws.Cell(5, 2).Style.Fill.BackgroundColor);
            Assert.Equal(black, ws.Cell(5, 2).Style.Font.FontColor);

            // WS — żółte; D i Del — białe (bez żółtego)
            Assert.Equal(wsYellow, ws.Cell(3, 3).Style.Fill.BackgroundColor);
            Assert.Equal(white, ws.Cell(4, 3).Style.Fill.BackgroundColor);
            Assert.Equal(white, ws.Cell(5, 3).Style.Fill.BackgroundColor);

            // Czcionka czarna w całym pliku: nagłówek, wiersze, stopka
            Assert.Equal(black, ws.Cell(1, 2).Style.Font.FontColor);
            Assert.Equal(black, ws.Cell(2, 3).Style.Font.FontColor);
            Assert.Equal(black, ws.Cell(3, 3).Style.Font.FontColor);
            // sumBase = 3 + 3 = 6
            Assert.Equal(black, ws.Cell(6, 2).Style.Font.FontColor);
            Assert.Equal(black, ws.Cell(6, 3).Style.Font.FontColor);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportMonth_BezLessColor_ZachowujeKoloryRolINurek()
    {
        var funkcjonariusze = new List<Funkcjonariusz>
        {
            new() { Id = 1, Imie = "Anna", Nazwisko = "Nowak", Stanowisko = "Dowódca zmiany" },
            new()
            {
                Id = 2, Imie = "Jan", Nazwisko = "Kowalski", Stanowisko = "Strażak",
                NazwyUprawnien = ["Nurek"]
            },
        };

        var wpisy = new List<GrafikWpis>
        {
            Wpis(1, 1, "D"),
        };

        var path = Path.Combine(Path.GetTempPath(), $"tukan-grafik-fullcolor-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ExportService().ExportMonth(
                path, 2026, 7, funkcjonariusze, wpisy,
                stanZmiany: 2, stanMinimalny: 1,
                kolory: DefaultKolory(),
                workDays: [1],
                lessColor: false);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);

            var dcaOrange = XLColor.FromHtml(RoleKeys.DomyslneKolory[RoleKeys.DowodcaZmiany]);
            var nurekRed = XLColor.FromHtml(RoleKeys.DomyslneKoloryWpisow[RoleKeys.NurekCzcionka]);
            var dYellow = XLColor.FromHtml(RoleKeys.DomyslneKoloryWpisow[RoleKeys.WolnaSluzba]);

            Assert.Equal(dcaOrange, ws.Cell(3, 2).Style.Fill.BackgroundColor);
            Assert.Equal(nurekRed, ws.Cell(4, 2).Style.Font.FontColor);
            Assert.Equal(dYellow, ws.Cell(3, 3).Style.Fill.BackgroundColor);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void ExportMonth_Stopka_ZawieraLegendeOznaczen()
    {
        var funkcjonariusze = new List<Funkcjonariusz>
        {
            new() { Id = 1, Imie = "Anna", Nazwisko = "Nowak", Stanowisko = "Strażak" },
        };

        var path = Path.Combine(Path.GetTempPath(), $"tukan-grafik-legend-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ExportService().ExportMonth(
                path, 2026, 7, funkcjonariusze, wpisy: [],
                stanZmiany: 1, stanMinimalny: 1,
                kolory: DefaultKolory(),
                workDays: [1]);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(1);

            // sumBase = 1+3 = 4; 5 wierszy sum → lastRow = 8; legenda od wiersza 10
            Assert.Contains("Dyżur", CellText(ws, 10, 1));
            Assert.Contains("Wolna służba", CellText(ws, 10, 1));
            Assert.Contains("Urlop", CellText(ws, 10, 1));
            Assert.Contains("Oddaje", CellText(ws, 11, 1));
            Assert.Contains("Nurek", CellText(ws, 11, 1));
            Assert.Contains("Kierowca", CellText(ws, 11, 1));
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
