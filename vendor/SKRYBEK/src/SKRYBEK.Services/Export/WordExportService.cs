using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Export;

public sealed class WordExportService
{
    public string ExportRozkaz(RozkazDzienny rozkaz, List<Samochod> samochody, string nrJrg, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var fileName = $"{rozkaz.NumerRozkazu}_{rozkaz.Rok}.docx";
        var path = Path.Combine(outputDir, fileName);

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        // Nagłówek dokumentu
        AddHeader(body, rozkaz, nrJrg);

        // 1) SŁUŻBA
        AddSectionTitle(body, "1)   SŁUŻBA");
        AddSluzbaTable(body, rozkaz.Sluzba);

        // 2) PODZIAŁ BOJOWY
        AddSectionTitle(body, "2)   PODZIAŁ BOJOWY");
        AddPodzialBojowyTable(body, rozkaz.PodzialBojowy, samochody);

        // DYŻURNI RATOWNICY MEDYCZNI PSP
        AddRatwnicyMedyczni(body, rozkaz.RatwnicyMedyczni);

        // 3) ZAJĘCIA — bez pustego akapitu po treści (łamanie zaraz potem)
        AddSectionTitle(body, "3)   ZAJĘCIA");
        AddZajecia(body, rozkaz.Zajecia);

        // 4) NIEOBECNI W SŁUŻBIE — strona 2
        body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        AddSectionTitle(body, "4)   NIEOBECNI W SŁUŻBIE");
        AddNieobecniTable(body, rozkaz.Nieobecni);

        // 5) UWAGI
        AddSectionTitle(body, "5)   UWAGI");
        AddUwagi(body, rozkaz.Uwagi);

        // sectPr musi być ostatnim elementem Body (marginesy, duplex, stopka z podpisem)
        SetPageProperties(mainPart, nrJrg);

        mainPart.Document.Save();
        SkrybekLog.Info($"Wyeksportowano rozkaz: {path}");
        return path;
    }

    // ── Ustawienia strony ─────────────────────────────────────────────────────

    /// <summary>Id relacji do części printerSettings (druk dwustronny).</summary>
    private const string PrinterSettingsRelationshipId = "rIdPrinterSettings";

    /// <summary>Szerokość A4 w twips (210 mm).</summary>
    private const int A4WidthTwips = 11906;

    /// <summary>Wysokość A4 w twips (297 mm).</summary>
    private const int A4HeightTwips = 16838;

    /// <summary>Marginesy „Normalne” w Wordzie — 2,54 cm (1440 twips) z każdej strony.</summary>
    private const int NormalMarginTwips = 1440;

    /// <summary>Szerokość obszaru tekstu (A4 minus marginesy normalne).</summary>
    private const int ContentWidthTwips = A4WidthTwips - 2 * NormalMarginTwips;

    private static void SetPageProperties(MainDocumentPart mainPart, string nrJrg)
    {
        // Osadź DEVMODE z DMDUP_VERTICAL — Word pokaże domyślnie
        // „Drukuj dwustronnie / Przerzucaj strony wzdłuż długiej krawędzi”.
        var printerPart = mainPart.AddNewPart<WordprocessingPrinterSettingsPart>(
            PrinterSettingsRelationshipId);
        using (var stream = printerPart.GetStream(FileMode.Create, FileAccess.Write))
            stream.Write(BuildDuplexLongEdgeDevMode());

        // Stopka z podpisem tylko od 2. strony (titlePg = inna pierwsza strona).
        var footerPart = mainPart.AddNewPart<FooterPart>();
        footerPart.Footer = BuildSignatureFooter(nrJrg);
        var footerRelId = mainPart.GetIdOfPart(footerPart);

        var firstFooterPart = mainPart.AddNewPart<FooterPart>();
        firstFooterPart.Footer = new Footer(EmptyParagraph());
        var firstFooterRelId = mainPart.GetIdOfPart(firstFooterPart);

        var sectionProps = new SectionProperties(
            new FooterReference { Type = HeaderFooterValues.Default, Id = footerRelId },
            new FooterReference { Type = HeaderFooterValues.First, Id = firstFooterRelId },
            new TitlePage(),
            new PageSize { Width = A4WidthTwips, Height = A4HeightTwips },
            new PageMargin
            {
                Top    = NormalMarginTwips,
                Right  = NormalMarginTwips,
                Bottom = NormalMarginTwips,
                Left   = NormalMarginTwips,
                Header = 720,
                Footer = 720
            },
            new PrinterSettingsReference { Id = PrinterSettingsRelationshipId });
        mainPart.Document.Body!.AppendChild(sectionProps);
    }

