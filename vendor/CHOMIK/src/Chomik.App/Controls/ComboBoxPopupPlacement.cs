using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace Chomik.App.Controls;

public static class ComboBoxPopupPlacement
{
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    public static readonly DependencyProperty FixPopupProperty = DependencyProperty.RegisterAttached(
        "FixPopup",
        typeof(bool),
        typeof(ComboBoxPopupPlacement),
        new PropertyMetadata(false, OnFixPopupChanged));

    public static bool GetFixPopup(DependencyObject element) => (bool)element.GetValue(FixPopupProperty);

    public static void SetFixPopup(DependencyObject element, bool value) => element.SetValue(FixPopupProperty, value);

    private static void OnFixPopupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox combo)
        {
            return;
        }

        combo.Loaded -= OnComboBoxLoaded;
        combo.DropDownOpened -= OnDropDownOpened;

        if (e.NewValue is true)
        {
            combo.Loaded += OnComboBoxLoaded;
            combo.DropDownOpened += OnDropDownOpened;
        }
    }

    private static void OnComboBoxLoaded(object sender, RoutedEventArgs e) => ConfigurePopup(sender as ComboBox);

    private static void OnDropDownOpened(object? sender, EventArgs e)
    {
        if (sender is not ComboBox combo)
        {
            return;
        }

        ConfigurePopup(combo);
        combo.Dispatcher.BeginInvoke(() => ConfigurePopup(combo), System.Windows.Threading.DispatcherPriority.Loaded);
        combo.Dispatcher.BeginInvoke(() => AlignPopupToTarget(combo), System.Windows.Threading.DispatcherPriority.Render);
    }

    private static void ConfigurePopup(ComboBox? combo)
    {
        if (combo is null)
        {
            return;
        }

        combo.ApplyTemplate();
        if (combo.Template.FindName("PART_Popup", combo) is not Popup popup)
        {
            return;
        }

        popup.Opened -= OnPopupOpened;
        popup.Opened += OnPopupOpened;

        var placementTarget = combo.Template.FindName("PART_ToggleButton", combo) as UIElement ?? combo;
        popup.PlacementTarget = placementTarget;
        popup.Placement = PlacementMode.Bottom;
        popup.HorizontalOffset = 0;
        popup.VerticalOffset = 2;
        popup.SetValue(Popup.AllowsTransparencyProperty, false);
    }

    private static void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is not Popup popup)
        {
            return;
        }

        var target = popup.PlacementTarget as FrameworkElement
            ?? (popup.TemplatedParent as ComboBox);
        if (target is null)
        {
            return;
        }

        AlignPopupToTarget(target, popup);
    }

    private static void AlignPopupToTarget(ComboBox combo) =>
        AlignPopupToTarget(combo, combo.Template.FindName("PART_Popup", combo) as Popup);

    private static void AlignPopupToTarget(FrameworkElement target, Popup? popup)
    {
        if (popup is null || !popup.IsOpen)
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
