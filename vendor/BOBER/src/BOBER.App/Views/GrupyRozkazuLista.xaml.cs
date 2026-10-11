using System.Windows.Controls;
using System.Windows.Input;
using BOBER.App.ViewModels;

namespace BOBER.App.Views;

public partial class GrupyRozkazuLista : UserControl
{
    public GrupyRozkazuLista()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Otwieramy listę na puszczeniu przycisku. MouseDown zostawiony siatce
    /// zamyka popup od razu, bo puszczenie myszy liczy się jako kliknięcie obok.
    /// </summary>
    private void OnPrzyciskPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsEnabled || e.ChangedButton != MouseButton.Left)
            return;

        e.Handled = true;
        Przycisk.CaptureMouse();
    }

    private void OnPrzyciskPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!Przycisk.IsMouseCaptured || e.ChangedButton != MouseButton.Left)
            return;

        Przycisk.ReleaseMouseCapture();
        e.Handled = true;
        if (Przycisk.IsMouseOver)
            Przycisk.IsChecked = Przycisk.IsChecked != true;
    }

    /// <summary>
    /// Checkbox w popupie przejmuje mysz i lista znika, zanim ptaszek się zapisze.
    /// </summary>
    private void OnOpcjaPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not CheckBox check || DataContext is not OznaczenieGrafikuViewModel vm)
            return;

        var grupy = vm.Grupy;
        switch (check.Tag as string)
        {
            case "Urlopy":
                grupy.Urlopy = !grupy.Urlopy;
                break;
            case "Wolna":
                grupy.WolnaSluzba = !grupy.WolnaSluzba;
                break;
            case "Chorzy":
                grupy.Chorzy = !grupy.Chorzy;
                break;
            case "Delegacje":
                grupy.Delegacje = !grupy.Delegacje;
                break;
            case "Dyzur":
                grupy.DyzurDomowy = !grupy.DyzurDomowy;
                break;
            default:
                return;
        }

        e.Handled = true;
    }
}