    private static Footer BuildSignatureFooter(string nrJrg)
    {
        var footer = new Footer();
        footer.AppendChild(MakeParagraph(
            $"Rozkaz podpisał D-ca JRG-{nrJrg}",
            alignment: JustificationValues.Left,
            bold: true,
            fontSize: 20));
        footer.AppendChild(SpacerParagraph(fontSize: 16));
        footer.AppendChild(MakeParagraph(
            "................................",
            alignment: JustificationValues.Left,
            fontSize: 20));
        return footer;
    }

    /// <summary>
    /// Minimalny DEVMODEW (220 B) z A4, pionowo i dwustronnie wzdłuż długiej krawędzi
    /// (dmDuplex = DMDUP_VERTICAL = 2).
    /// </summary>
    private static byte[] BuildDuplexLongEdgeDevMode()
    {
        const int cchDeviceName = 32;
        const int cchFormName = 32;
        const ushort dmSpecVersion = 0x0401;
        const ushort dmSize = 220;
        const uint dmOrientation = 0x0000_0001;
        const uint dmPaperSize = 0x0000_0002;
        const uint dmCopies = 0x0000_0100;
        const uint dmDuplex = 0x0000_1000;
        const short dmOrientPortrait = 1;
        const short dmPaperA4 = 9;
        const short dmDupVertical = 2; // długa krawędź
        const short dmColorMonochrome = 1;

        var buffer = new byte[dmSize];
        using var ms = new MemoryStream(buffer);
        using var bw = new BinaryWriter(ms);

        WriteFixedUnicode(bw, "WINSPOOL", cchDeviceName);
        bw.Write(dmSpecVersion);
        bw.Write((ushort)0); // dmDriverVersion
        bw.Write(dmSize);
        bw.Write((ushort)0); // dmDriverExtra
        bw.Write(dmOrientation | dmPaperSize | dmCopies | dmDuplex);
        bw.Write(dmOrientPortrait);
        bw.Write(dmPaperA4);
        bw.Write((short)0); // dmPaperLength
        bw.Write((short)0); // dmPaperWidth
        bw.Write((short)100); // dmScale
        bw.Write((short)1); // dmCopies
        bw.Write((short)0); // dmDefaultSource
        bw.Write((short)0); // dmPrintQuality
        bw.Write(dmColorMonochrome);
        bw.Write(dmDupVertical);
        bw.Write((short)0); // dmYResolution
        bw.Write((short)0); // dmTTOption
        bw.Write((short)0); // dmCollate
        WriteFixedUnicode(bw, "A4", cchFormName);
        // Pozostałe pola (dmLogPixels … dmPanningHeight) zostają zerami.

        return buffer;
    }

    private static void WriteFixedUnicode(BinaryWriter writer, string value, int charCount)
    {
        for (var i = 0; i < charCount; i++)
        {
            var ch = i < value.Length ? value[i] : '\0';
            writer.Write((ushort)ch);
        }
    }

    // ── Nagłówek ──────────────────────────────────────────────────────────────

    private static void AddHeader(Body body, RozkazDzienny rozkaz, string nrJrg)
    {
        // "Kraków, dn. DD.MM.RRRR" — data fizycznego wystawienia (nie „na dzień”)
        var dataWystawienia = DateOnly.FromDateTime(
            rozkaz.DataUtworzenia == default ? DateTime.Now : rozkaz.DataUtworzenia);
        body.AppendChild(MakeParagraph(
            $"Kraków, dn. {dataWystawienia.ToString("dd.MM.yyyy")}",
            alignment: JustificationValues.Right,
            fontSize: 20));

        // Jeden krótki odstęp przed tytułem (zamiast dwóch dużych pustych wierszy)
        body.AppendChild(SpacerParagraph(fontSize: 16));

        body.AppendChild(MakeParagraph(
            $"ROZKAZ DZIENNY NR {rozkaz.NumerFormatowany}",
            bold: true,
            fontSize: 28,
            alignment: JustificationValues.Center));

        body.AppendChild(MakeParagraph(
            $"Dowódcy Jednostki Ratowniczo Gaśniczej Nr {nrJrg}",
            bold: true,
            fontSize: 28,
            alignment: JustificationValues.Center));

        body.AppendChild(MakeParagraph(
            $"na dzień {rozkaz.Data.ToString("dd.MM.yyyy")}",
            fontSize: 28,
            alignment: JustificationValues.Center));

        body.AppendChild(EmptyParagraph());
    }

