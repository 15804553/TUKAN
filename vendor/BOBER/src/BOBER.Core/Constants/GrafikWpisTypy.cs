namespace BOBER.Core.Constants;

/// <summary>
/// Kody wpisów w komórkach grafiku i reguły ich interpretacji przy podsumowaniach.
/// Oddaje (O) — nakładka „/” na WS/D/U/UWS (obecność).
/// Kropka (.) — chętna oddać, tylko z D, U, UWS lub WS (nie wpływa na stan etatowy).
/// Pytajnik (?) — potrzebuje wolne, tylko gdy osoba jest w pracy (nie wpływa na stan).
/// UWS — urlop z wolną służbą (żółte tło jak WS, napis „U”).
/// </summary>
public static class GrafikWpisTypy
{
    public const string Dyzur = "D";
    public const string WolnaSluzba = "WS";
    public const string Urlop = "U";
    /// <summary>Urlop z wolną służbą — tło WS, tekst „U”; w rozkazie → WOLNA SŁUŻBA.</summary>
    public const string UrlopZWolnaSluzba = "UWS";
    public const string Delegacja = "Del";
    public const string Szkolenie = "S";
    public const string Chory = "C";
    public const string PotrzebujeWolne = "?";

    /// <summary>Sufiks Oddaje — bazowy kod (WS/D/U/UWS) zostaje pod spodem.</summary>
    public const char OddalSufiks = '/';

    /// <summary>Sufiks „chętna oddać” — tylko z D, U, UWS lub WS.</summary>
    public const char KropkaSufiks = '.';

    /// <summary>
    /// Sufiks zachowania żółtego tła WS przy Del/S z „brakiem koloru” (np. „Del*”).
    /// Nie jest wyświetlany w UI; tylko w typie zapisanym w DB / komórce.
    /// </summary>
    public const char ZachowajTloWsSufiks = '*';

    /// <summary>Znak wizualny długiej pauzy (Oddaje) w komórce.</summary>
    public const string OddalZnak = "\u2014"; // —

