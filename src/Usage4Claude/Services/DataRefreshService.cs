using System.Diagnostics;
using Usage4Claude.Models;

namespace Usage4Claude.Services;

public class DataRefreshService
{
    private readonly ClaudeApiService _apiService;
    private readonly AccountManager _accountManager;
    private readonly SettingsService _settingsService;

    private CancellationTokenSource? _timerCts;
    private Task? _timerTask;
    private DateTime _lastRefreshTime = DateTime.MinValue;
    private DateTime _lastManualRefreshTime = DateTime.MinValue;

    // Public state
    public bool IsRefreshing { get; private set; }
    public UsageData? LastUsageData { get; private set; }
    public UsageError? LastError { get; private set; }
    public DateTime LastRefreshTime => _lastRefreshTime;

    // Events
    public event EventHandler<UsageData?>? UsageDataChanged;
    public event EventHandler<UsageError?>? ErrorChanged;
    public event EventHandler<bool>? RefreshingChanged;

    // Constants
    private static readonly TimeSpan ManualRefreshCooldown = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PopoverRefreshThreshold = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MinimumAnimationDuration = TimeSpan.FromSeconds(1);

    public DataRefreshService(ClaudeApiService apiService, AccountManager accountManager, SettingsService settingsService)
    {
        _apiService = apiService;
        _accountManager = accountManager;
        _settingsService = settingsService;
    }

    /// <summary>
    /// Start periodic data refresh based on settings.
    /// </summary>
    public void Start()
    {
        Stop(); // Cancel any existing timer

        if (!_accountManager.HasAccounts) return;

        var interval = GetRefreshInterval();
        _timerCts = new CancellationTokenSource();
        _timerTask = RunPeriodicRefreshAsync(interval, _timerCts.Token);
    }

    /// <summary>
    /// Stop periodic refresh.
    /// </summary>
    public void Stop()
    {
        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _timerCts = null;
        _timerTask = null;
    }

    /// <summary>
    /// Restart with updated interval (call after settings change).
    /// </summary>
    public void Restart() => Start();

    /// <summary>
    /// Trigger a manual refresh with debounce.
    /// Returns false if refresh was skipped (cooldown active).
    /// </summary>
    public async Task<bool> ManualRefreshAsync()
    {
        if (IsRefreshing) return false;

        var elapsed = DateTime.UtcNow - _lastManualRefreshTime;
        if (elapsed < ManualRefreshCooldown) return false;

        _lastManualRefreshTime = DateTime.UtcNow;
        await FetchUsageAsync();
        return true;
    }

    /// <summary>
    /// Smart refresh for when UI popover opens.
    /// Only refreshes if enough time has passed since last fetch.
    /// </summary>
    public async Task RefreshOnPopoverOpenAsync()
    {
        var elapsed = DateTime.UtcNow - _lastRefreshTime;
        if (elapsed >= PopoverRefreshThreshold && !IsRefreshing)
        {
            await FetchUsageAsync();
        }
    }

    /// <summary>
    /// Force an immediate refresh (ignoring debounce).
    /// </summary>
    public Task ForceRefreshAsync() => FetchUsageAsync();

    private async Task RunPeriodicRefreshAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        // Initial fetch
        await FetchUsageAsync();

        // Periodic polling
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await FetchUsageAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
    }

    private async Task FetchUsageAsync()
    {
        var account = _accountManager.CurrentAccount;
        if (account == null) return;

        SetRefreshing(true);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var data = await _apiService.FetchUsageAsync(account);

            // Enforce minimum animation duration
            var elapsed = stopwatch.Elapsed;
            if (elapsed < MinimumAnimationDuration)
            {
                await Task.Delay(MinimumAnimationDuration - elapsed);
            }

            LastUsageData = data;
            LastError = null;
            _lastRefreshTime = DateTime.UtcNow;

            UsageDataChanged?.Invoke(this, data);
            ErrorChanged?.Invoke(this, null);
        }
        catch (UsageError ex)
        {
            LastError = ex;
            ErrorChanged?.Invoke(this, ex);
            Debug.WriteLine($"[DataRefreshService] Fetch failed: {ex.ErrorType} - {ex.Message}");
        }
        catch (Exception ex)
        {
            var error = new UsageError(UsageErrorType.NetworkError, ex.Message);
            LastError = error;
            ErrorChanged?.Invoke(this, error);
            Debug.WriteLine($"[DataRefreshService] Unexpected error: {ex.Message}");
        }
        finally
        {
            SetRefreshing(false);
        }
    }

    private void SetRefreshing(bool value)
    {
        IsRefreshing = value;
        RefreshingChanged?.Invoke(this, value);
    }

    private TimeSpan GetRefreshInterval()
    {
        var settings = _settingsService.Settings;
        return settings.RefreshMode switch
        {
            RefreshMode.Smart => TimeSpan.FromMinutes(5), // Default smart interval
            RefreshMode.Fixed => TimeSpan.FromSeconds(
                Math.Max(30, settings.RefreshIntervalSeconds)), // Minimum 30 seconds
            _ => TimeSpan.FromMinutes(5)
        };
    }
}
