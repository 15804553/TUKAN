using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Tukan.App.Models;

namespace Tukan.App.Infrastructure;

/// <summary>
/// Jednolita paleta kolorystyczna dla całej aplikacji TUKAN (CHOMIK, BOBER, SKRYBEK, shell).
/// </summary>
public enum TukanAppThemeKind
{
    TukanKlasyczny,
    ChomikFiolet,
    BoberPiasek,
    SkrybekGrafit,
    StrazPozarna,
    GranatSluzbowy
}

public static class TukanAppTheme
{
    public static TukanAppThemeKind DefaultKind => TukanAppThemeKind.TukanKlasyczny;

    public static TukanAppThemeKind Active { get; private set; } = DefaultKind;

    public static IReadOnlyList<TukanUiPaletteOption> PaletteOptions { get; } =
    [
        new(TukanAppThemeKind.TukanKlasyczny,
            "TUKAN klasyczny",
            "Granatowy panel boczny z pomarańczowym akcentem i jasnym tłem treści."),
        new(TukanAppThemeKind.ChomikFiolet,
            "CHOMIK fiolet",
            "Jednolita paleta fioletowa modułu personelu — spokojny, jasny wygląd."),
        new(TukanAppThemeKind.BoberPiasek,
            "BOBER piasek",
            "Ciepłe, piaskowe tony inspirowane grafikiem rocznym."),
        new(TukanAppThemeKind.SkrybekGrafit,
            "SKRYBEK grafit",
            "Ciemny motyw grafitowy — mniej zmęczenia oczu przy długiej pracy."),
        new(TukanAppThemeKind.StrazPozarna,
            "Straż pożarna",
            "Czerwono-bordowa paleta służbowa PSP."),
        new(TukanAppThemeKind.GranatSluzbowy,
            "Granat służbowy",
            "Profesjonalny granat z niebieskim akcentem.")
    ];

    public static TukanAppThemeKind Parse(string? value) =>
        Parse(value, TukanAppThemeKind.TukanKlasyczny);

    public static TukanAppThemeKind Parse(string? value, TukanAppThemeKind fallback) =>
        Enum.TryParse<TukanAppThemeKind>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

    public static void Apply(TukanAppThemeKind kind)
    {
        Active = kind;

        if (Application.Current?.Resources is not ResourceDictionary root)
        {
            return;
        }

        ApplyPalette(root, GetPalette(kind));
    }

    public static IReadOnlyList<Color> GetPreviewColors(TukanAppThemeKind kind)
    {
        var palette = GetPalette(kind);
        return
        [
            palette.Sidebar,
            palette.SidebarAccent,
            palette.Surface,
            palette.Primary
        ];
    }

    public static TukanUiPaletteOption GetOption(TukanAppThemeKind kind) =>
        PaletteOptions.First(option => option.Kind == kind);

