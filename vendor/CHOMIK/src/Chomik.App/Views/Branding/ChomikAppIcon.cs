using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Chomik.App.Views.Branding;

public static class ChomikAppIcon
{
    private static readonly Lazy<ImageSource> IconSource = new(Load);

    public static ImageSource GetIcon() => IconSource.Value;

    private static ImageSource Load()
    {
        return BitmapFrame.Create(ChomikBrandingAssets.LogoPngUri);
    }
}
