using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tukan.App.Infrastructure;

namespace Tukan.App.Views.Chrome;

public partial class TukanTitleBar : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TukanTitleBar),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public static readonly DependencyProperty ShowMinimizeButtonProperty =
        DependencyProperty.Register(nameof(ShowMinimizeButton), typeof(bool), typeof(TukanTitleBar),
            new PropertyMetadata(true, OnButtonsChanged));

    public static readonly DependencyProperty ShowMaximizeButtonProperty =
        DependencyProperty.Register(nameof(ShowMaximizeButton), typeof(bool), typeof(TukanTitleBar),
            new PropertyMetadata(true, OnButtonsChanged));

    public TukanTitleBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
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

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TukanTitleBar titleBar)
        {
            titleBar.UpdateTitle();
        }
    }

    private static void OnButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TukanTitleBar titleBar)
        {
            titleBar.UpdateButtons();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateTitle();
        UpdateButtons();
        UpdateMaximizeGlyph();

        if (HostWindow is not null)
        {
            HostWindow.StateChanged += (_, _) => UpdateMaximizeGlyph();
        }
    }

    private void UpdateTitle() => TitleTextBlock.Text = Title;

    private void UpdateButtons()
    {
        MinimizeButton.Visibility = ShowMinimizeButton ? Visibility.Visible : Visibility.Collapsed;
        MaximizeButton.Visibility = ShowMaximizeButton ? Visibility.Visible : Visibility.Collapsed;
    }

    private Window? HostWindow => Window.GetWindow(this);

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (HostWindow is null)
        {
            return;
        }

        CustomWindowChromeHelper.HandleTitleBarMouseLeftButtonDown(HostWindow, e);
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        if (HostWindow is not null)
        {
            HostWindow.WindowState = WindowState.Minimized;
        }
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        if (HostWindow is not null)
        {
            CustomWindowChromeHelper.ToggleWindowState(HostWindow);
            UpdateMaximizeGlyph();
        }
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
