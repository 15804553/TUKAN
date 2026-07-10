using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Chomik.App.Views.Branding;

public partial class ChomikBrandHeader : UserControl
{
    public static readonly DependencyProperty LogoSizeProperty =
        DependencyProperty.Register(nameof(LogoSize), typeof(double), typeof(ChomikBrandHeader), new PropertyMetadata(56.0));

    public static readonly DependencyProperty LogoCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(LogoCornerRadius),
            typeof(CornerRadius),
            typeof(ChomikBrandHeader),
            new PropertyMetadata(new CornerRadius(12)));

    public static readonly DependencyProperty VariantProperty =
        DependencyProperty.Register(nameof(Variant), typeof(ChomikLogoVariant), typeof(ChomikBrandHeader), new PropertyMetadata(ChomikLogoVariant.Dark));

    public static readonly DependencyProperty LogoAlignmentProperty =
        DependencyProperty.Register(nameof(LogoAlignment), typeof(HorizontalAlignment), typeof(ChomikBrandHeader), new PropertyMetadata(HorizontalAlignment.Left));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(ChomikBrandHeader), new PropertyMetadata(string.Empty, OnSubtitleChanged));

    public static readonly DependencyProperty SubtitleBrushProperty =
        DependencyProperty.Register(nameof(SubtitleBrush), typeof(Brush), typeof(ChomikBrandHeader));

    public static readonly DependencyProperty SubtitleFontSizeProperty =
        DependencyProperty.Register(nameof(SubtitleFontSize), typeof(double), typeof(ChomikBrandHeader), new PropertyMetadata(12.0));

    public static readonly DependencyProperty SubtitleVisibilityProperty =
        DependencyProperty.Register(nameof(SubtitleVisibility), typeof(Visibility), typeof(ChomikBrandHeader), new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty SubtitleTextAlignmentProperty =
        DependencyProperty.Register(nameof(SubtitleTextAlignment), typeof(TextAlignment), typeof(ChomikBrandHeader), new PropertyMetadata(TextAlignment.Left));

    public static readonly DependencyProperty SubtitleHorizontalAlignmentProperty =
        DependencyProperty.Register(nameof(SubtitleHorizontalAlignment), typeof(HorizontalAlignment), typeof(ChomikBrandHeader), new PropertyMetadata(HorizontalAlignment.Left));

    public ChomikBrandHeader() => InitializeComponent();

    public double LogoSize
    {
        get => (double)GetValue(LogoSizeProperty);
        set => SetValue(LogoSizeProperty, value);
    }

    public CornerRadius LogoCornerRadius
    {
        get => (CornerRadius)GetValue(LogoCornerRadiusProperty);
        set => SetValue(LogoCornerRadiusProperty, value);
    }

    public ChomikLogoVariant Variant
    {
        get => (ChomikLogoVariant)GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public HorizontalAlignment LogoAlignment
    {
        get => (HorizontalAlignment)GetValue(LogoAlignmentProperty);
        set => SetValue(LogoAlignmentProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public Brush? SubtitleBrush
    {
        get => (Brush?)GetValue(SubtitleBrushProperty);
        set => SetValue(SubtitleBrushProperty, value);
    }

    public double SubtitleFontSize
    {
        get => (double)GetValue(SubtitleFontSizeProperty);
        set => SetValue(SubtitleFontSizeProperty, value);
    }

    public Visibility SubtitleVisibility
    {
        get => (Visibility)GetValue(SubtitleVisibilityProperty);
        set => SetValue(SubtitleVisibilityProperty, value);
    }

    public TextAlignment SubtitleTextAlignment
    {
        get => (TextAlignment)GetValue(SubtitleTextAlignmentProperty);
        set => SetValue(SubtitleTextAlignmentProperty, value);
    }

    public HorizontalAlignment SubtitleHorizontalAlignment
    {
        get => (HorizontalAlignment)GetValue(SubtitleHorizontalAlignmentProperty);
        set => SetValue(SubtitleHorizontalAlignmentProperty, value);
    }

    private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChomikBrandHeader header)
        {
            header.SubtitleVisibility = string.IsNullOrWhiteSpace(header.Subtitle)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
