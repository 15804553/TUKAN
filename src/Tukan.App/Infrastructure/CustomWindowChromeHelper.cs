using System.Windows;
using System.Windows.Input;

namespace Tukan.App.Infrastructure;

/// <summary>
/// Wspólne zachowanie niestandardowego paska tytułu okna WPF (WindowChrome).
/// </summary>
public static class CustomWindowChromeHelper
{
    public static void ApplyMaximizedWorkArea(Window window)
    {
        if (window.WindowState != WindowState.Maximized)
        {
            return;
        }

        var workArea = SystemParameters.WorkArea;
        window.MaxWidth = workArea.Width;
        window.MaxHeight = workArea.Height;
        window.Left = workArea.Left;
        window.Top = workArea.Top;
        window.Width = workArea.Width;
        window.Height = workArea.Height;
    }

    public static void ClearMaximizedWorkAreaConstraints(Window window)
    {
        window.MaxWidth = double.PositiveInfinity;
        window.MaxHeight = double.PositiveInfinity;
    }

    public static void HandleTitleBarMouseLeftButtonDown(Window window, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && window.ResizeMode != ResizeMode.NoResize)
        {
            ToggleWindowState(window);
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            if (window.WindowState == WindowState.Maximized && window.ResizeMode != ResizeMode.NoResize)
            {
                window.WindowState = WindowState.Normal;
            }

            try
            {
                window.DragMove();
            }
            catch (InvalidOperationException)
            {
                // DragMove może się nie powieść przy szybkich kliknięciach.
            }
        }
    }

    public static void ToggleWindowState(Window window)
    {
        if (window.ResizeMode == ResizeMode.NoResize)
        {
            return;
        }

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }
}
