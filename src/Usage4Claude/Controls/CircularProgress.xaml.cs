using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Usage4Claude.Controls;

/// <summary>
/// A circular progress indicator control that renders an arc/ring
/// for displaying usage percentages. Supports smooth animations,
/// auto-calculated progress colors based on percentage thresholds,
/// and customizable center text display.
/// </summary>
public partial class CircularProgress : UserControl, INotifyPropertyChanged
{
    #region Color Constants

    // Color thresholds matching IconRenderer.cs
    private static readonly Color GreenColor = (Color)ColorConverter.ConvertFromString("#4CAF50");
    private static readonly Color AmberColor = (Color)ColorConverter.ConvertFromString("#FFA726");
    private static readonly Color OrangeColor = (Color)ColorConverter.ConvertFromString("#FF7043");
    private static readonly Color RedColor = (Color)ColorConverter.ConvertFromString("#EF5350");

    #endregion

    #region Dependency Properties

    /// <summary>
    /// The percentage value (0-100) displayed by the progress ring.
    /// </summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(CircularProgress),
        new PropertyMetadata(0.0, OnValueChanged, CoerceValue));

    /// <summary>
    /// The brush used for the background track ring.
    /// </summary>
    public static readonly DependencyProperty TrackColorProperty = DependencyProperty.Register(
        nameof(TrackColor),
        typeof(Brush),
        typeof(CircularProgress),
        new PropertyMetadata(new SolidColorBrush(Color.FromArgb(51, 128, 128, 128))));

    /// <summary>
    /// The brush used for the foreground progress arc.
    /// When not explicitly set, the color is auto-calculated from the Value.
    /// </summary>
    public static readonly DependencyProperty ProgressColorProperty = DependencyProperty.Register(
        nameof(ProgressColor),
        typeof(Brush),
        typeof(CircularProgress),
        new PropertyMetadata(null, OnProgressColorChanged));

    /// <summary>
    /// The thickness of the ring stroke.
    /// </summary>
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness),
        typeof(double),
        typeof(CircularProgress),
        new PropertyMetadata(10.0, OnGeometryAffectingPropertyChanged));

    /// <summary>
    /// The text displayed in the center of the ring.
    /// When not explicitly set, auto-generates from Value (e.g., "45%").
    /// </summary>
    public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register(
        nameof(DisplayText),
        typeof(string),
        typeof(CircularProgress),
        new PropertyMetadata(null, OnDisplayTextChanged));

    /// <summary>
    /// Small text displayed below the main percentage text.
    /// </summary>
    public static readonly DependencyProperty SubTextProperty = DependencyProperty.Register(
        nameof(SubText),
        typeof(string),
        typeof(CircularProgress),
        new PropertyMetadata(null, OnSubTextChanged));

    /// <summary>
    /// Whether value changes are animated with a smooth transition.
    /// </summary>
    public static readonly DependencyProperty IsAnimatedProperty = DependencyProperty.Register(
        nameof(IsAnimated),
        typeof(bool),
        typeof(CircularProgress),
        new PropertyMetadata(true));

    /// <summary>
    /// The diameter of the control in device-independent pixels.
    /// </summary>
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(CircularProgress),
        new PropertyMetadata(100.0, OnGeometryAffectingPropertyChanged));

    #endregion

    #region CLR Property Wrappers

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public Brush TrackColor
    {
        get => (Brush)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public Brush? ProgressColor
    {
        get => (Brush?)GetValue(ProgressColorProperty);
        set => SetValue(ProgressColorProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public string? DisplayText
    {
        get => (string?)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    public string? SubText
    {
        get => (string?)GetValue(SubTextProperty);
        set => SetValue(SubTextProperty, value);
    }

    public bool IsAnimated
    {
        get => (bool)GetValue(IsAnimatedProperty);
        set => SetValue(IsAnimatedProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    #endregion

    #region Computed Properties (for XAML binding)

    private Geometry _trackGeometry = Geometry.Empty;
    private Geometry _arcGeometry = Geometry.Empty;
    private bool _isProgressColorExplicit;

    /// <summary>
    /// The geometry for the full background track circle.
    /// </summary>
    public Geometry TrackGeometry
    {
        get => _trackGeometry;
        private set
        {
            if (_trackGeometry != value)
            {
                _trackGeometry = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// The geometry for the foreground progress arc.
    /// </summary>
    public Geometry ArcGeometry
    {
        get => _arcGeometry;
        private set
        {
            if (_arcGeometry != value)
            {
                _arcGeometry = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// The actual progress color used for rendering. Returns the explicitly set
    /// ProgressColor if available, otherwise auto-calculates from the Value.
    /// </summary>
    public Brush ActualProgressColor
    {
        get
        {
            if (_isProgressColorExplicit && ProgressColor != null)
                return ProgressColor;

            return new SolidColorBrush(GetColorForPercentage(Value));
        }
    }

    /// <summary>
    /// The display text shown in the center. Returns the explicit DisplayText
    /// if set, otherwise auto-generates a percentage string from Value.
    /// </summary>
    public string ActualDisplayText
    {
        get => DisplayText ?? $"{(int)Value}%";
    }

    /// <summary>
    /// Font size for the main display text, scaled relative to the control Size.
    /// </summary>
    public double DisplayFontSize => Size * 0.28;

    /// <summary>
    /// Font size for the sub text, scaled relative to the control Size.
    /// </summary>
    public double SubTextFontSize => Size * 0.11;

    /// <summary>
    /// Visibility of the sub text. Collapsed when SubText is null or empty.
    /// </summary>
    public Visibility SubTextVisibility =>
        string.IsNullOrEmpty(SubText) ? Visibility.Collapsed : Visibility.Visible;

    #endregion

    #region Animation State

    private double _animatedValue;
    private Storyboard? _activeStoryboard;

    /// <summary>
    /// The internal animated value used for arc geometry calculations during transitions.
    /// </summary>
    private double AnimatedValue
    {
        get => _animatedValue;
        set
        {
            _animatedValue = value;
            UpdateArcGeometry(_animatedValue);
        }
    }

    /// <summary>
    /// Attached dependency property used as a target for the animation storyboard.
    /// This allows DoubleAnimation to drive the AnimatedValue through the WPF property system.
    /// </summary>
    private static readonly DependencyProperty AnimatedValueProperty = DependencyProperty.Register(
        "AnimatedValueInternal",
        typeof(double),
        typeof(CircularProgress),
        new PropertyMetadata(0.0, OnAnimatedValueChanged));

    #endregion

    public CircularProgress()
    {
        InitializeComponent();
        UpdateAllGeometry();
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Dependency Property Callbacks

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        var value = (double)baseValue;
        return Math.Clamp(value, 0.0, 100.0);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularProgress control)
        {
            var newValue = (double)e.NewValue;
            var oldValue = (double)e.OldValue;

            // Update auto-calculated color
            if (!control._isProgressColorExplicit)
            {
                control.OnPropertyChanged(nameof(ActualProgressColor));
            }

            // Update display text if auto
            if (control.DisplayText == null)
            {
                control.OnPropertyChanged(nameof(ActualDisplayText));
            }

            // Animate or directly set
            if (control.IsAnimated && control.IsLoaded)
            {
                control.AnimateValue(oldValue, newValue);
            }
            else
            {
                control.SetValue(AnimatedValueProperty, newValue);
                control.UpdateArcGeometry(newValue);
            }
        }
    }

    private static void OnProgressColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularProgress control)
        {
            control._isProgressColorExplicit = e.NewValue != null;
            control.OnPropertyChanged(nameof(ActualProgressColor));
        }
    }

    private static void OnDisplayTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularProgress control)
        {
            control.OnPropertyChanged(nameof(ActualDisplayText));
        }
    }

    private static void OnSubTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularProgress control)
        {
            control.OnPropertyChanged(nameof(SubTextVisibility));
        }
    }

    private static void OnGeometryAffectingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularProgress control)
        {
            control.UpdateAllGeometry();
            control.OnPropertyChanged(nameof(DisplayFontSize));
            control.OnPropertyChanged(nameof(SubTextFontSize));
        }
    }

    private static void OnAnimatedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CircularProgress control)
        {
            control.AnimatedValue = (double)e.NewValue;
        }
    }

    #endregion

    #region Animation

    private void AnimateValue(double from, double to)
    {
        // Stop any running animation
        _activeStoryboard?.Stop(this);
        _activeStoryboard?.Remove(this);

        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(400)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        Storyboard.SetTarget(animation, this);
        Storyboard.SetTargetProperty(animation, new PropertyPath(AnimatedValueProperty));

        _activeStoryboard = storyboard;
        storyboard.Begin(this, true);
    }

    #endregion

    #region Geometry Calculation

    private void UpdateAllGeometry()
    {
        UpdateTrackGeometry();
        UpdateArcGeometry(Value);
    }

    private void UpdateTrackGeometry()
    {
        var radius = (Size - StrokeThickness) / 2.0;
        if (radius <= 0)
        {
            TrackGeometry = Geometry.Empty;
            return;
        }

        var center = new Point(Size / 2.0, Size / 2.0);
        TrackGeometry = new EllipseGeometry(center, radius, radius);
    }

    private void UpdateArcGeometry(double percentage)
    {
        var radius = (Size - StrokeThickness) / 2.0;
        if (radius <= 0 || percentage <= 0)
        {
            ArcGeometry = Geometry.Empty;
            return;
        }

        ArcGeometry = CreateArcGeometry(percentage, radius);
    }

    /// <summary>
    /// Creates a PathGeometry representing a circular arc for the given percentage.
    /// The arc starts at the 12 o'clock position (-90 degrees) and sweeps clockwise.
    /// For percentages at or above 100%, a full ellipse is returned.
    /// </summary>
    /// <param name="percentage">The percentage of the circle to fill (0-100).</param>
    /// <param name="radius">The radius of the arc.</param>
    /// <returns>A Geometry representing the progress arc.</returns>
    private Geometry CreateArcGeometry(double percentage, double radius)
    {
        var center = new Point(Size / 2.0, Size / 2.0);

        if (percentage >= 100)
        {
            return new EllipseGeometry(center, radius, radius);
        }

        var angle = percentage / 100.0 * 360.0;
        var startAngleDeg = -90.0; // 12 o'clock position
        var endAngleDeg = startAngleDeg + angle;

        var startRad = startAngleDeg * Math.PI / 180.0;
        var endRad = endAngleDeg * Math.PI / 180.0;

        var startPoint = new Point(
            center.X + radius * Math.Cos(startRad),
            center.Y + radius * Math.Sin(startRad));

        var endPoint = new Point(
            center.X + radius * Math.Cos(endRad),
            center.Y + radius * Math.Sin(endRad));

        var isLargeArc = angle > 180.0;

        var figure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false,
            IsFilled = false
        };

        figure.Segments.Add(new ArcSegment(
            endPoint,
            new Size(radius, radius),
            0,
            isLargeArc,
            SweepDirection.Clockwise,
            true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    #endregion

    #region Color Calculation

    /// <summary>
    /// Returns a color based on the percentage thresholds.
    /// Matches the color scheme used in IconRenderer.cs.
    /// </summary>
    /// <param name="percentage">The usage percentage (0-100).</param>
    /// <returns>The appropriate color for the given percentage.</returns>
    private static Color GetColorForPercentage(double percentage) => percentage switch
    {
        < 50 => GreenColor,
        < 75 => AmberColor,
        < 90 => OrangeColor,
        _ => RedColor
    };

    #endregion
}
