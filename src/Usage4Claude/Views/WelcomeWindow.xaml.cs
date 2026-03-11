using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Usage4Claude.Services;

namespace Usage4Claude.Views;

/// <summary>
/// Welcome wizard shown on first launch. Guides the user through 3 steps:
/// 1. Welcome / introduction
/// 2. Account setup (browser login or manual entry)
/// 3. Final settings (auto-start, notifications) and completion
/// </summary>
public partial class WelcomeWindow : Window
{
    private int _currentStep = 1;
    private bool _accountAdded;

    private static readonly SolidColorBrush ActiveDotBrush = CreateFrozenBrush(0x00, 0x78, 0xD4);
    private static readonly SolidColorBrush InactiveDotBrush = CreateFrozenBrush(0xCC, 0xCC, 0xCC);

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    public WelcomeWindow()
    {
        InitializeComponent();
        GoToStep(1);
    }

    private void GoToStep(int step)
    {
        _currentStep = step;
        Step1Panel.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

        // Update step indicator dots
        Dot1.Fill = step >= 1 ? ActiveDotBrush : InactiveDotBrush;
        Dot2.Fill = step >= 2 ? ActiveDotBrush : InactiveDotBrush;
        Dot3.Fill = step >= 3 ? ActiveDotBrush : InactiveDotBrush;

        // Update step 3 description based on whether an account was added
        if (step == 3)
        {
            ReadyDescription.Text = _accountAdded
                ? "Your account has been configured. Usage4Claude will now monitor your usage."
                : "You can add an account later from Settings.";
        }
    }

    private void GetStarted_Click(object sender, RoutedEventArgs e)
    {
        GoToStep(2);
    }

    private void ManualAdd_Click(object sender, RoutedEventArgs e)
    {
        var orgId = ManualOrgIdBox.Text.Trim();
        var sessionKey = ManualSessionKeyBox.Password.Trim();

        if (string.IsNullOrEmpty(orgId) || string.IsNullOrEmpty(sessionKey))
        {
            MessageBox.Show("Please enter both Organization ID and Session Key.",
                "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Guid.TryParse(orgId, out _))
        {
            MessageBox.Show("Organization ID must be a valid UUID format.",
                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var accountManager = App.Current.Services.GetRequiredService<AccountManager>();
        var added = accountManager.AddAccount(sessionKey, orgId, orgId);

        if (added)
        {
            _accountAdded = true;
            GoToStep(3);
        }
        else
        {
            MessageBox.Show("Failed to save account. Please try again.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        GoToStep(3);
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        // Apply settings
        var settingsService = App.Current.Services.GetRequiredService<SettingsService>();
        settingsService.Settings.LaunchAtLogin = LaunchAtLoginCheckBox.IsChecked == true;
        settingsService.Settings.NotificationsEnabled = NotificationsCheckBox.IsChecked == true;
        settingsService.Settings.IsFirstLaunch = false;
        settingsService.Save();

        // Apply auto-start if checked
        if (LaunchAtLoginCheckBox.IsChecked == true)
        {
            try
            {
                var autoStart = App.Current.Services.GetRequiredService<AutoStartService>();
                autoStart.EnableAutoStart();
            }
            catch (Exception ex)
            {
                // Non-critical: auto-start registration failure should not block wizard completion
                System.Diagnostics.Debug.WriteLine($"[WelcomeWindow] Auto-start failed: {ex.Message}");
            }
        }

        DialogResult = true;
        Close();
    }
}
