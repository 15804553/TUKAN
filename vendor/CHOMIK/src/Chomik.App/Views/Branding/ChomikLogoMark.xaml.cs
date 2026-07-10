using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chomik.App.Views.Branding;

public enum ChomikLogoVariant
{
    Light,
    Dark
}

public partial class ChomikLogoMark : UserControl
{
    public static readonly DependencyProperty LogoSizeProperty =
        DependencyProperty.Register(
            nameof(LogoSize),
            typeof(double),
            typeof(ChomikLogoMark),
            new PropertyMetadata(56.0, OnLogoSizeChanged));

    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CornerRadius),
            typeof(CornerRadius),
            typeof(ChomikLogoMark),
            new PropertyMetadata(new CornerRadius(12), OnCornerRadiusChanged));

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(
            nameof(Variant),
            typeof(ChomikLogoVariant),
            typeof(ChomikLogoMark),
            new PropertyMetadata(ChomikLogoVariant.Dark));

    public ChomikLogoMark()
    {
        InitializeComponent();
        Loaded += OnLogoLoaded;
        SizeChanged += (_, _) => UpdateRoundedClip();
    }

    public double LogoSize
    {
        get => (double)GetValue(LogoSizeProperty);
        set => SetValue(LogoSizeProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public ChomikLogoVariant Variant
    {
        get => (ChomikLogoVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    private static void OnLogoSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChomikLogoMark logo && e.NewValue is double size)
        {
            logo.Width = size;
            logo.Height = size;
        }
    }

    private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChomikLogoMark logo)
        {
            logo.UpdateRoundedClip();
        }
    }

    private void OnLogoLoaded(object sender, RoutedEventArgs e)
    {
        ApplyVariant();
        UpdateRoundedClip();
    }

    private void ApplyVariant()
    {
        if (LogoBorder is null)
        {
            return;
        }

        LogoBorder.BorderThickness = new Thickness(0);
        LogoBorder.BorderBrush = null;
        LogoBorder.Background = Brushes.Transparent;
        LogoBorder.Effect = null;
    }

    private void UpdateRoundedClip()
    {
        if (LogoBorder is null || LogoBorder.ActualWidth <= 0 || LogoBorder.ActualHeight <= 0)
        {
            return;
        }

        var radius = CornerRadius.TopLeft;
        LogoBorder.Clip = new RectangleGeometry(
            new Rect(0, 0, LogoBorder.ActualWidth, LogoBorder.ActualHeight),
            radius,
            radius);
    }
}
