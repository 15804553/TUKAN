using System.Windows;
using System.Windows.Input;

namespace BOBER.App.Views.Chrome;

public partial class GrafikNotatkaDialog : Window
{
    public GrafikNotatkaDialog()
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: false);
        Loaded += (_, _) =>
        {
            NoteTextBox.Focus();
            NoteTextBox.CaretIndex = NoteTextBox.Text.Length;
        };
    }

    public string NoteText
    {
        get => NoteTextBox.Text;
        set => NoteTextBox.Text = value ?? string.Empty;
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnAcceptClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
