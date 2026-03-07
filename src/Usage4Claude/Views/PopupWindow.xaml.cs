using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Usage4Claude.Views;

/// <summary>
/// A borderless popup window that displays usage details when the user
/// left-clicks the system tray icon. Closes automatically when it loses
/// focus or when the user presses Escape.
/// </summary>
public partial class PopupWindow : Window
{
    private Storyboard? _spinStoryboard;

    public PopupWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Start or stop the spin animation based on the current IsRefreshing state
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateSpinAnimation(vm.IsRefreshing);
            vm.StartCountdownTimer();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.MainViewModel.IsRefreshing) && sender is ViewModels.MainViewModel vm)
        {
            Dispatcher.Invoke(() => UpdateSpinAnimation(vm.IsRefreshing));
        }
    }

    private void UpdateSpinAnimation(bool isRefreshing)
    {
        if (_spinStoryboard == null)
        {
            _spinStoryboard = FindResource("SpinAnimation") as Storyboard;
        }

        if (_spinStoryboard == null) return;

        if (isRefreshing)
        {
            _spinStoryboard.Begin(this, true);
        }
        else
        {
            _spinStoryboard.Stop(this);
        }
    }

    /// <summary>
    /// Toggle between reset time and countdown display when the info rows are clicked.
    /// </summary>
    private void InfoRows_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.ToggleDisplayModeCommand.Execute(null);
        }
    }

    /// <summary>
    /// Close the popup when it loses focus (user clicks elsewhere).
    /// </summary>
    private void Window_Deactivated(object? sender, EventArgs e)
    {
        CleanupAndClose();
    }

    /// <summary>
    /// Close the popup when the user presses Escape.
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CleanupAndClose();
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// Position the popup above the system tray area (bottom-right of screen,
    /// above the taskbar). Uses SystemParameters.WorkArea to determine the
    /// available screen space excluding the taskbar.
    /// </summary>
    public void PositionNearTray()
    {
        // Ensure layout is calculated so ActualWidth/ActualHeight are available
        if (ActualWidth == 0 || ActualHeight == 0)
        {
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Arrange(new Rect(DesiredSize));
            UpdateLayout();
        }

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 8;
        Top = workArea.Bottom - ActualHeight - 8;
    }

    private void CleanupAndClose()
    {
        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.StopCountdownTimer();
        }

        _spinStoryboard?.Stop(this);
        Close();
    }
}
