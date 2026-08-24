using System.Windows;
using System.Windows.Media;

namespace BOBER.App.Helpers;

/// <summary>
/// Paleta TUKAN dla widoków BOBER (plan urlopów, grafik służb, grafik nurkowy) — bez barw piaskowych.
/// </summary>
public static class UrlopPlanPalette
{
    public static readonly SolidColorBrush BackgroundBrush = F("#F2F4F5");
    public static readonly SolidColorBrush SurfaceBrush = F("#F2F4F5");
    public static readonly SolidColorBrush SurfaceVariantBrush = F("#E8ECED");
    public static readonly SolidColorBrush CardBrush = F("#FFFFFF");
    public static readonly SolidColorBrush BorderBrush = F("#D5D8DC");
    public static readonly SolidColorBrush AccentBrush = F("#E67E22");
    public static readonly SolidColorBrush AccentLightBrush = F("#F39C12");
    public static readonly SolidColorBrush ForegroundBrush = F("#2C3E50");
    public static readonly SolidColorBrush ForegroundMutedBrush = F("#7F8C8D");
    public static readonly SolidColorBrush PrimaryBrush = F("#1E2A38");
    public static readonly SolidColorBrush PrimaryLightBrush = F("#2C3E50");
    public static readonly SolidColorBrush ButtonCloseBrush = F("#C42B1C");
    public static readonly SolidColorBrush OkBrush = F("#27AE60");
    public static readonly SolidColorBrush TitleForegroundBrush = F("#ECF0F1");
    public static readonly SolidColorBrush OnAccentBrush = F("#FFFFFF");

    public static void ApplyTo(ResourceDictionary resources)
    {
        resources["BackgroundBrush"] = BackgroundBrush;
        resources["SurfaceBrush"] = SurfaceBrush;
        resources["SurfaceVariantBrush"] = SurfaceVariantBrush;
        resources["CardBrush"] = CardBrush;
        resources["BorderBrush"] = BorderBrush;
        resources["AccentBrush"] = AccentBrush;
        resources["AccentLightBrush"] = AccentLightBrush;
        resources["ForegroundBrush"] = ForegroundBrush;
        resources["ForegroundMutedBrush"] = ForegroundMutedBrush;
        resources["TextBrush"] = ForegroundBrush;
        resources["MutedTextBrush"] = ForegroundMutedBrush;
        resources["PrimaryBrush"] = PrimaryBrush;
        resources["PrimaryLightBrush"] = PrimaryLightBrush;
        resources["ButtonCloseBrush"] = ButtonCloseBrush;
        resources["TitleBarForegroundBrush"] = TitleForegroundBrush;
    }

    public static ResourceDictionary CreateResources()
    {
        var dict = new ResourceDictionary();
        ApplyTo(dict);
        dict.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/BOBER;component/Themes/UrlopPlanChrome.xaml",
                UriKind.Absolute)
        });
        return dict;
    }

    private static SolidColorBrush F(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }
}