    // ── Sekcja SŁUŻBA ─────────────────────────────────────────────────────────

    private static void AddSluzbaTable(Body body, List<PozycjaSluzby> sluzba)
    {
        // Układ jak we wzorcu: stanowisko | odstęp | odstęp | - | osoba
        const int colStanowisko = 3783;
        const int colSpacer = 377;
        const int colOsoba = 3783;

        var table = CreateBorderlessTable();
        var allPos = Enum.GetValues<StanowiskoSluzby>().Cast<StanowiskoSluzby>().ToList();

        foreach (var stanowisko in allPos)
        {
            var pozycja = sluzba.FirstOrDefault(s => s.Stanowisko == stanowisko);
            var osoba   = pozycja?.Nazwisko ?? ".............................";

            var row = new TableRow();
            row.AppendChild(MakeCell(
                new PozycjaSluzby { Stanowisko = stanowisko }.NazwaStanowiska,
                width: colStanowisko));
            row.AppendChild(MakeCell(string.Empty, width: colSpacer, center: true));
            row.AppendChild(MakeCell(string.Empty, width: colSpacer, center: true));
            row.AppendChild(MakeCell("-", width: colSpacer, center: true));
            row.AppendChild(MakeCell(osoba, width: colOsoba));
            table.AppendChild(row);
        }

        body.AppendChild(table);
        body.AppendChild(EmptyParagraph());
    }

    // ── Podział bojowy ────────────────────────────────────────────────────────

    private static void AddPodzialBojowyTable(Body body, List<PozycjaSamochodu> podział, List<Samochod> samochody)
    {
        var podstawowe = samochody.Where(s => s.CzyPodstawowy).OrderBy(s => s.Kolejnosc).ToList();
        var dodatkowe  = samochody.Where(s => !s.CzyPodstawowy).OrderBy(s => s.Kolejnosc).ToList();

        if (podstawowe.Count > 0)
        {
            var table = CreateBorderTable();

            // Wiersz nagłówków pojazdów
            var headerRow = new TableRow();
            foreach (var s in podstawowe)
                headerRow.AppendChild(MakeCell(s.Nazwa, bold: true, center: true, width: 3100, shading: "E0E0E0"));
            table.AppendChild(headerRow);

            // Wiersze z osobami (maks pozycji)
            int maxPoz = podstawowe.Max(s => s.LiczbaPozycji);
            for (int poz = 1; poz <= maxPoz; poz++)
            {
                var row = new TableRow();
                foreach (var sam in podstawowe)
                {
                    var wpis = podział.FirstOrDefault(p => p.SamochodId == sam.Id && p.Pozycja == poz);
                    row.AppendChild(MakeCell(
                        wpis?.Nazwisko ?? string.Empty,
                        width: 3100,
                        center: true));
                }
                table.AppendChild(row);
            }
            body.AppendChild(table);
            body.AppendChild(EmptyParagraph());
        }

        // Grupy specjalne (dodatkowe)
        if (dodatkowe.Count > 0)
        {
            int cols = Math.Min(dodatkowe.Count, 3);
            var table = CreateBorderTable();

            var headerRow = new TableRow();
            for (int i = 0; i < cols && i < dodatkowe.Count; i++)
                headerRow.AppendChild(MakeCell(dodatkowe[i].Nazwa, bold: true, center: true, width: 3100, shading: "E0E0E0"));
            table.AppendChild(headerRow);

            int maxPoz = dodatkowe.Take(cols).Max(s => s.LiczbaPozycji);
            for (int poz = 1; poz <= maxPoz; poz++)
            {
                var row = new TableRow();
                for (int i = 0; i < cols && i < dodatkowe.Count; i++)
                {
                    var sam  = dodatkowe[i];
                    var wpis = podział.FirstOrDefault(p => p.SamochodId == sam.Id && p.Pozycja == poz);
                    row.AppendChild(MakeCell(wpis?.Nazwisko ?? string.Empty, width: 3100, center: true));
                }
                table.AppendChild(row);
            }
            body.AppendChild(table);
            body.AppendChild(EmptyParagraph());
        }
    }

