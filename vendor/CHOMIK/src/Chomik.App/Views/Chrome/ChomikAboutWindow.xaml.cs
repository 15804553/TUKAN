using System.IO;
using System.Reflection;
using System.Windows;

namespace Chomik.App.Views.Chrome;

public partial class ChomikAboutWindow : Window
{
    public ChomikAboutWindow()
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: false);
        VersionTextBlock.Text = BuildVersionLine();
    }

    private static string BuildVersionLine()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var versionText = version is { Major: > 0, Build: > 0 }
            ? $"Wersja {version.Major}.{version.Minor}.{version.Build}"
            : version is { Major: > 0 }
                ? $"Wersja {version.Major}.{version.Minor}"
                : "Wersja 2.4";

        var buildDate = File.GetLastWriteTime(assembly.Location);
        return $"{versionText}  •  {buildDate:dd.MM.yyyy}";
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
