using BOBER.Core.Enums;
using BOBER.Core.Models;
using BOBER.Core.Oznaczenia;

namespace BOBER.Core.Constants;

/// <summary>
/// Kody wpisów w komórkach grafiku i reguły ich interpretacji przy podsumowaniach.
/// Oddaje (O) — nakładka „/” (obecność). Kropka (.) — chętna oddać.
/// Gdy załadowany <see cref="OznaczeniaLookup"/> — reguły biorą z katalogu oznaczeń.
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

    public const char OddalSufiks = '/';
    public const char KropkaSufiks = '.';
    public const char ZachowajTloWsSufiks = '*';
    public const string OddalZnak = "\u2014";

    public static bool JestNieobecnoscia(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return false;

        if (MaPytajnik(typWpisu))
            return false;

        if (MaOddal(typWpisu) && MoznaOddac(typWpisu))
            return false;

        var kod = BazowyKod(typWpisu);
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
            || kod.Equals(Chory, StringComparison.OrdinalIgnoreCase)
            || kod.Equals("DEL", StringComparison.OrdinalIgnoreCase)
            || kod.Equals("DD", StringComparison.OrdinalIgnoreCase);
    }

    public static bool MoznaOddac(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        var ozn = OznaczeniaLookup.FindByKod(kod);
        if (ozn is not null)
            return ozn.MoznaOddac;

        return kod.Equals(Dyzur, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopRodzicielski, StringComparison.OrdinalIgnoreCase);
    }

    public static bool JestUrlopem(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        var ozn = OznaczeniaLookup.FindByKod(kod);
        if (ozn is not null)
        {
            return ozn.RolaNalozania is RolaNalozaniaOznaczenia.Urlop
                or RolaNalozaniaOznaczenia.UrlopZWolnaSluzba
                || ozn.SekcjaRozkazu == SekcjaRozkazuGrafiku.Urlop;
        }

        return kod.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopRodzicielski, StringComparison.OrdinalIgnoreCase);
    }

    public static bool JestUrlopemRodzicielskim(string? typWpisu) =>
        BazowyKod(typWpisu).Equals(UrlopRodzicielski, StringComparison.OrdinalIgnoreCase);

    public static bool MaTloWolnejSluzby(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        var ozn = OznaczeniaLookup.FindByKod(kod);
        if (ozn is not null)
        {
            return ozn.RolaNalozania is RolaNalozaniaOznaczenia.WolnaSluzba
                or RolaNalozaniaOznaczenia.UrlopZWolnaSluzba
                || kod.Equals(Dyzur, StringComparison.OrdinalIgnoreCase);
        }

        return kod.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Dyzur, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolvePoNalozeniu(string? aktualnyTyp, string nowyTyp)
    {
        var bazowy = BazowyKod(aktualnyTyp);
        var nowy = (nowyTyp ?? string.Empty).Trim();

        if (OznaczeniaLookup.HasItems)
        {
            var aktualne = OznaczeniaLookup.FindByKod(bazowy);
            var nowe = OznaczeniaLookup.FindByKod(nowy);
            var uws = OznaczeniaLookup.FindByRola(RolaNalozaniaOznaczenia.UrlopZWolnaSluzba);
            var urlop = OznaczeniaLookup.FindByRola(RolaNalozaniaOznaczenia.Urlop);

            if (nowe?.RolaNalozania == RolaNalozaniaOznaczenia.WolnaSluzba && uws is not null)
            {
                if (aktualne?.RolaNalozania == RolaNalozaniaOznaczenia.UrlopZWolnaSluzba && urlop is not null)
                    return urlop.Kod;
                if (aktualne?.RolaNalozania == RolaNalozaniaOznaczenia.Urlop)
                    return uws.Kod;
            }

            if (nowe?.RolaNalozania == RolaNalozaniaOznaczenia.Urlop
                && aktualne?.RolaNalozania == RolaNalozaniaOznaczenia.WolnaSluzba
                && uws is not null)
                return uws.Kod;

            return nowy;
        }

        if (nowy.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase))
        {
            if (bazowy.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase))
                return Urlop;
            if (bazowy.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
                || bazowy.Equals(UrlopRodzicielski, StringComparison.OrdinalIgnoreCase))
                return UrlopZWolnaSluzba;
        }

        if ((nowy.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
                || nowy.Equals(UrlopRodzicielski, StringComparison.OrdinalIgnoreCase))
            && bazowy.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase))
            return UrlopZWolnaSluzba;

        return nowy;
    }

    public static bool NieMoznaOddacBoZakazanyTyp(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return false;
        if (MoznaOddac(typWpisu))
            return false;
        return !string.IsNullOrEmpty(BazowyKod(typWpisu));
    }

    public static bool MaOddal(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return false;

        var bezKropki = BezKropki(typWpisu.Trim());
        return bezKropki.Length > 1 && bezKropki[^1] == OddalSufiks;
    }

    public static bool MaKropke(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return false;

        var trimmed = typWpisu.Trim();
        if (MaOddal(trimmed))
            trimmed = trimmed[..^1];

        return trimmed.Length > 0 && trimmed[^1] == KropkaSufiks;
    }

    public static bool MaPytajnik(string? typWpisu) =>
        BazowyKod(typWpisu) == PotrzebujeWolne;

    public static bool JestWPracy(string? typWpisu) =>
        string.IsNullOrWhiteSpace(typWpisu) || MaPytajnik(typWpisu) || !JestNieobecnoscia(typWpisu);

    public static bool MaZachowaneTloWs(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return false;

        var t = typWpisu.Trim();
        return t.Length > 0 && t[^1] == ZachowajTloWsSufiks;
    }

    public static string UsunSufiksZachowanegoTla(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return string.Empty;

        var t = typWpisu.Trim();
        if (t.Length > 0 && t[^1] == ZachowajTloWsSufiks)
            t = t[..^1];

        return t;
    }

    public static string BazowyKod(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return string.Empty;

        var trimmed = typWpisu.Trim();
        if (trimmed.Length > 1 && trimmed[^1] == OddalSufiks)
            trimmed = trimmed[..^1];
        if (trimmed.Length > 0 && trimmed[^1] == KropkaSufiks)
            trimmed = trimmed[..^1];
        if (trimmed.Length > 0 && trimmed[^1] == ZachowajTloWsSufiks)
            trimmed = trimmed[..^1];

        return trimmed;
    }

    public static string ZZachowanymTlemWs(string typWpisu) =>
        UsunSufiksZachowanegoTla(typWpisu) + ZachowajTloWsSufiks;

    public static bool ZachowujeTloWsPrzyBraku(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        var ozn = OznaczeniaLookup.FindByKod(kod);
        if (ozn is not null)
            return ozn.ZachowajTloWsPrzyBraku;

        return JestDelLubS(kod);
    }

    public static bool JestDelLubS(string? typWpisu)
    {
        var b = BazowyKod(typWpisu);
        return b.Equals(Delegacja, StringComparison.OrdinalIgnoreCase)
            || b.Equals(Szkolenie, StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveDelSDlaZapisu(string? poprzedniTyp, string nowyTyp)
    {
        var czysty = UsunSufiksZachowanegoTla(nowyTyp);
        if (!ZachowujeTloWsPrzyBraku(czysty))
            return czysty;

        var zachowaj = MaTloWolnejSluzby(poprzedniTyp)
            || (ZachowujeTloWsPrzyBraku(poprzedniTyp) && MaZachowaneTloWs(poprzedniTyp));

        return zachowaj ? ZZachowanymTlemWs(czysty) : czysty;
    }

    public static string? PrzelaczOddal(string? typWpisu)
    {
        if (!MoznaOddac(typWpisu))
            return null;

        var bazowy = BazowyKod(typWpisu);
        return MaOddal(typWpisu) ? bazowy : bazowy + OddalSufiks;
    }

    public static string? PrzelaczKropke(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        var ozn = OznaczeniaLookup.FindByKod(kod);
        var wolno = ozn?.MoznaKropke ?? MoznaOddac(typWpisu);
        if (!wolno)
            return null;

        var bazowy = BazowyKod(typWpisu);
        if (MaKropke(typWpisu))
            return bazowy;

        return bazowy + KropkaSufiks;
    }

    public static string? PrzelaczPytajnik(string? typWpisu)
    {
        if (MaPytajnik(typWpisu))
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(typWpisu))
            return null;

        return PotrzebujeWolne;
    }

    public static string TekstGlowny(string? typWpisu, bool fromUrlopPlan = false)
    {
        if (MaPytajnik(typWpisu))
            return string.Empty;

        var bazowy = BazowyKod(typWpisu);
        var ozn = OznaczeniaLookup.FindByKod(bazowy);

        // Flaga LEWA/PRAWA/CENTRUM — symbol nie jest tekstem głównym ( CENTRUM = nakładka Oddaje ).
        if (ozn is not null && ozn.FlagaPozycja != Enums.FlagaPozycjaOznaczenia.Nie)
            return string.Empty;

        if (ozn is not null)
        {
            if (JestUrlopemRodzicielskim(typWpisu))
                return UrlopRodzicielskiTekst;

            if (JestUrlopem(typWpisu) && fromUrlopPlan && !JestUrlopemRodzicielskim(typWpisu))
                return UrlopPlanowanyTekst;

            if (!string.IsNullOrEmpty(ozn.TekstWyswietlany))
                return ozn.TekstWyswietlany;

            return bazowy;
        }

        if (bazowy.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase))
            return WolnaSluzba;

        if (JestUrlopemRodzicielskim(typWpisu))
            return UrlopRodzicielskiTekst;

        if (JestUrlopem(typWpisu))
            return fromUrlopPlan ? UrlopPlanowanyTekst : Urlop;

        return string.IsNullOrEmpty(bazowy) ? string.Empty : bazowy;
    }

    public static string TekstZnaczka(string? typWpisu) => TekstZnaczkaPrawa(typWpisu);

    public static string TekstZnaczkaLewa(string? typWpisu)
    {
        if (MaKropke(typWpisu))
        {
            var chce = OznaczeniaLookup.FindChceOddac();
            if (chce?.FlagaPozycja == Enums.FlagaPozycjaOznaczenia.Lewa)
                return SymbolZnaczka(chce);
        }

        var ozn = OznaczeniaLookup.FindByKod(BazowyKod(typWpisu));
        if (ozn?.FlagaPozycja != Enums.FlagaPozycjaOznaczenia.Lewa)
            return string.Empty;

        return SymbolZnaczka(ozn);
    }

    public static string TekstZnaczkaPrawa(string? typWpisu)
    {
        if (MaKropke(typWpisu))
        {
            var chce = OznaczeniaLookup.FindChceOddac();
            if (chce is null)
                return "\u2022";
            if (chce.FlagaPozycja is Enums.FlagaPozycjaOznaczenia.Lewa)
                return string.Empty;
            return SymbolZnaczka(chce);
        }

        var ozn = OznaczeniaLookup.FindByKod(BazowyKod(typWpisu));
        if (ozn?.FlagaPozycja == Enums.FlagaPozycjaOznaczenia.Prawa)
            return SymbolZnaczka(ozn);

        if (MaPytajnik(typWpisu))
            return PotrzebujeWolne;

        return string.Empty;
    }

    private static string SymbolZnaczka(Models.OznaczenieGrafiku? ozn)
    {
        if (ozn is null)
            return string.Empty;
        if (!string.IsNullOrEmpty(ozn.TekstWyswietlany))
            return ozn.TekstWyswietlany;
        return ozn.Kod;
    }

    public static string TekstWyswietlany(string? typWpisu, bool fromUrlopPlan = false)
    {
        var glowny = TekstGlowny(typWpisu, fromUrlopPlan);
        var lewy = TekstZnaczkaLewa(typWpisu);
        var prawy = TekstZnaczkaPrawa(typWpisu);
        return string.Concat(lewy, glowny, prawy);
    }

    private static string BezKropki(string trimmed) =>
        trimmed.Length > 0 && trimmed[^1] == KropkaSufiks
            ? trimmed[..^1]
            : trimmed;
}