    // ── Ratownicy medyczni ────────────────────────────────────────────────────

    private static void AddRatwnicyMedyczni(Body body, List<RatownikMedyczny> ratownicy)
    {
        body.AppendChild(MakeParagraph("DYŻURNI RATOWNICY MEDYCZNI PSP", bold: true, fontSize: 20));

        var r1 = ratownicy.FirstOrDefault(r => r.Pozycja == 1)?.Nazwisko ?? string.Empty;
        var r2 = ratownicy.FirstOrDefault(r => r.Pozycja == 2)?.Nazwisko ?? string.Empty;

        var table = CreateBorderlessTable();
        var row = new TableRow();
        row.AppendChild(MakeCell($"1.  {r1}", width: 4500));
        row.AppendChild(MakeCell($"2.  {r2}", width: 4500));
        table.AppendChild(row);
        body.AppendChild(table);
        body.AppendChild(EmptyParagraph());
    }

    // ── Zajęcia ───────────────────────────────────────────────────────────────

    private static void AddZajecia(Body body, string zajecia)
    {
        // Bez pustego akapitu na końcu — zaraz potem jest łamanie strony do sekcji 4.
        body.AppendChild(MakeParagraph(
            string.IsNullOrWhiteSpace(zajecia) ? "............................................................................" : zajecia,
            fontSize: 20));
    }

    // ── Nieobecni ─────────────────────────────────────────────────────────────

