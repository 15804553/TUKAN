using System.Windows;
using BOBER.App.Helpers;
using Tukan.App.Infrastructure;

namespace Tukan.App.Views;

public partial class DutyAssignmentsWindow : Window
{
    public DutyAssignmentsWindow(DutyAssignmentsView content, string title)
    {
        Resources = UrlopPlanPalette.CreateResources();
        InitializeComponent();
        Title = $"TUKAN — {title}";
        TitleBar.Title = title;
        ContentHost.Content = content;
        StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            CustomWindowChromeHelper.ApplyMaximizedWorkArea(this);
            return;
        }

        CustomWindowChromeHelper.ClearMaximizedWorkAreaConstraints(this);
    }
}
