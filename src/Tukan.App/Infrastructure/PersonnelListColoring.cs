using System.Windows.Media;
using BOBER.Core.Constants;
using BOBER.Core.Models;
using Chomik.App.Helpers;
using ChomikFunkcjonariusz = Chomik.Core.Models.Funkcjonariusz;
using BoberServices = BOBER.Services.AppServices;

namespace Tukan.App.Infrastructure;

/// <summary>
/// Kolorowanie listy Edycji personelu — te same wartości kolorów co grafik,
/// niezależnie od checkboxów Aktywny (te dotyczą grafiku miesięcznego).
/// </summary>
public sealed class PersonnelListColoring(BoberServices bober) : IPersonnelListColoring
{
    private bool _enabled;
    private IReadOnlyDictionary<string, KolorStanowiska> _kolory =
        new Dictionary<string, KolorStanowiska>(StringComparer.OrdinalIgnoreCase);

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _enabled = await bober.Settings.GetKolorowanieEdycjaPersoneluAsync(cancellationToken);
        var all = await bober.Kolory.GetAllAsync(cancellationToken);
        _kolory = KoloryLookup.Index(all);
    }

    public bool TryGetAppearance(ChomikFunkcjonariusz funkcjonariusz, out Brush background, out Brush nameBorder)
    {
        background = Brushes.Transparent;
        nameBorder = Brushes.Transparent;
        if (!_enabled)
            return false;

        var role = ClassifyRole(funkcjonariusz);
        background = BrushFromHex(GetHex(role, RoleKeys.DomyslneKolory), Colors.White);

        if (IsNurek(funkcjonariusz))
            nameBorder = BrushFromHex(
                GetHex(RoleKeys.NurekCzcionka, RoleKeys.DomyslneKoloryWpisow),
                Color.FromRgb(0xF8, 0x08, 0x08));

        return true;
    }

    private string GetHex(string klucz, IReadOnlyDictionary<string, string> defaults)
    {
        if (_kolory.TryGetValue(klucz, out var kolor) && !string.IsNullOrWhiteSpace(kolor.KolorHex))
            return kolor.KolorHex;

        return defaults.TryGetValue(klucz, out var hex)
            ? hex
            : RoleKeys.GetDefaultKolorHex(klucz);
    }

    private static string ClassifyRole(ChomikFunkcjonariusz f)
    {
        if (MatchesStanowisko(f, "dowódca zmiany", "dowodca zmiany"))
            return RoleKeys.DowodcaZmiany;
        if (MatchesStanowisko(f, "dowódca sekcji", "dowodca sekcji",
                "zastępca dowódcy zmiany", "zastepca dowodcy zmiany"))
            return RoleKeys.DowodcaSekcji;
        if (MatchesStanowisko(f, "dowódca zastępu", "dowodca zastepu"))
            return RoleKeys.DowodcaZastepu;
        if (IsKierowca(f))
            return RoleKeys.Kierowca;
        return RoleKeys.Zwykly;
    }

    private static bool IsNurek(ChomikFunkcjonariusz f) =>
        f.Uprawnienia.Any(u =>
            ContainsInsensitive(u.Nazwa, "nurek")
            || ContainsInsensitive(u.Nazwa, "kierownik prac podwodnych")
            || ContainsInsensitive(u.Nazwa, "kpp"));

    private static bool IsKierowca(ChomikFunkcjonariusz f)
    {
        if (MatchesStanowisko(f, "kierowca"))
            return true;

        return f.Uprawnienia.Any(u =>
            ContainsInsensitive(u.Nazwa, "prawo jazdy")
            && (ContainsInsensitive(u.Podtyp, "kat. c")
                || ContainsInsensitive(u.Podtyp, "c+e")
                || ContainsInsensitive(u.Podtyp, "kat. d")));
    }

    private static bool MatchesStanowisko(ChomikFunkcjonariusz f, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(f.Stanowisko))
            return false;

        var lower = f.Stanowisko.ToLowerInvariant();
        return needles.Any(n => lower.Contains(n, StringComparison.Ordinal));
    }

    private static bool ContainsInsensitive(string? value, string needle) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static SolidColorBrush BrushFromHex(string hex, Color fallback)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(fallback);
        }
    }
}
