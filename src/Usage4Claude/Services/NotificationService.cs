using System.Diagnostics;
using Microsoft.Toolkit.Uwp.Notifications;
using Usage4Claude.Models;

namespace Usage4Claude.Services;

/// <summary>
/// Manages Windows Toast notifications for usage events.
/// Subscribes to DataRefreshService events and sends notifications
/// based on user settings and usage thresholds.
/// </summary>
public class NotificationService : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly DataRefreshService _refreshService;

    // Tracking state to avoid duplicate notifications
    private double _lastNotifiedPercentage;
    private bool _wasAboveThreshold;
    private bool _disposed;

    // Notification thresholds
    private static readonly double[] WarningThresholds = { 80.0, 90.0, 95.0 };

    public NotificationService(SettingsService settingsService, DataRefreshService refreshService)
    {
        _settingsService = settingsService;
        _refreshService = refreshService;

        _refreshService.UsageDataChanged += OnUsageDataChanged;
        _refreshService.ErrorChanged += OnErrorChanged;
    }

    private void OnUsageDataChanged(object? sender, UsageData? data)
    {
        if (data == null) return;
        if (!_settingsService.Settings.NotificationsEnabled) return;

        var percentage = data.FiveHour?.Percentage ?? 0;

        // Check if we crossed a threshold upward
        foreach (var threshold in WarningThresholds)
        {
            if (percentage >= threshold && _lastNotifiedPercentage < threshold)
            {
                SendHighUsageNotification(percentage, threshold);
                break; // Only send one notification per update
            }
        }

        // Check for limit reset (was above 50%, now below 10%)
        if (_wasAboveThreshold && percentage < 10.0)
        {
            SendLimitResetNotification();
        }

        _wasAboveThreshold = percentage >= 50.0;
        _lastNotifiedPercentage = percentage;
    }

    private void OnErrorChanged(object? sender, UsageError? error)
    {
        if (error == null) return;
        if (!_settingsService.Settings.NotificationsEnabled) return;

        // Only notify for persistent errors (not transient network issues)
        if (error.ErrorType is UsageErrorType.SessionExpired or UsageErrorType.Unauthorized)
        {
            SendErrorNotification(error);
        }
    }

    private void SendHighUsageNotification(double percentage, double threshold)
    {
        try
        {
            new ToastContentBuilder()
                .AddText("High Usage Warning")
                .AddText($"Your Claude usage is at {percentage:F0}% (crossed {threshold:F0}% threshold)")
                .AddAttributionText("Usage4Claude")
                .Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Toast failed: {ex.Message}");
        }
    }

    private void SendLimitResetNotification()
    {
        try
        {
            new ToastContentBuilder()
                .AddText("Usage Limit Reset")
                .AddText("Your Claude usage limit has been reset.")
                .AddAttributionText("Usage4Claude")
                .Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Toast failed: {ex.Message}");
        }
    }

    private void SendErrorNotification(UsageError error)
    {
        try
        {
            var message = error.ErrorType switch
            {
                UsageErrorType.SessionExpired => "Your session has expired. Please re-login.",
                UsageErrorType.Unauthorized => "Authentication failed. Please check your credentials.",
                _ => $"Error: {error.Message}"
            };

            new ToastContentBuilder()
                .AddText("Usage4Claude Error")
                .AddText(message)
                .AddAttributionText("Usage4Claude")
                .Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Toast failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Send a test notification (for settings UI testing).
    /// </summary>
    public void SendTestNotification()
    {
        try
        {
            new ToastContentBuilder()
                .AddText("Test Notification")
                .AddText("Notifications are working correctly!")
                .AddAttributionText("Usage4Claude")
                .Show();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Test toast failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear all Usage4Claude notifications from Action Center.
    /// </summary>
    public static void ClearAllNotifications()
    {
        try
        {
            ToastNotificationManagerCompat.History.Clear();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationService] Clear failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _refreshService.UsageDataChanged -= OnUsageDataChanged;
        _refreshService.ErrorChanged -= OnErrorChanged;
    }
}
