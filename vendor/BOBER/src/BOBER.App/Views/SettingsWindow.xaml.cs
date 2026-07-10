using System.Windows;
using BOBER.App.Controllers;
using BOBER.App.Views.Chrome;

namespace BOBER.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsController controller)
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: true);

        var view = new BoberSettingsView(controller)
        {
            ShowCancelButton = true
        };
        view.SettingsSaved += (_, _) =>
        {
            DialogResult = true;
            Close();
        };
        view.CancelRequested += (_, _) =>
        {
            DialogResult = false;
            Close();
        };
        SettingsHost.Content = view;

        Closing += (_, _) => { if (DialogResult is null) DialogResult = false; };
    }
}
