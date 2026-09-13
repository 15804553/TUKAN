using BOBER.Core.Enums;
using BOBER.Core.Models;
using BOBER.Core.Oznaczenia;

namespace BOBER.Core.Constants;

/// <summary>
/// Kody wpisów grafiku + flagi (LEWA/PRAWA jako sufiksy, CENTRUM jako overlay).
/// Format: <c>BAZA[\u001ELkod][\u001ERkod][\u001EC][\u001EW|\u001EF#RRGGBB]</c>
/// <c>W</c> / <c>F#…</c> = zachowane tło komórki przy oznaczeniu bez własnego koloru.
/// </summary>
public static class GrafikWpisTypy
{
    public const string Dyzur = "D";
    public const string WolnaSluzba = "WS";
    public const string Urlop = "U";
    public const string UrlopZWolnaSluzba = "UWS";
    public const string UrlopRodzicielski = "Ur";
    public const string Delegacja = "Del";
    public const string Szkolenie = "S";
    public const string Chory = "C";
    public const string PotrzebujeWolne = "?";

    public const string UrlopPlanowanyIndeks = "\u209A";
    public const string UrlopPlanowanyTekst = Urlop + UrlopPlanowanyIndeks;
    public const string UrlopRodzicielskiIndeks = "\u1D63";
    public const string UrlopRodzicielskiTekst = Urlop + UrlopRodzicielskiIndeks;

    /// <summary>Separator segmentów flag w TypWpisu.</summary>
    public const char FlagaSeparator = '\u001E';

    /// <summary>Zachowane tło przy oznaczeniu z „brak koloru”.</summary>
    public readonly record struct ZachowaneTloInfo(bool JakWs, string? Hex)
    {
        public static ZachowaneTloInfo Brak => default;
        public static ZachowaneTloInfo Ws => new(true, null);
        public static ZachowaneTloInfo ZHex(string hex) => new(false, hex);
        public bool MaWartosc => JakWs || !string.IsNullOrWhiteSpace(Hex);
    }

    public readonly record struct WpisFlags(
        string Bazowy,
        string? LewaKod,
        string? PrawaKod,
        bool Centrum,
        bool ZachowajTloWs = false,
        string? ZachowaneTloHex = null);

    public static WpisFlags Parse(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return new WpisFlags(string.Empty, null, null, false);

        var parts = typWpisu.Trim().Split(FlagaSeparator);
        var bazowy = parts[0];
        string? lewa = null;
        string? prawa = null;
        var centrum = false;
        var zachowajTloWs = false;
        string? zachowaneTloHex = null;

        for (var i = 1; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length == 0)
                continue;
            if (p[0] == 'L' && p.Length > 1)
                lewa = p[1..];
            else if (p[0] == 'R' && p.Length > 1)
                prawa = p[1..];
            else if (p[0] == 'C')
                centrum = true;
            else if (p[0] == 'W')
                zachowajTloWs = true;
            else if (p[0] == 'F' && p.Length > 1)
                zachowaneTloHex = p[1..];
        }

