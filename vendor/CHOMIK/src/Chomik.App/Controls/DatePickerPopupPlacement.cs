using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace Chomik.App.Controls;

public static class DatePickerPopupPlacement
{
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    public static readonly DependencyProperty FixPopupProperty = DependencyProperty.RegisterAttached(
        "FixPopup",
        typeof(bool),
        typeof(DatePickerPopupPlacement),
        new PropertyMetadata(false, OnFixPopupChanged));

    public static bool GetFixPopup(DependencyObject element) => (bool)element.GetValue(FixPopupProperty);

    public static void SetFixPopup(DependencyObject element, bool value) => element.SetValue(FixPopupProperty, value);

    private static void OnFixPopupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DatePicker picker)
        {
            return;
        }

        picker.Loaded -= OnDatePickerLoaded;
        if (e.NewValue is true)
        {
            picker.Loaded += OnDatePickerLoaded;
        }
    }

    private static void OnDatePickerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DatePicker picker)
        {
            return;
        }

        picker.ApplyTemplate();
        if (picker.Template.FindName("PART_Popup", picker) is not Popup popup)
        {
            return;
        }

        popup.Opened -= OnPopupOpened;
        popup.Opened += OnPopupOpened;
    }

    private static void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is not Popup popup)
        {
            return;
        }

        var target = popup.PlacementTarget as FrameworkElement;
        if (target is null)
        {
            return;
        }

        popup.UpdateLayout();
        popup.Child?.UpdateLayout();

        var screen = target.PointToScreen(new Point(0, target.ActualHeight));
        if (popup.Child is not Visual popupVisual)
        {
            return;
        }

        if (PresentationSource.FromVisual(popupVisual) is not HwndSource source || source.Handle == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(
            source.Handle,
            IntPtr.Zero,
            (int)screen.X,
            (int)screen.Y,
            0,
            0,
            SwpNoSize | SwpNoZOrder | SwpNoActivate);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
