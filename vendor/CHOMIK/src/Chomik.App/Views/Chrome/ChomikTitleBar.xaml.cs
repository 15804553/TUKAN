using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chomik.App.Views.Chrome;

public partial class ChomikTitleBar : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ChomikTitleBar),
            new PropertyMetadata("Chomik", OnTitleChanged));

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChomikTitleBar titleBar)
        {
            titleBar.UpdateTitleText();
        }
    }

    public static readonly DependencyProperty ShowMinimizeButtonProperty =
        DependencyProperty.Register(nameof(ShowMinimizeButton), typeof(bool), typeof(ChomikTitleBar), new PropertyMetadata(false, OnChromeButtonsChanged));

    public static readonly DependencyProperty ShowMaximizeButtonProperty =
        DependencyProperty.Register(nameof(ShowMaximizeButton), typeof(bool), typeof(ChomikTitleBar), new PropertyMetadata(false, OnChromeButtonsChanged));

    public ChomikTitleBar()
    {
        InitializeComponent();
        Loaded += OnTitleBarLoaded;
    }

    private void OnTitleBarLoaded(object sender, RoutedEventArgs e)
    {
        UpdateTitleText();
        UpdateChromeButtons();
        UpdateMaximizeGlyph();
        if (HostWindow is not null)
        {
            HostWindow.StateChanged += (_, _) => UpdateMaximizeGlyph();
        }
    }

    private void UpdateTitleText()
    {
        if (TitleTextBlock is not null)
        {
            TitleTextBlock.Text = Title;
        }
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool ShowMinimizeButton
    {
        get => (bool)GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    public bool ShowMaximizeButton
    {
        get => (bool)GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    private static void OnChromeButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChomikTitleBar titleBar)
        {
            titleBar.UpdateChromeButtons();
        }
    }

    private void UpdateChromeButtons()
    {
        MinimizeButton.Visibility = ShowMinimizeButton ? Visibility.Visible : Visibility.Collapsed;
        MaximizeButton.Visibility = ShowMaximizeButton ? Visibility.Visible : Visibility.Collapsed;
    }

    private Window? HostWindow => Window.GetWindow(this);

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ShowMaximizeButton)
        {
            ToggleMaximize();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            HostWindow?.DragMove();
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        if (HostWindow is not null)
        {
            HostWindow.WindowState = WindowState.Minimized;
        }
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void ToggleMaximize()
    {
        if (HostWindow is null)
        {
            return;
        }

        HostWindow.WindowState = HostWindow.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        UpdateMaximizeGlyph();
    }

    private void UpdateMaximizeGlyph()
    {
        if (HostWindow is null)
        {
            return;
        }

        MaximizeButton.Content = HostWindow.WindowState == WindowState.Maximized ? "❐" : "▢";
        MaximizeButton.ToolTip = HostWindow.WindowState == WindowState.Maximized ? "Przywróć" : "Maksymalizuj";
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => HostWindow?.Close();
}
