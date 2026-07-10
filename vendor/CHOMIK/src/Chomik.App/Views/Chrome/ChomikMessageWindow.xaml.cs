using System.Windows;

namespace Chomik.App.Views.Chrome;

public partial class ChomikMessageWindow : Window
{
    public ChomikMessageWindow()
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: false);
    }

    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public void Configure(string message, string title, ChomikMessageButtons buttons)
    {
        Title = title;
        TitleBar.Title = title;
        MessageTextBlock.Text = message;

        var showYesNo = buttons == ChomikMessageButtons.YesNo;
        OkButton.Visibility = showYesNo ? Visibility.Collapsed : Visibility.Visible;
        YesButton.Visibility = showYesNo ? Visibility.Visible : Visibility.Collapsed;
        NoButton.Visibility = showYesNo ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.OK;
        DialogResult = true;
    }

    private void OnYesClick(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Yes;
        DialogResult = true;
    }

    private void OnNoClick(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        DialogResult = false;
    }
}
