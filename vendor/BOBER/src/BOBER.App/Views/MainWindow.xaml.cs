using System.Windows;
using BOBER.App.Controllers;
using BOBER.App.Views.Chrome;

namespace BOBER.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainController controller)
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this);
        GrafikView.Initialize(controller);
        GrafikView.LogoutRequested += (_, _) =>
        {
            DialogResult = false;
            Close();
        };

        Closing += (_, _) =>
        {
            if (DialogResult is null)
            {
                DialogResult = false;
            }
        };
    }
}
