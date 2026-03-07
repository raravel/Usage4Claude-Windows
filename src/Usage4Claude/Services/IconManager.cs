using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using Usage4Claude.Helpers;
using Usage4Claude.Models;

namespace Usage4Claude.Services;

/// <summary>
/// Manages the system tray icon, updating it based on usage data and display settings.
/// Listens to <see cref="DataRefreshService.UsageDataChanged"/> events and re-renders
/// the tray icon according to the current <see cref="UserSettings.IconDisplayMode"/>
/// and <see cref="UserSettings.IconStyleMode"/>.
/// </summary>
public class IconManager : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly DataRefreshService _refreshService;
    private readonly IconCache _iconCache = new();

    private TaskbarIcon? _taskbarIcon;
    private bool _disposed;

    public IconManager(SettingsService settingsService, DataRefreshService refreshService)
    {
        _settingsService = settingsService;
        _refreshService = refreshService;

        _refreshService.UsageDataChanged += OnUsageDataChanged;
        _refreshService.RefreshingChanged += OnRefreshingChanged;
    }

    /// <summary>
    /// Initialize with the TaskbarIcon instance (called after XAML resources are loaded).
    /// </summary>
    public void Initialize(TaskbarIcon taskbarIcon)
    {
        _taskbarIcon = taskbarIcon;
        UpdateIcon(null); // Show default icon
    }

    /// <summary>
    /// Force icon refresh (e.g., after settings change).
    /// Clears the cache and re-renders with current settings.
    /// </summary>
    public void RefreshIcon()
    {
        _iconCache.Clear();
        UpdateIcon(_refreshService.LastUsageData);
    }

    private void OnUsageDataChanged(object? sender, UsageData? data)
    {
        Application.Current?.Dispatcher.Invoke(() => UpdateIcon(data));
    }

    private void OnRefreshingChanged(object? sender, bool isRefreshing)
    {
        if (!isRefreshing || _taskbarIcon == null) return;
        Application.Current?.Dispatcher.Invoke(() =>
        {
            _taskbarIcon.ToolTipText = "Usage4Claude - Refreshing...";
        });
    }

    private void UpdateIcon(UsageData? data)
    {
        if (_taskbarIcon == null) return;

        var settings = _settingsService.Settings;
        var percentage = data?.FiveHour?.Percentage ?? 0;
        var hasData = data?.FiveHour != null;
        var monochrome = settings.IconStyleMode == IconStyleMode.Monochrome;

        if (!hasData)
        {
            var defaultBitmap = IconRenderer.RenderDefaultIcon(monochrome);
            ApplyIcon(defaultBitmap);
            _taskbarIcon.ToolTipText = "Usage4Claude - No data";
            return;
        }

        var showText = settings.IconDisplayMode is IconDisplayMode.PercentageOnly or IconDisplayMode.Both;
        var showRing = settings.IconDisplayMode is IconDisplayMode.IconOnly or IconDisplayMode.Both;

        var bitmap = _iconCache.GetOrCreate(percentage, showText, showRing, monochrome,
            (pct, text, ring, mono) => IconRenderer.RenderIcon(pct, text, ring, mono));

        ApplyIcon(bitmap);
        _taskbarIcon.ToolTipText = $"Usage4Claude - {percentage:F0}%{FormatResetTimeForTooltip(data?.FiveHour?.ResetsAt)}";
    }

    private static string FormatResetTimeForTooltip(DateTime? resetTime)
    {
        if (resetTime == null) return string.Empty;

        var remaining = resetTime.Value - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero) return string.Empty;

        if (remaining.TotalHours >= 1)
            return $" ({(int)remaining.TotalHours}h)";
        if (remaining.TotalMinutes >= 1)
            return $" ({(int)remaining.TotalMinutes}m)";
        return " (< 1m)";
    }

    /// <summary>
    /// Apply a rendered BitmapSource as the tray icon.
    /// Converts the WPF BitmapSource to a System.Drawing.Icon and updates the TaskbarIcon.
    /// </summary>
    private void ApplyIcon(BitmapSource bitmap)
    {
        if (_taskbarIcon == null) return;

        try
        {
            using var icon = BitmapSourceToIcon(bitmap);
            _taskbarIcon.UpdateIcon(icon);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IconManager] Failed to apply icon: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert a WPF BitmapSource to a System.Drawing.Icon.
    /// Uses PNG encoding and System.Drawing.Bitmap interop for reliable conversion.
    /// </summary>
    private static Icon BitmapSourceToIcon(BitmapSource source)
    {
        // Encode BitmapSource to PNG bytes
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));

        using var memoryStream = new MemoryStream();
        encoder.Save(memoryStream);
        memoryStream.Position = 0;

        // Load into System.Drawing.Bitmap, then convert to Icon via HICON
        using var drawingBitmap = new Bitmap(memoryStream);
        var hIcon = drawingBitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _refreshService.UsageDataChanged -= OnUsageDataChanged;
        _refreshService.RefreshingChanged -= OnRefreshingChanged;
        _iconCache.Clear();
    }
}
