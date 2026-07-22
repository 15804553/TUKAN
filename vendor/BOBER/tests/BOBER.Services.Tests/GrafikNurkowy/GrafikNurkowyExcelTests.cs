using ClosedXML.Excel;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using BOBER.Services.GrafikNurkowy;
using BOBER.Services.Urlop;

namespace BOBER.Services.Tests.GrafikNurkowy;

public sealed class GrafikNurkowyMappingTests
{
    [Theory]
    [InlineData(null, "1")]
    [InlineData("", "1")]
    [InlineData("   ", "1")]
    [InlineData("U", "U")]
    [InlineData("u", "U")]
    [InlineData("D", null)]
    [InlineData("WS", null)]
    [InlineData("Del", null)]
    [InlineData("S", null)]
    [InlineData("C", null)]
    [InlineData("U/", "1")]
    [InlineData("D/", "1")]
    [InlineData("WS/", "1")]
    public void MapFromGrafikWpis_MapsOnlyWorkAndVacation(string? typ, string? expected)
    {
        Assert.Equal(expected, GrafikNurkowyConstants.MapFromGrafikWpis(typ));
    }

    [Fact]
    public void BuildFileName_ContainsMonthAndYear()
    {
        var name = GrafikNurkowyConstants.BuildFileName(8, 2026);
        Assert.Equal("Grafiki nurków SGRW-N Kraków Sierpień 2026.xlsx", name);
    }

    [Fact]
    public void ResolveFunkcja_PrefersKpp()
    {
        var f = new Funkcjonariusz
        {
            Imie = "Jan",
            Nazwisko = "Kowalski",
            NazwyUprawnien = ["Nurek", "Kierownik prac podwodnych"]
        };
        Assert.Equal(GrafikNurkowyConstants.FunkcjaKpp, GrafikNurkowyConstants.ResolveFunkcja(f));
    }

    [Theory]
    [InlineData("Mł.nurek", "mł.nurek")]
    [InlineData("mł.nurek", "mł.nurek")]
    [InlineData("Młodszy nurek", "mł.nurek")]
    [InlineData("Nurek", "nurek")]
    public void ResolveFunkcja_MapsNurekAndMlodszyNurek(string uprawnienie, string expected)
    {
        var f = new Funkcjonariusz
        {
            Imie = "Jan",
            Nazwisko = "Kowalski",
            NazwyUprawnien = [uprawnienie]
        };
        Assert.Equal(expected, GrafikNurkowyConstants.ResolveFunkcja(f));
    }

    [Fact]
    public void MaUprawnieniaNumek_IncludesMlodszyNurek()
    {
        var f = new Funkcjonariusz { NazwyUprawnien = ["Mł.nurek"] };
        Assert.True(f.MaUprawnieniaNumek);
        Assert.True(RoleClassifier.IsNurek(f));
    }
}