    private static UnifiedPalette GetPalette(TukanAppThemeKind kind) => kind switch
    {
        TukanAppThemeKind.ChomikFiolet => new UnifiedPalette(
            Primary: C("#474073"), PrimaryDark: C("#383460"), PrimaryLight: C("#5E5789"),
            Accent: C("#B8AED8"), Background: C("#F2F1F7"), Surface: C("#F2F1F7"),
            SurfaceVariant: C("#E8E4F0"), Card: C("#FFFFFF"), Border: C("#D5D0E6"),
            Text: C("#2A2640"), MutedText: C("#6B6488"), SidebarMuted: C("#D8D2F0"),
            SidebarBackground: C("#474073"), AccentLight: C("#C8BFE0"),
            TitleBar: C("#5E5789"), ButtonHover: C("#5E5789"), ButtonClose: C("#C42B1C"),
            ControlHoverBorder: C("#B8AED8"), ControlHoverFill: C("#E8E4F0"),
            Sidebar: C("#474073"), SidebarAccent: C("#B8AED8"), SidebarText: C("#FFFFFF"),
            SidebarMutedText: C("#D8D2F0"), SidebarHover: C("#5E5789")),

        TukanAppThemeKind.BoberPiasek => new UnifiedPalette(
            Primary: C("#8B7D56"), PrimaryDark: C("#6E6244"), PrimaryLight: C("#A89868"),
            Accent: C("#8B7D56"), Background: C("#D9CFA8"), Surface: C("#D9CFA8"),
            SurfaceVariant: C("#C2B280"), Card: C("#E8DFC0"), Border: C("#A89868"),
            Text: C("#2C2818"), MutedText: C("#5C5538"), SidebarMuted: C("#8B7D56"),
            SidebarBackground: C("#8B7D56"), AccentLight: C("#B8A870"),
            TitleBar: C("#B8AA78"), ButtonHover: C("#A08850"), ButtonClose: C("#C42B1C"),
            ControlHoverBorder: C("#B8A870"), ControlHoverFill: C("#CFC59A"),
            Sidebar: C("#6E6244"), SidebarAccent: C("#E8DFC0"), SidebarText: C("#F5F0E1"),
            SidebarMutedText: C("#CFC59A"), SidebarHover: C("#8B7D56")),

        TukanAppThemeKind.SkrybekGrafit => new UnifiedPalette(
            Primary: C("#5A7A9A"), PrimaryDark: C("#3A5A7A"), PrimaryLight: C("#7AAAC8"),
            Accent: C("#5A7A9A"), Background: C("#1E1E1E"), Surface: C("#1E1E1E"),
            SurfaceVariant: C("#383838"), Card: C("#2D2D2D"), Border: C("#4A4A4A"),
            Text: C("#E0E0E0"), MutedText: C("#9E9E9E"), SidebarMuted: C("#9E9E9E"),
            SidebarBackground: C("#252525"), AccentLight: C("#7AAAC8"),
            TitleBar: C("#252525"), ButtonHover: C("#3A3A3A"), ButtonClose: C("#C42B1C"),
            ControlHoverBorder: C("#7AAAC8"), ControlHoverFill: C("#333333"),
            Sidebar: C("#1E1E1E"), SidebarAccent: C("#7AAAC8"), SidebarText: C("#E0E0E0"),
            SidebarMutedText: C("#9E9E9E"), SidebarHover: C("#383838")),

        TukanAppThemeKind.StrazPozarna => new UnifiedPalette(
            Primary: C("#991B1B"), PrimaryDark: C("#7F1D1D"), PrimaryLight: C("#B91C1C"),
            Accent: C("#DC2626"), Background: C("#FAFAF9"), Surface: C("#FAFAF9"),
            SurfaceVariant: C("#F5F5F4"), Card: C("#FFFFFF"), Border: C("#D6D3D1"),
            Text: C("#1C1917"), MutedText: C("#78716C"), SidebarMuted: C("#FECACA"),
            SidebarBackground: C("#991B1B"), AccentLight: C("#EF4444"),
            TitleBar: C("#B91C1C"), ButtonHover: C("#B91C1C"), ButtonClose: C("#C42B1C"),
            ControlHoverBorder: C("#EF4444"), ControlHoverFill: C("#FEE2E2"),
            Sidebar: C("#7F1D1D"), SidebarAccent: C("#FCA5A5"), SidebarText: C("#FFFFFF"),
            SidebarMutedText: C("#FECACA"), SidebarHover: C("#991B1B")),

        TukanAppThemeKind.GranatSluzbowy => new UnifiedPalette(
            Primary: C("#1A3A5C"), PrimaryDark: C("#142D47"), PrimaryLight: C("#254A70"),
            Accent: C("#2980B9"), Background: C("#EEF2F6"), Surface: C("#EEF2F6"),
            SurfaceVariant: C("#DDE4EC"), Card: C("#FFFFFF"), Border: C("#B8C5D4"),
            Text: C("#1A2332"), MutedText: C("#5A6B7D"), SidebarMuted: C("#A8BDD4"),
            SidebarBackground: C("#1A3A5C"), AccentLight: C("#3498DB"),
            TitleBar: C("#254A70"), ButtonHover: C("#254A70"), ButtonClose: C("#C42B1C"),
            ControlHoverBorder: C("#3498DB"), ControlHoverFill: C("#DDE4EC"),
            Sidebar: C("#142D47"), SidebarAccent: C("#5DADE2"), SidebarText: C("#ECF0F1"),
            SidebarMutedText: C("#A8BDD4"), SidebarHover: C("#1A3A5C")),

        _ => new UnifiedPalette(
            Primary: C("#1E2A38"), PrimaryDark: C("#15202B"), PrimaryLight: C("#2C3E50"),
            Accent: C("#E67E22"), Background: C("#F2F4F5"), Surface: C("#F2F4F5"),
            SurfaceVariant: C("#E8ECED"), Card: C("#FFFFFF"), Border: C("#D5D8DC"),
            Text: C("#2C3E50"), MutedText: C("#7F8C8D"), SidebarMuted: C("#95A5A6"),
            SidebarBackground: C("#1E2A38"), AccentLight: C("#F39C12"),
            TitleBar: C("#2C3E50"), ButtonHover: C("#2C3E50"), ButtonClose: C("#C42B1C"),
            ControlHoverBorder: C("#E67E22"), ControlHoverFill: C("#E8ECED"),
            Sidebar: C("#1E2A38"), SidebarAccent: C("#E67E22"), SidebarText: C("#ECF0F1"),
            SidebarMutedText: C("#95A5A6"), SidebarHover: C("#2C3E50"))
    };

