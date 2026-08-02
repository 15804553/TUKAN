using System.Windows;
using System.Windows.Media;

namespace Tukan.App.Infrastructure;

/// <summary>
/// Jednolita paleta kolorystyczna aplikacji TUKAN (motyw klasyczny).
/// </summary>
public static class TukanAppTheme
{
    public static void Apply()
    {
        if (Application.Current?.Resources is not ResourceDictionary root)
        {
            return;
        }

        ApplyPalette(root, CreateDefaultPalette());
    }

    private static UnifiedPalette CreateDefaultPalette() => new(
        Primary: C("#1E2A38"), PrimaryDark: C("#15202B"), PrimaryLight: C("#2C3E50"),
        Accent: C("#E67E22"), Background: C("#F2F4F5"), Surface: C("#F2F4F5"),
        SurfaceVariant: C("#E8ECED"), Card: C("#FFFFFF"), Border: C("#D5D8DC"),
        Text: C("#2C3E50"), MutedText: C("#7F8C8D"), SidebarMuted: C("#95A5A6"),
        SidebarBackground: C("#1E2A38"), AccentLight: C("#F39C12"),
        TitleBar: C("#2C3E50"), TitleBarForeground: C("#ECF0F1"),
        ButtonHover: C("#2C3E50"), ButtonClose: C("#C42B1C"),
        ControlHoverBorder: C("#E67E22"), ControlHoverFill: C("#E8ECED"),
        Sidebar: C("#1E2A38"), SidebarAccent: C("#E67E22"), SidebarText: C("#ECF0F1"),
        SidebarMutedText: C("#95A5A6"), SidebarHover: C("#2C3E50"));

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
        SetColor(root, "TitleBarForegroundColor", palette.TitleBarForeground);
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
        SetBrush(root, "TitleBarForegroundBrush", palette.TitleBarForeground);
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
        Color TitleBarForeground,
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
