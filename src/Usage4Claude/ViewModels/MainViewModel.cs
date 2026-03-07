using System.Windows;
using System.Windows.Input;
using Usage4Claude.Models;
using Usage4Claude.Services;

namespace Usage4Claude.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly DataRefreshService _refreshService;
    private readonly AccountManager _accountManager;
    private readonly SmartMonitorService _smartMonitor;

    // Usage data properties
    private double _fiveHourPercentage;
    private string _fiveHourResetTime = string.Empty;
    private double? _sevenDayPercentage;
    private string? _sevenDayResetTime;
    private double? _opusPercentage;
    private double? _sonnetPercentage;

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

    // Properties with change notification
    public double FiveHourPercentage { get => _fiveHourPercentage; private set => SetProperty(ref _fiveHourPercentage, value); }
    public string FiveHourResetTime { get => _fiveHourResetTime; private set => SetProperty(ref _fiveHourResetTime, value); }
    public double? SevenDayPercentage { get => _sevenDayPercentage; private set => SetProperty(ref _sevenDayPercentage, value); }
    public string? SevenDayResetTime { get => _sevenDayResetTime; private set => SetProperty(ref _sevenDayResetTime, value); }
    public double? OpusPercentage { get => _opusPercentage; private set => SetProperty(ref _opusPercentage, value); }
    public double? SonnetPercentage { get => _sonnetPercentage; private set => SetProperty(ref _sonnetPercentage, value); }

    public bool HasExtraUsage { get => _hasExtraUsage; private set => SetProperty(ref _hasExtraUsage, value); }
    public double? ExtraUsagePercentage { get => _extraUsagePercentage; private set => SetProperty(ref _extraUsagePercentage, value); }
    public string ExtraUsageText { get => _extraUsageText; private set => SetProperty(ref _extraUsageText, value); }

    public bool IsRefreshing { get => _isRefreshing; private set => SetProperty(ref _isRefreshing, value); }
    public bool HasError { get => _hasError; private set => SetProperty(ref _hasError, value); }
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string AccountDisplayName { get => _accountDisplayName; private set => SetProperty(ref _accountDisplayName, value); }
    public bool HasAccounts { get => _hasAccounts; private set => SetProperty(ref _hasAccounts, value); }

    // Commands
    public ICommand RefreshCommand { get; }

    public MainViewModel(DataRefreshService refreshService, AccountManager accountManager, SmartMonitorService smartMonitor)
    {
        _refreshService = refreshService;
        _accountManager = accountManager;
        _smartMonitor = smartMonitor;

        RefreshCommand = new AsyncRelayCommand(async () => await _refreshService.ManualRefreshAsync());

        // Subscribe to service events
        _refreshService.UsageDataChanged += OnUsageDataChanged;
        _refreshService.ErrorChanged += OnErrorChanged;
        _refreshService.RefreshingChanged += OnRefreshingChanged;

        // Initial state
        UpdateAccountInfo();
    }

    private void OnUsageDataChanged(object? sender, UsageData? data)
    {
        if (data == null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
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
        AccountDisplayName = _accountManager.CurrentAccount?.DisplayName ?? "No account";
    }

    private static string FormatResetTime(DateTime? resetTime)
    {
        if (resetTime == null) return string.Empty;

        var remaining = resetTime.Value - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero) return "Resetting...";

        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
        if (remaining.TotalMinutes >= 1)
            return $"{(int)remaining.TotalMinutes}m";
        return "< 1m";
    }

    private static string FormatExtraUsage(ExtraUsageData? extra)
    {
        if (extra == null || !extra.Enabled) return string.Empty;

        var used = extra.Used ?? 0;
        var limit = extra.Limit ?? 0;
        return $"${used:F2} / ${limit:F2} {extra.Currency.ToUpperInvariant()}";
    }
}
