using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Usage4Claude.Models;
using Usage4Claude.Services;

namespace Usage4Claude.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DataRefreshService _refreshService;
    private readonly AccountManager _accountManager;
    private readonly SmartMonitorService _smartMonitor;
    private readonly SettingsService _settingsService;

    // Raw reset-at timestamps for countdown computation
    private DateTime? _fiveHourResetsAt;
    private DateTime? _sevenDayResetsAt;

    // Usage data properties
    private double _fiveHourPercentage;
    private string _fiveHourResetTime = string.Empty;
    private double? _sevenDayPercentage;
    private string? _sevenDayResetTime;
    private double? _opusPercentage;
    private double? _sonnetPercentage;

    // Countdown properties
    private string _fiveHourCountdown = string.Empty;
    private string _sevenDayCountdown = string.Empty;
    private bool _showRemainingMode;

    // Extra usage
    private bool _hasExtraUsage;
    private double? _extraUsagePercentage;
    private string _extraUsageText = string.Empty;

    // State
    private bool _isRefreshing;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private string _statusText = string.Empty;
    private string _accountDisplayName = string.Empty;
    private bool _hasAccounts;
    private bool _hasMultipleAccounts;

    // Countdown timer
    private DispatcherTimer? _countdownTimer;

    // Properties with change notification
    public double FiveHourPercentage { get => _fiveHourPercentage; private set => SetProperty(ref _fiveHourPercentage, value); }
    public string FiveHourResetTime { get => _fiveHourResetTime; private set => SetProperty(ref _fiveHourResetTime, value); }
    public double? SevenDayPercentage { get => _sevenDayPercentage; private set => SetProperty(ref _sevenDayPercentage, value); }
    public string? SevenDayResetTime { get => _sevenDayResetTime; private set => SetProperty(ref _sevenDayResetTime, value); }
    public double? OpusPercentage { get => _opusPercentage; private set => SetProperty(ref _opusPercentage, value); }
    public double? SonnetPercentage { get => _sonnetPercentage; private set => SetProperty(ref _sonnetPercentage, value); }

    public string FiveHourCountdown { get => _fiveHourCountdown; private set => SetProperty(ref _fiveHourCountdown, value); }
    public string SevenDayCountdown { get => _sevenDayCountdown; private set => SetProperty(ref _sevenDayCountdown, value); }

    public bool ShowRemainingMode
    {
        get => _showRemainingMode;
        set
        {
            if (SetProperty(ref _showRemainingMode, value))
            {
                NotifyDisplayValueProperties();
            }
        }
    }

    // Computed display values that switch between reset time and countdown
    public string FiveHourDisplayValue
    {
        get
        {
            var suffix = ShowRemainingMode ? FiveHourCountdown : FiveHourResetTime;
            return string.IsNullOrEmpty(suffix)
                ? $"{FiveHourPercentage:F0}%"
                : $"{FiveHourPercentage:F0}% - {suffix}";
        }
    }

    public string SevenDayDisplayValue => SevenDayPercentage.HasValue
        ? (ShowRemainingMode
            ? $"{SevenDayPercentage:F0}% - {SevenDayCountdown}"
            : $"{SevenDayPercentage:F0}% - {SevenDayResetTime}")
        : string.Empty;

    public string OpusDisplayValue => OpusPercentage.HasValue
        ? $"{OpusPercentage:F0}%"
        : string.Empty;

    public string SonnetDisplayValue => SonnetPercentage.HasValue
        ? $"{SonnetPercentage:F0}%"
        : string.Empty;

    public bool HasExtraUsage { get => _hasExtraUsage; private set => SetProperty(ref _hasExtraUsage, value); }
    public double? ExtraUsagePercentage { get => _extraUsagePercentage; private set => SetProperty(ref _extraUsagePercentage, value); }
    public string ExtraUsageText { get => _extraUsageText; private set => SetProperty(ref _extraUsageText, value); }

    public bool IsRefreshing { get => _isRefreshing; private set => SetProperty(ref _isRefreshing, value); }
    public bool HasError { get => _hasError; private set => SetProperty(ref _hasError, value); }
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string AccountDisplayName { get => _accountDisplayName; private set => SetProperty(ref _accountDisplayName, value); }
    public bool HasAccounts { get => _hasAccounts; private set => SetProperty(ref _hasAccounts, value); }
    public bool HasMultipleAccounts { get => _hasMultipleAccounts; private set => SetProperty(ref _hasMultipleAccounts, value); }

    // Commands
    public ICommand RefreshCommand { get; }
    public ICommand ToggleDisplayModeCommand { get; }
    public ICommand CycleAccountCommand { get; }

    public MainViewModel(DataRefreshService refreshService, AccountManager accountManager, SmartMonitorService smartMonitor, SettingsService settingsService)
    {
        _refreshService = refreshService;
        _accountManager = accountManager;
        _smartMonitor = smartMonitor;
        _settingsService = settingsService;

        RefreshCommand = new AsyncRelayCommand(async () => await _refreshService.ManualRefreshAsync());
        ToggleDisplayModeCommand = new RelayCommand(() => ShowRemainingMode = !ShowRemainingMode);
        CycleAccountCommand = new RelayCommand(CycleToNextAccount);

        // Subscribe to service events
        _refreshService.UsageDataChanged += OnUsageDataChanged;
        _refreshService.ErrorChanged += OnErrorChanged;
        _refreshService.RefreshingChanged += OnRefreshingChanged;

        // Listen for account changes to update UI automatically
        _accountManager.CurrentAccountChanged += (_, _) =>
        {
            Application.Current.Dispatcher.BeginInvoke(UpdateAccountInfo);
        };

        // Initial state
        UpdateAccountInfo();
    }

    /// <summary>
    /// Start the countdown timer. Called when the popup window opens.
    /// The timer runs on the UI dispatcher thread, so property updates are safe.
    /// </summary>
    public void StartCountdownTimer()
    {
        if (_countdownTimer != null) return;

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += OnCountdownTick;
        _countdownTimer.Start();

        // Update immediately so the countdown shows right away
        UpdateCountdownValues();
    }

    /// <summary>
    /// Stop the countdown timer. Called when the popup window closes.
    /// </summary>
    public void StopCountdownTimer()
    {
        if (_countdownTimer == null) return;

        _countdownTimer.Tick -= OnCountdownTick;
        _countdownTimer.Stop();
        _countdownTimer = null;
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        UpdateCountdownValues();

        // Stop timer if both reset times have passed or are null
        var fiveHourExpired = !_fiveHourResetsAt.HasValue || _fiveHourResetsAt.Value <= DateTime.UtcNow;
        var sevenDayExpired = !_sevenDayResetsAt.HasValue || _sevenDayResetsAt.Value <= DateTime.UtcNow;

        if (fiveHourExpired && sevenDayExpired)
        {
            StopCountdownTimer();
        }
    }

    private void UpdateCountdownValues()
    {
        FiveHourCountdown = FormatCountdown(_fiveHourResetsAt, useShortFormat: false);
        SevenDayCountdown = FormatCountdown(_sevenDayResetsAt, useShortFormat: true);
        NotifyDisplayValueProperties();
    }

    private void NotifyDisplayValueProperties()
    {
        OnPropertyChanged(nameof(FiveHourDisplayValue));
        OnPropertyChanged(nameof(SevenDayDisplayValue));
        OnPropertyChanged(nameof(OpusDisplayValue));
        OnPropertyChanged(nameof(SonnetDisplayValue));
    }

    private void OnUsageDataChanged(object? sender, UsageData? data)
    {
        if (data == null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            // Store raw reset timestamps for countdown computation
            _fiveHourResetsAt = data.FiveHour?.ResetsAt;
            _sevenDayResetsAt = data.SevenDay?.ResetsAt;

            // Five hour (always present)
            FiveHourPercentage = data.FiveHour?.Percentage ?? 0;
            FiveHourResetTime = FormatResetTime(data.FiveHour?.ResetsAt);

            // Seven day
            SevenDayPercentage = data.SevenDay?.Percentage;
            SevenDayResetTime = FormatResetTime(data.SevenDay?.ResetsAt);

            // Model-specific
            OpusPercentage = data.Opus?.Percentage;
            SonnetPercentage = data.Sonnet?.Percentage;

            // Extra usage
            HasExtraUsage = data.ExtraUsage?.Enabled == true;
            ExtraUsagePercentage = data.ExtraUsage?.Percentage;
            ExtraUsageText = FormatExtraUsage(data.ExtraUsage);

            // Update countdown and display values
            if (_countdownTimer != null)
            {
                UpdateCountdownValues();
            }
            else
            {
                // Timer not running - still notify display value changes for XAML bindings
                NotifyDisplayValueProperties();

                // Restart timer if new future reset timestamps arrived
                var hasFutureReset = (_fiveHourResetsAt.HasValue && _fiveHourResetsAt.Value > DateTime.UtcNow)
                                  || (_sevenDayResetsAt.HasValue && _sevenDayResetsAt.Value > DateTime.UtcNow);
                if (hasFutureReset)
                {
                    StartCountdownTimer();
                }
            }

            // Status
            HasError = false;
            ErrorMessage = string.Empty;
            StatusText = $"Updated {DateTime.Now:HH:mm:ss}";
        });
    }

    private void OnErrorChanged(object? sender, UsageError? error)
    {
        if (error == null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            HasError = true;
            ErrorMessage = error.Message;
            StatusText = $"Error: {error.ErrorType}";
        });
    }

    private void OnRefreshingChanged(object? sender, bool isRefreshing)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            IsRefreshing = isRefreshing;
            if (isRefreshing)
            {
                StatusText = "Refreshing...";
            }
        });
    }

    public void UpdateAccountInfo()
    {
        HasAccounts = _accountManager.HasAccounts;
        HasMultipleAccounts = _accountManager.Accounts.Count > 1;
        AccountDisplayName = _accountManager.CurrentAccount?.DisplayName ?? "No account";
    }

    private void CycleToNextAccount()
    {
        var accounts = _accountManager.Accounts;
        if (accounts.Count <= 1) return;

        var currentId = _accountManager.CurrentAccount?.Id;
        int currentIndex = -1;
        for (int i = 0; i < accounts.Count; i++)
        {
            if (accounts[i].Id == currentId) { currentIndex = i; break; }
        }
        var nextIndex = (currentIndex + 1) % accounts.Count;

        if (_accountManager.SwitchAccount(accounts[nextIndex].Id))
        {
            // Reset and restart refresh for the new account
            _refreshService.Reset();
            _refreshService.Start();

            // Update tray icon
            var iconManager = App.Current.Services.GetRequiredService<IconManager>();
            iconManager.RefreshIcon();

            // Note: UpdateAccountInfo() is triggered automatically via CurrentAccountChanged event.
        }
    }

    private string FormatResetTime(DateTime? resetTime)
    {
        if (resetTime == null) return string.Empty;

        var remaining = resetTime.Value - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero) return "Resetting...";

        var timeFormat = _settingsService.Settings.TimeFormat;
        var localResetTime = resetTime.Value.ToLocalTime();

        return timeFormat switch
        {
            TimeFormatPreference.TwelveHour => localResetTime.ToString("h:mm tt"),
            TimeFormatPreference.TwentyFourHour => localResetTime.ToString("HH:mm"),
            _ => FormatResetTimeRelative(remaining) // System default: relative format
        };
    }

    private static string FormatResetTimeRelative(TimeSpan remaining)
    {
        if (remaining.TotalDays >= 1)
            return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        if (remaining.TotalMinutes >= 1)
            return $"{(int)remaining.TotalMinutes}m";
        return "< 1m";
    }

    /// <summary>
    /// Format a countdown from a reset-at timestamp to a human-readable string
    /// that updates every second.
    /// </summary>
    /// <param name="resetsAt">The UTC time when the limit resets.</param>
    /// <param name="useShortFormat">
    /// When true, uses day-granularity format (Xd Yh Zm) for longer periods.
    /// When false, always includes seconds (Xh Ym Zs).
    /// </param>
    private static string FormatCountdown(DateTime? resetsAt, bool useShortFormat)
    {
        if (resetsAt == null) return string.Empty;

        var remaining = resetsAt.Value - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero) return "Resetting...";

        if (useShortFormat && remaining.TotalDays >= 1)
        {
            return $"{(int)remaining.TotalDays}d {remaining.Hours}h {remaining.Minutes}m";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m {remaining.Seconds:D2}s";
        }

        if (remaining.TotalMinutes >= 1)
        {
            return $"{(int)remaining.TotalMinutes}m {remaining.Seconds:D2}s";
        }

        return $"{remaining.Seconds}s";
    }

    private static string FormatExtraUsage(ExtraUsageData? extra)
    {
        if (extra == null || !extra.Enabled) return string.Empty;

        var used = extra.Used ?? 0;
        var limit = extra.Limit ?? 0;
        return $"${used:F2} / ${limit:F2} {extra.Currency.ToUpperInvariant()}";
    }
}