public sealed class GrafikNurkowyExcelServiceTests
{
    [Fact]
    public void CreateOrUpdate_WritesWorkAndVacationValues()
    {
        var service = new GrafikNurkowyExcelService();
        var path = Path.Combine(Path.GetTempPath(), $"grafik_nurkowy_{Guid.NewGuid():N}.xlsx");
        var nurkowie = new List<Funkcjonariusz>
        {
            new()
            {
                Id = 1,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                NumerZmiany = 1,
                NazwyUprawnien = ["Nurek"]
            },
            new()
            {
                Id = 2,
                Imie = "Anna",
                Nazwisko = "Nowak",
                NumerZmiany = 2,
                NazwyUprawnien = ["Kierownik prac podwodnych"]
            }
        };

        var workDays = new HashSet<int> { 1, 4, 7 };
        var wartosci = new Dictionary<(int, int), string?>
        {
            [(1, 1)] = "1",
            [(1, 4)] = "U",
            [(1, 7)] = "1"
        };

        try
        {
            service.CreateOrUpdate(path, 2026, 8, 1, [nurkowie[0]], nurkowie, wartosci, workDays);

            var preview = service.ReadPreview(path, 2026, 8);
            Assert.Equal(2, preview.Count);

            var kowalski = preview.Single(p => p.ImieNazwisko.Contains("Kowalski", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("1", kowalski.Dni[1]);
            Assert.Equal("U", kowalski.Dni[4]);
            Assert.Equal("1", kowalski.Dni[7]);
            Assert.False(kowalski.Dni.ContainsKey(2));

            // Aktualizacja tej samej zmiany nie kasuje drugiej osoby
            service.CreateOrUpdate(path, 2026, 8, 1, [nurkowie[0]], nurkowie, wartosci, workDays);
            preview = service.ReadPreview(path, 2026, 8);
            Assert.Contains(preview, p => p.ImieNazwisko.Contains("Nowak", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CreateOrUpdate_AppliesTemplateColorsAndMerges()
    {
        var service = new GrafikNurkowyExcelService();
        var path = Path.Combine(Path.GetTempPath(), $"grafik_nurkowy_style_{Guid.NewGuid():N}.xlsx");
        var nurkowie = new List<Funkcjonariusz>
        {
            new()
            {
                Id = 1,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                NumerZmiany = 1,
                NazwyUprawnien = ["Nurek"]
            },
            new()
            {
                Id = 2,
                Imie = "Anna",
                Nazwisko = "Nowak",
                NumerZmiany = 2,
                NazwyUprawnien = ["Kierownik prac podwodnych"]
            }
        };

        try
        {
            service.CreateOrUpdate(
                path, 2026, 8, 1,
                [nurkowie[0]],
                nurkowie,
                new Dictionary<(int, int), string?> { [(1, 3)] = "1", [(1, 6)] = "U" },
                new HashSet<int> { 3, 6, 9 },
                new Dictionary<int, int> { [1] = 2, [3] = 1 });

            using var workbook = new XLWorkbook(path);
            var ws = workbook.Worksheet(GrafikNurkowyConstants.BuildSheetName(8, 2026));

            // Day 1 = zm. II (róż), day 3 = zm. I (żółty) — wg mapy służb
            Assert.True(ColorsMatch(ws.Cell(2, 4).Style.Fill.BackgroundColor, GrafikNurkowyConstants.ColorZmiana2));
            Assert.True(ColorsMatch(ws.Cell(2, 6).Style.Fill.BackgroundColor, GrafikNurkowyConstants.ColorZmiana1));

            Assert.True(ws.Cell(3, 1).IsMerged());
            Assert.False(ws.Cell(3, 1).GetString().Contains("SGRW", StringComparison.OrdinalIgnoreCase));
            Assert.True(ColorsMatch(ws.Cell(3, 1).Style.Fill.BackgroundColor, GrafikNurkowyConstants.ColorJednostkaPsp));
            Assert.True(ColorsMatch(ws.Cell(3, 2).Style.Fill.BackgroundColor, GrafikNurkowyConstants.ColorZmiana1));
            Assert.True(ColorsMatch(ws.Cell(4, 2).Style.Fill.BackgroundColor, GrafikNurkowyConstants.ColorZmiana2));
            Assert.Equal(XLAlignmentHorizontalValues.Center,
                ws.Cell(3, 6).Style.Alignment.Horizontal);
            Assert.True(string.IsNullOrWhiteSpace(ws.Cell(2, 1).GetString()));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void CreateOrUpdate_UsesSumFormulasAndConditionalFormats()
    {
        var service = new GrafikNurkowyExcelService();
        var path = Path.Combine(Path.GetTempPath(), $"grafik_nurkowy_cf_{Guid.NewGuid():N}.xlsx");
        var nurkowie = new List<Funkcjonariusz>
        {
            new()
            {
                Id = 1,
                Imie = "Jan",
                Nazwisko = "Kowalski",
                NumerZmiany = 1,
                NazwyUprawnien = ["Nurek"]
            },
            new()
            {
                Id = 2,
                Imie = "Anna",
                Nazwisko = "Nowak",
                NumerZmiany = 1,
                NazwyUprawnien = ["Kierownik prac podwodnych"]
            }
        };

        try
        {
            service.CreateOrUpdate(
                path, 2026, 8, 1,
                nurkowie,
                nurkowie,
                new Dictionary<(int, int), string?>
                {
                    [(1, 3)] = "1",
                    [(1, 6)] = "U",
                    [(2, 3)] = "1"
                },
                new HashSet<int> { 3, 6, 9 });

            using var workbook = new XLWorkbook(path);
            var ws = workbook.Worksheet(GrafikNurkowyConstants.BuildSheetName(8, 2026));
            var summaryRow = 5; // 2 osoby + pusty odstęp? first data=3, 2 people → rows 3-4, summary at 6

            // Znajdź wiersz podsumowania po etykiecie
            summaryRow = Enumerable.Range(3, 10)
                .First(r => ws.Cell(r, GrafikNurkowyConstants.ColImieNazwisko).GetString()
                    .Contains("JRG", StringComparison.OrdinalIgnoreCase));

            var day3Col = GrafikNurkowyConstants.FirstDayCol + 3 - 1;
            var formula = ws.Cell(summaryRow, day3Col).FormulaA1;
            Assert.StartsWith("SUM(", formula, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("3:", formula);

            // Wartość „1” zapisana jako liczba
            Assert.True(ws.Cell(3, day3Col).TryGetValue(out double numeric));
            Assert.Equal(1d, numeric);

            // „U” jako tekst
            var day6Col = GrafikNurkowyConstants.FirstDayCol + 6 - 1;
            Assert.Equal("U", ws.Cell(3, day6Col).GetString());

            Assert.True(ws.ConditionalFormats.Any());
            Assert.Contains(ws.ConditionalFormats, cf =>
                cf.Values.Any(v => v.Value.Value == "\"KPP\"" || v.Value.Value == "KPP")
                || cf.Values.Values.Any(v => v.Value.Contains("KPP", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static bool ColorsMatch(XLColor actual, string expectedHex)
    {
        var expected = XLColor.FromHtml(expectedHex);
        return actual.Color.R == expected.Color.R
            && actual.Color.G == expected.Color.G
            && actual.Color.B == expected.Color.B;
    }
}