    private static void AddNieobecniTable(Body body, List<NieobecnyWSluzbie> nieobecni)
    {
        var urlopy     = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.Urlop).ToList();
        var wolny      = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.CzasWolny).ToList();
        var chorzy     = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.Chory).ToList();
        var delegowani = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.Delegowany).ToList();
        var domowy     = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.DyzurDomowy).ToList();

        var table = CreateBorderTable();

        // Nagłówek kolumn
        var hdr = new TableRow();
        hdr.AppendChild(MakeCell("URLOPY", bold: true, center: true, width: 3000, shading: "E8E8E8"));
        hdr.AppendChild(MakeCell("CZAS WOLNY", bold: true, center: true, width: 3000, shading: "E8E8E8"));
        hdr.AppendChild(MakeCell("CHORZY", bold: true, center: true, width: 2000, shading: "E8E8E8"));
        hdr.AppendChild(MakeCell("DELEGOWANI", bold: true, center: true, width: 2000, shading: "E8E8E8"));
        table.AppendChild(hdr);

        int maxRows = Math.Max(Math.Max(urlopy.Count, wolny.Count), Math.Max(chorzy.Count, delegowani.Count));
        maxRows = Math.Max(maxRows, 4);

        for (int i = 0; i < maxRows; i++)
        {
            var row = new TableRow();
            row.AppendChild(MakeCell(FormatNieobecny(urlopy, i), width: 3000, center: true));
            row.AppendChild(MakeCell(FormatNieobecny(wolny, i), width: 3000, center: true));
            row.AppendChild(MakeCell(FormatNieobecny(chorzy, i), width: 2000, center: true));
            row.AppendChild(MakeCell(FormatNieobecny(delegowani, i), width: 2000, center: true));
            table.AppendChild(row);
        }
        body.AppendChild(table);

        // DYŻUR DOMOWY
        body.AppendChild(EmptyParagraph());
        body.AppendChild(MakeParagraph("DYŻUR DOMOWY:", bold: true, fontSize: 20));

        var domTbl = CreateBorderlessTable();
        for (int i = 0; i < domowy.Count; i += 2)
        {
            var row = new TableRow();
            row.AppendChild(MakeCell($"{i + 1}.  {domowy[i].Nazwisko}", width: 4500));
            var n2 = i + 1 < domowy.Count ? $"{i + 2}.  {domowy[i + 1].Nazwisko}" : string.Empty;
            row.AppendChild(MakeCell(n2, width: 4500));
            domTbl.AppendChild(row);
        }
        if (domowy.Count == 0)
        {
            var row = new TableRow();
            row.AppendChild(MakeCell("1.  ......................................", width: 4500));
            row.AppendChild(MakeCell("2.  ......................................", width: 4500));
            domTbl.AppendChild(row);
        }
        body.AppendChild(domTbl);

        // Większy odstęp przed sekcją 5) UWAGI
        body.AppendChild(SpacerParagraph(fontSize: 20));
        body.AppendChild(SpacerParagraph(fontSize: 20));
        body.AppendChild(SpacerParagraph(fontSize: 20));
    }

    private static string FormatNieobecny(List<NieobecnyWSluzbie> lista, int idx)
        => idx < lista.Count ? $" {lista[idx].Nazwisko}" : string.Empty;

    // ── Uwagi ─────────────────────────────────────────────────────────────────

    private static void AddUwagi(Body body, string uwagi)
    {
        if (!string.IsNullOrWhiteSpace(uwagi))
            body.AppendChild(MakeParagraph(uwagi, fontSize: 20));
        body.AppendChild(SingleDottedLineParagraph());
    }

    /// <summary>
    /// Jedna linia kropek na szerokość kolumny tekstu — tab z liderem, bez zawijania.
    /// </summary>
    private static Paragraph SingleDottedLineParagraph()
    {
        var runProps = new RunProperties(
            new FontSize { Val = "20" },
            new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });

        return new Paragraph(
            new ParagraphProperties(
                new Tabs(
                    new TabStop
                    {
                        Val = TabStopValues.Right,
                        Leader = TabStopLeaderCharValues.Dot,
                        Position = ContentWidthTwips
                    }),
                new SpacingBetweenLines { Before = "0", After = "40", Line = "240", LineRule = LineSpacingRuleValues.Auto }),
            new Run(runProps, new TabChar()));
    }

    // ── Pomocniki ─────────────────────────────────────────────────────────────

    private static void AddSectionTitle(Body body, string text)
    {
        body.AppendChild(MakeParagraph(text, bold: true, fontSize: 22));
    }

    private static Paragraph EmptyParagraph()
        => new(new ParagraphProperties(
            new SpacingBetweenLines { Before = "0", After = "40", Line = "240", LineRule = LineSpacingRuleValues.Auto }));

    private static Paragraph SpacerParagraph(int fontSize) =>
        MakeParagraph(string.Empty, fontSize: fontSize);

    private static Paragraph MakeParagraph(
        string text,
        bool bold = false,
        int fontSize = 20,
        JustificationValues? alignment = null)
    {
        var runProps = new RunProperties();
        if (bold) runProps.AppendChild(new Bold());
        runProps.AppendChild(new FontSize { Val = fontSize.ToString() });
        runProps.AppendChild(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });

        var align = alignment ?? JustificationValues.Left;
        var para = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = align },
                new SpacingBetweenLines { Before = "0", After = "40", Line = "240", LineRule = LineSpacingRuleValues.Auto }),
            new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        return para;
    }

    private static Table CreateBorderlessTable()
    {
        var tbl = new Table();
        tbl.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder    { Val = BorderValues.None },
                new BottomBorder { Val = BorderValues.None },
                new LeftBorder   { Val = BorderValues.None },
                new RightBorder  { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None },
                new InsideVerticalBorder   { Val = BorderValues.None })));
        return tbl;
    }

    private static Table CreateBorderTable()
    {
        var tbl = new Table();
        tbl.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder    { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder   { Val = BorderValues.Single, Size = 4 },
                new RightBorder  { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder   { Val = BorderValues.Single, Size = 4 })));
        return tbl;
    }

    private static TableCell MakeCell(
        string text,
        int width = 2000,
        bool bold = false,
        bool center = false,
        string? shading = null)
    {
        var cell = new TableCell();
        var cellProps = new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width.ToString() });
        if (shading is not null)
            cellProps.AppendChild(new Shading { Fill = shading, Val = ShadingPatternValues.Clear });
        cell.AppendChild(cellProps);

        var runProps = new RunProperties();
        if (bold) runProps.AppendChild(new Bold());
        runProps.AppendChild(new FontSize { Val = "20" });
        runProps.AppendChild(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });

        var alignment = center ? JustificationValues.Center : JustificationValues.Left;
        cell.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Justification { Val = alignment },
                new SpacingBetweenLines { Before = "0", After = "20", Line = "240", LineRule = LineSpacingRuleValues.Auto }),
            new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
        return cell;
    }
}