    private static void ApplyPalette(ResourceDictionary root, UnifiedPalette palette)
    {
        SetColor(root, "PrimaryColor", palette.Primary);
        SetColor(root, "PrimaryDarkColor", palette.PrimaryDark);
        SetColor(root, "PrimaryLightColor", palette.PrimaryLight);
        SetColor(root, "AccentColor", palette.Accent);
        SetColor(root, "SurfaceColor", palette.Surface);
        SetColor(root, "CardColor", palette.Card);
        SetColor(root, "TextColor", palette.Text);
        SetColor(root, "MutedTextColor", palette.MutedText);
        SetColor(root, "SidebarMutedColor", palette.SidebarMuted);
        SetColor(root, "SidebarBackgroundColor", palette.SidebarBackground);

        SetColor(root, "BackgroundColor", palette.Background);
        SetColor(root, "SurfaceVariantColor", palette.SurfaceVariant);
        SetColor(root, "BorderColor", palette.Border);
        SetColor(root, "AccentLightColor", palette.AccentLight);
        SetColor(root, "ForegroundColor", palette.Text);
        SetColor(root, "ForegroundMutedColor", palette.MutedText);
        SetColor(root, "TitleBarColor", palette.TitleBar);
        SetColor(root, "ButtonHoverColor", palette.ButtonHover);
        SetColor(root, "ButtonCloseColor", palette.ButtonClose);
        SetColor(root, "ControlHoverBorderColor", palette.ControlHoverBorder);
        SetColor(root, "ControlHoverFillColor", palette.ControlHoverFill);

        SetBrush(root, "PrimaryBrush", palette.Primary);
        SetBrush(root, "PrimaryDarkBrush", palette.PrimaryDark);
        SetBrush(root, "PrimaryLightBrush", palette.PrimaryLight);
        SetBrush(root, "AccentBrush", palette.Accent);
        SetBrush(root, "SurfaceBrush", palette.Surface);
        SetBrush(root, "CardBrush", palette.Card);
        SetBrush(root, "TextBrush", palette.Text);
        SetBrush(root, "MutedTextBrush", palette.MutedText);
        SetBrush(root, "SidebarMutedBrush", palette.SidebarMuted);
        SetBrush(root, "SidebarBackgroundBrush", palette.SidebarBackground);

        SetBrush(root, "BackgroundBrush", palette.Background);
        SetBrush(root, "SurfaceVariantBrush", palette.SurfaceVariant);
        SetBrush(root, "BorderBrush", palette.Border);
        SetBrush(root, "AccentLightBrush", palette.AccentLight);
        SetBrush(root, "ForegroundBrush", palette.Text);
        SetBrush(root, "ForegroundMutedBrush", palette.MutedText);
        SetBrush(root, "TitleBarBrush", palette.TitleBar);
        SetBrush(root, "ButtonHoverBrush", palette.ButtonHover);
        SetBrush(root, "ButtonCloseBrush", palette.ButtonClose);
        SetBrush(root, "ControlHoverBorderBrush", palette.ControlHoverBorder);
        SetBrush(root, "ControlHoverFillBrush", palette.ControlHoverFill);

        SetBrush(root, "TukanSidebarBrush", palette.Sidebar);
        SetBrush(root, "TukanSidebarAccentBrush", palette.SidebarAccent);
        SetBrush(root, "TukanSidebarTextBrush", palette.SidebarText);
        SetBrush(root, "TukanSidebarMutedBrush", palette.SidebarMutedText);
        SetBrush(root, "TukanSidebarHoverBrush", palette.SidebarHover);
    }

    private static void SetColor(ResourceDictionary root, string key, Color color)
    {
        foreach (var dictionary in EnumerateDictionaries(root))
        {
            if (!dictionary.Contains(key))
            {
                continue;
            }

            if (dictionary[key] is Color)
            {
                dictionary[key] = color;
            }
        }
    }

    private static void SetBrush(ResourceDictionary root, string key, Color color)
    {
        foreach (var dictionary in EnumerateDictionaries(root))
        {
            if (!dictionary.Contains(key))
            {
                continue;
            }

            if (dictionary[key] is SolidColorBrush brush)
            {
                if (brush.IsFrozen)
                {
                    dictionary[key] = new SolidColorBrush(color);
                }
                else
                {
                    brush.Color = color;
                }
            }
        }
    }

    private static IEnumerable<ResourceDictionary> EnumerateDictionaries(ResourceDictionary root)
    {
        yield return root;

        foreach (var merged in root.MergedDictionaries)
        {
            foreach (var nested in EnumerateDictionaries(merged))
            {
                yield return nested;
            }
        }
    }

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;

    private readonly record struct UnifiedPalette(
        Color Primary,
        Color PrimaryDark,
        Color PrimaryLight,
        Color Accent,
        Color Background,
        Color Surface,
        Color SurfaceVariant,
        Color Card,
        Color Border,
        Color Text,
        Color MutedText,
        Color SidebarMuted,
        Color SidebarBackground,
        Color AccentLight,
        Color TitleBar,
        Color ButtonHover,
        Color ButtonClose,
        Color ControlHoverBorder,
        Color ControlHoverFill,
        Color Sidebar,
        Color SidebarAccent,
        Color SidebarText,
        Color SidebarMutedText,
        Color SidebarHover);
}
