namespace Chomik.App.Views.Branding;

/// <summary>
/// Ścieżki pack URI do zasobów graficznych CHOMIK (działają jako exe i jako biblioteka w TUKAN).
/// </summary>
public static class ChomikBrandingAssets
{
    public const string LogoPngPackUri = "pack://application:,,,/Chomik;component/Assets/logo.png";

    public static Uri LogoPngUri { get; } = new(LogoPngPackUri, UriKind.Absolute);
}
