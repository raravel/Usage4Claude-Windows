using System.Diagnostics;
using Usage4Claude.Models;

namespace Usage4Claude.Services;

/// <summary>
/// Implements adaptive monitoring that adjusts polling frequency based on usage change detection.
/// When usage changes → Active (1 min). When stable → gradually slows: IdleShort (3 min) → IdleMedium (5 min) → IdleLong (10 min).
/// </summary>
public class SmartMonitorService
{
    private double? _lastUtilization;
    private int _unchangedCount;
    private MonitoringMode _currentMode = MonitoringMode.Active;

    private const double ChangeThreshold = 0.01; // 0.01% change detection

    // Unchanged count thresholds for mode transitions
    private const int ActiveToIdleShort = 3;
    private const int IdleShortToIdleMedium = 6;
    private const int IdleMediumToIdleLong = 12;

    /// <summary>
    /// Current monitoring mode.
    /// </summary>
    public MonitoringMode CurrentMode => _currentMode;

    /// <summary>
    /// Event fired when the monitoring mode changes (interval should be updated).
    /// </summary>
    public event EventHandler<MonitoringMode>? ModeChanged;

    /// <summary>
    /// Get the refresh interval for the current monitoring mode.
    /// </summary>
    public TimeSpan CurrentInterval => GetInterval(_currentMode);

    /// <summary>
    /// Update the monitoring state based on new utilization data.
    /// Call this after each successful API fetch.
    /// </summary>
    public void UpdateUtilization(double currentUtilization)
    {
        if (HasUtilizationChanged(currentUtilization))
        {
            SwitchToActiveMode();
        }
        else
        {
            HandleNoChange();
        }

        _lastUtilization = currentUtilization;
    }

    /// <summary>
    /// Reset to active mode (e.g., on account switch or manual refresh).
    /// </summary>
    public void Reset()
    {
        _lastUtilization = null;
        _unchangedCount = 0;
        SetMode(MonitoringMode.Active);
    }

    private bool HasUtilizationChanged(double current)
    {
        if (_lastUtilization == null) return true; // First reading always active
        return Math.Abs(current - _lastUtilization.Value) >= ChangeThreshold;
    }

    private void SwitchToActiveMode()
    {
        _unchangedCount = 0;
        SetMode(MonitoringMode.Active);
    }

    private void HandleNoChange()
    {
        _unchangedCount++;
        var newMode = CalculateNewMode();
        if (newMode != _currentMode)
        {
            SetMode(newMode);
        }
    }

    private MonitoringMode CalculateNewMode()
    {
        return _currentMode switch
        {
            MonitoringMode.Active when _unchangedCount >= ActiveToIdleShort => MonitoringMode.IdleShort,
            MonitoringMode.IdleShort when _unchangedCount >= IdleShortToIdleMedium => MonitoringMode.IdleMedium,
            MonitoringMode.IdleMedium when _unchangedCount >= IdleMediumToIdleLong => MonitoringMode.IdleLong,
            _ => _currentMode
        };
    }

    private void SetMode(MonitoringMode mode)
    {
        if (_currentMode == mode) return;

        var oldMode = _currentMode;
        _currentMode = mode;
        Debug.WriteLine($"[SmartMonitorService] Mode changed: {oldMode} -> {mode} (interval: {GetInterval(mode).TotalSeconds}s)");
        ModeChanged?.Invoke(this, mode);
    }

    /// <summary>
    /// Get the polling interval for a given mode.
    /// </summary>
    public static TimeSpan GetInterval(MonitoringMode mode) => mode switch
    {
        MonitoringMode.Active => TimeSpan.FromMinutes(1),
        MonitoringMode.IdleShort => TimeSpan.FromMinutes(3),
        MonitoringMode.IdleMedium => TimeSpan.FromMinutes(5),
        MonitoringMode.IdleLong => TimeSpan.FromMinutes(10),
        _ => TimeSpan.FromMinutes(5)
    };
}
