using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Usage4Claude.Controls;

/// <summary>
/// A reusable row control for displaying usage limit information.
/// Shows a colored indicator dot, a limit type name, and a value text.
/// </summary>
public partial class UsageInfoRow : UserControl
{
    /// <summary>
    /// The display name for the limit type (e.g., "5-Hour", "7-Day", "Opus").
    /// </summary>
    public static readonly DependencyProperty LimitNameProperty = DependencyProperty.Register(
        nameof(LimitName),
        typeof(string),
        typeof(UsageInfoRow),
        new PropertyMetadata(string.Empty));

    /// <summary>
    /// The value text shown on the right side (e.g., "45% - 2h 30m").
    /// </summary>
    public static readonly DependencyProperty ValueTextProperty = DependencyProperty.Register(
        nameof(ValueText),
        typeof(string),
        typeof(UsageInfoRow),
        new PropertyMetadata(string.Empty));

    /// <summary>
    /// The brush used for the colored indicator dot.
    /// </summary>
    public static readonly DependencyProperty IndicatorColorProperty = DependencyProperty.Register(
        nameof(IndicatorColor),
        typeof(Brush),
        typeof(UsageInfoRow),
        new PropertyMetadata(Brushes.Gray));

    public string LimitName
    {
        get => (string)GetValue(LimitNameProperty);
        set => SetValue(LimitNameProperty, value);
    }

    public string ValueText
    {
        get => (string)GetValue(ValueTextProperty);
        set => SetValue(ValueTextProperty, value);
    }

    public Brush IndicatorColor
    {
        get => (Brush)GetValue(IndicatorColorProperty);
        set => SetValue(IndicatorColorProperty, value);
    }

    public UsageInfoRow()
    {
        InitializeComponent();
    }
}