    /// <summary>
    /// Czy wpis oznacza nieobecność w składzie. Pusta komórka / ? / Oddaje = w pracy.
    /// Kropka nie zmienia statusu obecności.
    /// </summary>
    public static bool JestNieobecnoscia(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return false;

        if (MaPytajnik(typWpisu))
            return false;

        if (MaOddal(typWpisu) && MoznaOddac(typWpisu))
            return false;

        var kod = BazowyKod(typWpisu);
        return kod.Equals(Dyzur, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Delegacja, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Szkolenie, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Chory, StringComparison.OrdinalIgnoreCase)
            || kod.Equals("DEL", StringComparison.OrdinalIgnoreCase)
            || kod.Equals("DD", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Czy kod bazowy (WS, D, U, UWS) można oddać klawiszem O.</summary>
    public static bool MoznaOddac(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        return kod.Equals(Dyzur, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>U lub UWS — osoba na urlopie w grafiku.</summary>
    public static bool JestUrlopem(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        return kod.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Czy komórka ma żółte tło wolnej służby (WS, D, UWS). Del/S: własne kolory lub zachowane tło WS (*).</summary>
    public static bool MaTloWolnejSluzby(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        return kod.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Dyzur, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// U+W → UWS, W na UWS → U, U na WS → UWS; w pozostałych przypadkach zwraca <paramref name="nowyTyp"/>.
    /// </summary>
    public static string ResolvePoNalozeniu(string? aktualnyTyp, string nowyTyp)
    {
        var bazowy = BazowyKod(aktualnyTyp);
        var nowy = (nowyTyp ?? string.Empty).Trim();

        if (nowy.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase))
        {
            if (bazowy.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase))
                return Urlop;
            if (bazowy.Equals(Urlop, StringComparison.OrdinalIgnoreCase))
                return UrlopZWolnaSluzba;
        }

        if (nowy.Equals(Urlop, StringComparison.OrdinalIgnoreCase)
            && bazowy.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase))
            return UrlopZWolnaSluzba;

        return nowy;
    }

    /// <summary>S, C lub Del — nie podlegają oddaniu; UI pokazuje komunikat.</summary>
    public static bool NieMoznaOddacBoZakazanyTyp(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        return kod.Equals(Szkolenie, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Chory, StringComparison.OrdinalIgnoreCase)
            || kod.Equals(Delegacja, StringComparison.OrdinalIgnoreCase)
            || kod.Equals("DEL", StringComparison.OrdinalIgnoreCase);
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

    public static bool MaPytajnik(string? typWpisu)
    {
        var kod = BazowyKod(typWpisu);
        return kod == PotrzebujeWolne;
    }

    /// <summary>Czy komórka oznacza osobę w pracy (pusta lub tylko ?).</summary>
    public static bool JestWPracy(string? typWpisu) =>
        string.IsNullOrWhiteSpace(typWpisu) || MaPytajnik(typWpisu);

    /// <summary>Czy typ ma zachować żółte tło WS (sufiks *).</summary>
    public static bool MaZachowaneTloWs(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return false;

        var t = typWpisu.Trim();
        return t.Length > 0 && t[^1] == ZachowajTloWsSufiks;
    }

    /// <summary>Usuwa sufiks zachowania tła WS (np. „Del*” → „Del”).</summary>
    public static string UsunSufiksZachowanegoTla(string? typWpisu)
    {
        if (string.IsNullOrWhiteSpace(typWpisu))
            return string.Empty;

        var t = typWpisu.Trim();
        if (t.Length > 0 && t[^1] == ZachowajTloWsSufiks)
            t = t[..^1];

        return t;
    }

    /// <summary>Kod bez sufiksów Oddaje, kropki i zachowania tła (np. „U.” → „U”, „Del*” → „Del”).</summary>
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

    /// <summary>Typ do zapisu z zachowaniem tła WS (np. „Del” → „Del*”).</summary>
    public static string ZZachowanymTlemWs(string typWpisu) =>
        UsunSufiksZachowanegoTla(typWpisu) + ZachowajTloWsSufiks;

    /// <summary>Czy bazowy kod to Del lub S.</summary>
    public static bool JestDelLubS(string? typWpisu)
    {
        var b = BazowyKod(typWpisu);
        return b.Equals(Delegacja, StringComparison.OrdinalIgnoreCase)
            || b.Equals(Szkolenie, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Del/S przy zapisie: zachowaj żółte tło WS (sufiks *), gdy poprzednio było WS/D/UWS
    /// albo Del/S z już zachowanym tłem. Przy „braku koloru” pusta służba → bez żółtego.
    /// </summary>
    public static string ResolveDelSDlaZapisu(string? poprzedniTyp, string nowyTyp)
    {
        var czysty = UsunSufiksZachowanegoTla(nowyTyp);
        if (!JestDelLubS(czysty))
            return czysty;

        var zachowaj = MaTloWolnejSluzby(poprzedniTyp)
            || (JestDelLubS(poprzedniTyp) && MaZachowaneTloWs(poprzedniTyp));

        return zachowaj ? ZZachowanymTlemWs(czysty) : czysty;
    }

    /// <summary>Dodaje lub usuwa Oddaje. Zwraca null, gdy nie wolno.</summary>
    public static string? PrzelaczOddal(string? typWpisu)
    {
        if (!MoznaOddac(typWpisu))
            return null;

        var bazowy = BazowyKod(typWpisu);
        // Oddaje i kropka się wykluczają — przy Oddaje zdejmujemy kropkę.
        return MaOddal(typWpisu) ? bazowy : bazowy + OddalSufiks;
    }

    /// <summary>Dodaje lub usuwa kropkę (tylko D / U / UWS / WS). Zwraca null, gdy nie wolno.</summary>
    public static string? PrzelaczKropke(string? typWpisu)
    {
        if (!MoznaOddac(typWpisu))
            return null;

        var bazowy = BazowyKod(typWpisu);
        if (MaKropke(typWpisu))
            return bazowy; // zdejmij kropkę i ewentualne Oddaje

        return bazowy + KropkaSufiks;
    }

    /// <summary>Dodaje lub usuwa „?” — tylko gdy osoba jest w pracy. Zwraca null, gdy nie wolno.</summary>
    public static string? PrzelaczPytajnik(string? typWpisu)
    {
        if (MaPytajnik(typWpisu))
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(typWpisu))
            return null;

        return PotrzebujeWolne;
    }

    /// <summary>Tekst główny komórki (bez kropki/? — te rysujemy mniejszym znakiem).</summary>
    public static string TekstGlowny(string? typWpisu)
    {
        if (MaPytajnik(typWpisu))
            return string.Empty;

        var bazowy = BazowyKod(typWpisu);
        var jestWs = bazowy.Equals(WolnaSluzba, StringComparison.OrdinalIgnoreCase);

        if (jestWs)
            return MaOddal(typWpisu) ? OddalZnak : string.Empty;

        // UWS — żółte tło jak WS, ale napis „U”
        if (bazowy.Equals(UrlopZWolnaSluzba, StringComparison.OrdinalIgnoreCase))
            return Urlop;

        if (string.IsNullOrEmpty(bazowy))
            return string.Empty;

        return bazowy;
    }

    /// <summary>Mniejszy znaczek w komórce: „•” (chętna oddać) lub „?”.</summary>
    public static string TekstZnaczka(string? typWpisu)
    {
        if (MaPytajnik(typWpisu))
            return PotrzebujeWolne;
        if (MaKropke(typWpisu))
            return "\u2022"; // • — grubsza kropka niż „.”
        return string.Empty;
    }

    /// <summary>Tekst do eksportu Excel (główny + znaczek). Oddaje przy U/D → przekreślenie; przy WS → „—”.</summary>
    public static string TekstWyswietlany(string? typWpisu)
    {
        var glowny = TekstGlowny(typWpisu);
        var znaczek = TekstZnaczka(typWpisu);

        if (string.IsNullOrEmpty(znaczek))
            return glowny;

        return string.IsNullOrEmpty(glowny) ? znaczek : glowny + znaczek;
    }

    private static string BezKropki(string trimmed)
    {
        return trimmed.Length > 0 && trimmed[^1] == KropkaSufiks
            ? trimmed[..^1]
            : trimmed;
    }
}
