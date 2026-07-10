using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Chomik.App.Controls;

/// <summary>
/// Naprawia pozycjonowanie tooltipów w oknach z AllowsTransparency (custom chrome).
/// </summary>
public static class ToolTipPlacementFix
{
    public static void Register()
    {
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            FrameworkElement.ToolTipOpeningEvent,
            new ToolTipEventHandler(OnToolTipOpening),
            true);
    }

    private static void OnToolTipOpening(object sender, ToolTipEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        ToolTipService.SetPlacement(element, PlacementMode.Mouse);
        ToolTipService.SetPlacementTarget(element, element);
        ToolTipService.SetHorizontalOffset(element, 14);
        ToolTipService.SetVerticalOffset(element, 14);

        if (element.ToolTip is ToolTip explicitTip)
        {
            ApplyToToolTip(explicitTip, element);
        }
    }

    private static void ApplyToToolTip(ToolTip tip, FrameworkElement placementTarget)
    {
        tip.PlacementTarget = placementTarget;
        tip.Placement = PlacementMode.Mouse;
        tip.HorizontalOffset = 14;
        tip.VerticalOffset = 14;
        tip.StaysOpen = false;
    }
}