        return new WpisFlags(bazowy, lewa, prawa, centrum, zachowajTloWs, zachowaneTloHex);
    }

    public static string Compose(
        string? bazowy,
        string? lewaKod,
        string? prawaKod,
        bool centrum,
        bool zachowajTloWs = false,
        string? zachowaneTloHex = null)
    {
        var b = (bazowy ?? string.Empty).Trim();
        var sb = new System.Text.StringBuilder(b);
        if (!string.IsNullOrWhiteSpace(lewaKod))
            sb.Append(FlagaSeparator).Append('L').Append(lewaKod.Trim());
        if (!string.IsNullOrWhiteSpace(prawaKod))
            sb.Append(FlagaSeparator).Append('R').Append(prawaKod.Trim());
        if (centrum)
            sb.Append(FlagaSeparator).Append('C');

        // W = tło jak WS (kolor z ustawień); F#hex = konkretne zachowane tło.
        if (zachowajTloWs)
            sb.Append(FlagaSeparator).Append('W');
        else if (!string.IsNullOrWhiteSpace(zachowaneTloHex)
                 && !RoleKeys.IsBrakWypelnienia(zachowaneTloHex))
            sb.Append(FlagaSeparator).Append('F').Append(zachowaneTloHex.Trim());

        return sb.ToString();
    }

    public static string Compose(
        string? bazowy,
        string? lewaKod,
        string? prawaKod,
        bool centrum,
        ZachowaneTloInfo tlo) =>
        Compose(bazowy, lewaKod, prawaKod, centrum, tlo.JakWs, tlo.Hex);

    public static string BazowyKod(string? typWpisu) => Parse(typWpisu).Bazowy;

    public static bool MaCentrumOverlay(string? typWpisu) => Parse(typWpisu).Centrum;

    public static bool MaZachowaneTloWs(string? typWpisu) => Parse(typWpisu).ZachowajTloWs;

    public static string? ZachowaneTloHex(string? typWpisu)
    {
        var hex = Parse(typWpisu).ZachowaneTloHex;
        return string.IsNullOrWhiteSpace(hex) || RoleKeys.IsBrakWypelnienia(hex) ? null : hex.Trim();
    }

    public static ZachowaneTloInfo GetZachowaneTlo(string? typWpisu)
    {
        var p = Parse(typWpisu);
        if (p.ZachowajTloWs)
            return ZachowaneTloInfo.Ws;
        if (!string.IsNullOrWhiteSpace(p.ZachowaneTloHex) && !RoleKeys.IsBrakWypelnienia(p.ZachowaneTloHex))
            return ZachowaneTloInfo.ZHex(p.ZachowaneTloHex.Trim());
        return ZachowaneTloInfo.Brak;
    }

    public static string? PrzelaczFlage(string? typWpisu, OznaczenieGrafiku flaga)
    {
        if (flaga.FlagaPozycja == FlagaPozycjaOznaczenia.Nie)
            return flaga.Kod;

        var cur = Parse(typWpisu);
        var tlo = GetZachowaneTlo(typWpisu);
        return flaga.FlagaPozycja switch
        {
            FlagaPozycjaOznaczenia.Lewa => Compose(
                cur.Bazowy,
                string.Equals(cur.LewaKod, flaga.Kod, StringComparison.OrdinalIgnoreCase) ? null : flaga.Kod,
                cur.PrawaKod,
                cur.Centrum,
                tlo),
            FlagaPozycjaOznaczenia.Prawa => Compose(
                cur.Bazowy,
                cur.LewaKod,
                string.Equals(cur.PrawaKod, flaga.Kod, StringComparison.OrdinalIgnoreCase) ? null : flaga.Kod,
                cur.Centrum,
                tlo),
            FlagaPozycjaOznaczenia.Centrum => Compose(
                cur.Bazowy,
                cur.LewaKod,
                cur.PrawaKod,
                !cur.Centrum,
                tlo),
            _ => flaga.Kod
        };
    }

    /// <summary>
    /// Ustawia bazowy kod, zachowując flagi LEWA/PRAWA/CENTRUM.
    /// Oznaczenie bez własnego koloru nie zmienia tła — zapamiętuje aktualne wypełnienie komórki.
    /// </summary>
    public static string UstawBazowy(string? typWpisu, string nowyBazowy)
    {
        var cur = Parse(typWpisu);
        var tlo = ResolveTloPoZmianieBazy(cur, nowyBazowy);
        return Compose(nowyBazowy, cur.LewaKod, cur.PrawaKod, cur.Centrum, tlo);
    }

    /// <summary>Aktualne tło komórki wynikające z obecnego wpisu (do zachowania przy „brak koloru”).</summary>
    public static ZachowaneTloInfo CaptureAktualneTlo(string? typWpisu) =>
        CaptureAktualneTlo(Parse(typWpisu));

    private static ZachowaneTloInfo CaptureAktualneTlo(WpisFlags cur)
    {
        if (cur.ZachowajTloWs)
            return ZachowaneTloInfo.Ws;

        if (!string.IsNullOrWhiteSpace(cur.ZachowaneTloHex)
            && !RoleKeys.IsBrakWypelnienia(cur.ZachowaneTloHex))
            return ZachowaneTloInfo.ZHex(cur.ZachowaneTloHex.Trim());

        if (JestKodZTlemWs(cur.Bazowy))
            return ZachowaneTloInfo.Ws;

        var ozn = OznaczeniaLookup.FindByKod(cur.Bazowy);
        if (ozn is not null && !ozn.JestFlaga && ozn.MaWlasnyKolor)
            return ZachowaneTloInfo.ZHex(ozn.KolorHex.Trim());

        return ZachowaneTloInfo.Brak;
    }

    private static ZachowaneTloInfo ResolveTloPoZmianieBazy(WpisFlags cur, string? nowyBazowy)
    {
        var nowy = (nowyBazowy ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(nowy))
            return ZachowaneTloInfo.Brak;

        // D/WS/UWS mają własne tło — nie zapisujemy segmentu zachowania.
        if (JestKodZTlemWs(nowy))
            return ZachowaneTloInfo.Brak;

        var ozn = OznaczeniaLookup.FindByKod(nowy);
        if (ozn is not null)
        {
            if (ozn.JestFlaga)
                return CaptureAktualneTlo(cur);

            // Własny kolor → nadpisuje tło, bez segmentu zachowania.
            if (ozn.MaWlasnyKolor)
                return ZachowaneTloInfo.Brak;

            // Brak koloru → nie zmieniaj aktualnego tła komórki.
            return CaptureAktualneTlo(cur);
        }

        // Poza katalogiem: zachowaj tło (np. legacy kod bez wpisu w ustawieniach).
        return CaptureAktualneTlo(cur);
    }

    private static bool JestKodZTlemWs(string kod) =>
        kod.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase)
        || kod.Equals(Dyzur, StringComparison.OrdinalIgnoreCase)
        || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase);

    public static bool JestNieobecnoscia(string? typWpisu)
    {
        // Overlay CENTRUM (np. Oddaje) — osoba dostępna.
        if (MaCentrumOverlay(typWpisu))
            return false;

        var kod = BazowyKod(typWpisu);
        if (string.IsNullOrEmpty(kod))
            return false;

        var ozn = OznaczeniaLookup.FindByKod(kod);
        if (ozn is not null)
            return !ozn.WPracy;

        return kod.Equals(Dyzur, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopRodzicielski, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Delegacja, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Szkolenie, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Chory, StringComparison.OrdinalIgnoreCase);
    }

    public static bool JestUrlopem(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        if (kod.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopRodzicielski, StringComparison.OrdinalIgnoreCase))
            return true;

        var ozn = OznaczeniaLookup.FindByKod(kod);
        return ozn?.SekcjaRozkazu == SekcjaRozkazuGrafiku.Urlop;
    }

    public static bool JestUrlopemRodzicielskim(string? typWpisu) =>
        BazowyKod(typWpisu).Equals(UrlopRodzicielski, StringComparison.OrdinalIgnoreCase);

    public static bool MaTloWolnejSluzby(string? typWpisu)
    {
        var p = Parse(typWpisu);
        if (p.ZachowajTloWs)
            return true;

        return JestKodZTlemWs(p.Bazowy);
    }

    public static bool JestWPracy(string? typWpisu) =>
        string.IsNullOrWhiteSpace(typWpisu) || !JestNieobecnoscia(typWpisu);

    public static string TekstGlowny(string? typWpisu, bool fromUrlopPlan = false)
    {
        var kod = BazowyKod(typWpisu);
        if (string.IsNullOrEmpty(kod))
            return string.Empty;

        var ozn = OznaczeniaLookup.FindByKod(kod);
        // Flaga nigdy nie jest treścią główną — gdyby ktoś zapisał kod flagi jako bazę, ukryj.
        if (ozn?.JestFlaga == true)
            return string.Empty;

        if (ozn is not null && !string.IsNullOrEmpty(ozn.TekstWyswietlany))
            return ozn.TekstWyswietlany;

        if (JestUrlopemRodzicielskim(typWpisu))
            return UrlopRodzicielskiTekst;

        if (JestUrlopem(typWpisu) && fromUrlopPlan)
            return UrlopPlanowanyTekst;

        if (JestUrlopem(typWpisu) && kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase))
            return Urlop;

        return kod;
    }

    public static string TekstZnaczkaLewa(string? typWpisu)
    {
        var p = Parse(typWpisu);
        if (!string.IsNullOrEmpty(p.LewaKod))
            return SymbolFlagi(OznaczeniaLookup.FindByKod(p.LewaKod), p.LewaKod);

        // Legacy: sama baza = flaga LEWA.
        if (string.IsNullOrEmpty(p.PrawaKod) && !p.Centrum)
        {
            var ozn = OznaczeniaLookup.FindByKod(p.Bazowy);
            if (ozn?.FlagaPozycja == FlagaPozycjaOznaczenia.Lewa)
                return SymbolFlagi(ozn, p.Bazowy);
        }

        return string.Empty;
    }

    public static string TekstZnaczkaPrawa(string? typWpisu)
    {
        var p = Parse(typWpisu);
        if (!string.IsNullOrEmpty(p.PrawaKod))
            return SymbolFlagi(OznaczeniaLookup.FindByKod(p.PrawaKod), p.PrawaKod);

        if (string.IsNullOrEmpty(p.LewaKod) && !p.Centrum)
        {
            var ozn = OznaczeniaLookup.FindByKod(p.Bazowy);
            if (ozn?.FlagaPozycja == FlagaPozycjaOznaczenia.Prawa)
                return SymbolFlagi(ozn, p.Bazowy);
        }

        return string.Empty;
    }

    public static string TekstWyswietlany(string? typWpisu, bool fromUrlopPlan = false) =>
        string.Concat(TekstZnaczkaLewa(typWpisu), TekstGlowny(typWpisu, fromUrlopPlan), TekstZnaczkaPrawa(typWpisu));

    public static StylWyswietlaniaOznaczenia ResolveStyl(string? typWpisu)
    {
        if (MaCentrumOverlay(typWpisu))
        {
            var centrum = OznaczeniaLookup.FindByFlaga(FlagaPozycjaOznaczenia.Centrum);
            return centrum?.StylWyswietlania ?? StylWyswietlaniaOznaczenia.Przekreslenie;
        }

        var ozn = OznaczeniaLookup.FindByKod(BazowyKod(typWpisu));
        if (ozn is null || ozn.JestFlaga)
            return StylWyswietlaniaOznaczenia.Normalny;
        return ozn.StylWyswietlania;
    }

    private static string SymbolFlagi(OznaczenieGrafiku? ozn, string fallbackKod)
    {
        if (ozn is not null && !string.IsNullOrEmpty(ozn.TekstWyswietlany))
            return ozn.TekstWyswietlany;
        if (ozn is not null)
            return ozn.Kod;
        return fallbackKod;
    }
}
