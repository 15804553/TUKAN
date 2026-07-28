using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Chomik.App.Views.Branding;

public static class ChomikAppIcon
{
    private static readonly Lazy<ImageSource> IconSource = new(Load);

    /// <summary>
    /// Ikona hosta (np. TUKAN) — gdy ustawiona, zastępuje logo Chomika w oknach chrome.
    /// </summary>
    public static ImageSource? IconOverride { get; set; }

    public static ImageSource GetIcon() => IconOverride ?? IconSource.Value;

    private static ImageSource Load()
    {
        return BitmapFrame.Create(ChomikBrandingAssets.LogoPngUri);
    }
}
