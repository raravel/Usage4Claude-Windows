using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Usage4Claude.Models;
using Usage4Claude.Services;

namespace Usage4Claude.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly IconManager _iconManager;
    private readonly AccountManager _accountManager;
    private readonly ClaudeApiService _claudeApiService;
    private readonly AutoStartService _autoStartService;

    public SettingsViewModel(
        SettingsService settingsService,
        IconManager iconManager,
        AccountManager accountManager,
        ClaudeApiService claudeApiService,
        AutoStartService autoStartService)
    {
        _settingsService = settingsService;
        _iconManager = iconManager;
        _accountManager = accountManager;
        _claudeApiService = claudeApiService;
        _autoStartService = autoStartService;

        // Sync the persisted setting with actual registry state on init
        try
        {
            _settingsService.Settings.LaunchAtLogin = _autoStartService.IsAutoStartEnabled();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Settings] Failed to read auto-start state: {ex.Message}");
        }

        // Initialize commands
        AddAccountCommand = new RelayCommand(ExecuteAddAccount, CanExecuteAddAccount);
        RemoveAccountCommand = new RelayCommand(ExecuteRemoveAccount, CanExecuteRemoveAccount);
        SetCurrentAccountCommand = new RelayCommand(ExecuteSetCurrentAccount, CanExecuteSetCurrentAccount);
        TestConnectionCommand = new AsyncRelayCommand(ExecuteTestConnectionAsync, CanExecuteTestConnection);
        SaveAccountChangesCommand = new RelayCommand(ExecuteSaveAccountChanges, CanExecuteSaveAccountChanges);

        // Update check commands
        CheckForUpdateCommand = new AsyncRelayCommand(ExecuteCheckForUpdate);
        OpenUpdateUrlCommand = new RelayCommand(ExecuteOpenUpdateUrl);

        // Load initial accounts
        RefreshAccountsList();
    }

    // --- Display settings ---

    public IconDisplayMode IconDisplayMode
    {
        get => _settingsService.Settings.IconDisplayMode;
        set
        {
            if (_settingsService.Settings.IconDisplayMode != value)
            {
                _settingsService.Settings.IconDisplayMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowIcon));
                OnPropertyChanged(nameof(ShowPercentage));
                Save();
                _iconManager.RefreshIcon();
            }
        }
    }

    public IconStyleMode IconStyleMode
    {
        get => _settingsService.Settings.IconStyleMode;
        set
        {
            if (_settingsService.Settings.IconStyleMode != value)
            {
                _settingsService.Settings.IconStyleMode = value;
                OnPropertyChanged();
                Save();
                _iconManager.RefreshIcon();
            }
        }
    }

    public DisplayMode DisplayMode
    {
        get => _settingsService.Settings.DisplayMode;
        set
        {
            if (_settingsService.Settings.DisplayMode != value)
            {
                _settingsService.Settings.DisplayMode = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public List<LimitType> CustomDisplayTypes
    {
        get => _settingsService.Settings.CustomDisplayTypes;
        set
        {
            if (_settingsService.Settings.CustomDisplayTypes.SequenceEqual(value)) return;
            _settingsService.Settings.CustomDisplayTypes = value;
            OnPropertyChanged();
            Save();
        }
    }

    // --- Individual LimitType checkbox bindings ---

    public bool ShowFiveHour
    {
        get => CustomDisplayTypes.Contains(LimitType.FiveHour);
        set => ToggleLimitType(LimitType.FiveHour, value);
    }

    public bool ShowSevenDay
    {
        get => CustomDisplayTypes.Contains(LimitType.SevenDay);
        set => ToggleLimitType(LimitType.SevenDay, value);
    }

    public bool ShowExtraUsage
    {
        get => CustomDisplayTypes.Contains(LimitType.ExtraUsage);
        set => ToggleLimitType(LimitType.ExtraUsage, value);
    }

    public bool ShowOpusWeekly
    {
        get => CustomDisplayTypes.Contains(LimitType.OpusWeekly);
        set => ToggleLimitType(LimitType.OpusWeekly, value);
    }

    public bool ShowSonnetWeekly
    {
        get => CustomDisplayTypes.Contains(LimitType.SonnetWeekly);
        set => ToggleLimitType(LimitType.SonnetWeekly, value);
    }

    private void ToggleLimitType(LimitType type, bool include)
    {
        var types = new List<LimitType>(CustomDisplayTypes);
        if (include && !types.Contains(type))
            types.Add(type);
        else if (!include && types.Contains(type) && types.Count > 1)
            types.Remove(type);
        CustomDisplayTypes = types;
        OnPropertyChanged(nameof(ShowFiveHour));
        OnPropertyChanged(nameof(ShowSevenDay));
        OnPropertyChanged(nameof(ShowExtraUsage));
        OnPropertyChanged(nameof(ShowOpusWeekly));
        OnPropertyChanged(nameof(ShowSonnetWeekly));
    }

    // --- Refresh settings ---

    public RefreshMode RefreshMode
    {
        get => _settingsService.Settings.RefreshMode;
        set
        {
            if (_settingsService.Settings.RefreshMode != value)
            {
                _settingsService.Settings.RefreshMode = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public int RefreshIntervalSeconds
    {
        get => _settingsService.Settings.RefreshIntervalSeconds;
        set
        {
            if (_settingsService.Settings.RefreshIntervalSeconds != value)
            {
                _settingsService.Settings.RefreshIntervalSeconds = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    // --- Appearance settings ---

    public AppAppearance Appearance
    {
        get => _settingsService.Settings.Appearance;
        set
        {
            if (_settingsService.Settings.Appearance != value)
            {
                _settingsService.Settings.Appearance = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public TimeFormatPreference TimeFormat
    {
        get => _settingsService.Settings.TimeFormat;
        set
        {
            if (_settingsService.Settings.TimeFormat != value)
            {
                _settingsService.Settings.TimeFormat = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public AppLanguage Language
    {
        get => _settingsService.Settings.Language;
        set
        {
            if (_settingsService.Settings.Language != value)
            {
                _settingsService.Settings.Language = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    // --- System settings ---

    public bool LaunchAtLogin
    {
        get => _settingsService.Settings.LaunchAtLogin;
        set
        {
            if (_settingsService.Settings.LaunchAtLogin == value) return;

            try
            {
                if (value)
                    _autoStartService.EnableAutoStart();
                else
                    _autoStartService.DisableAutoStart();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsViewModel] Failed to update auto-start: {ex.Message}");
                return;
            }

            _settingsService.Settings.LaunchAtLogin = value;
            OnPropertyChanged();
            Save();
        }
    }

    public bool NotificationsEnabled
    {
        get => _settingsService.Settings.NotificationsEnabled;
        set
        {
            if (_settingsService.Settings.NotificationsEnabled != value)
            {
                _settingsService.Settings.NotificationsEnabled = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    // --- Helper checkbox bindings for display content ---

    /// <summary>
    /// Whether the icon ring is shown in the tray icon.
    /// Combined with ShowPercentage to compute the effective IconDisplayMode.
    /// </summary>
    public bool ShowIcon
    {
        get => IconDisplayMode is IconDisplayMode.IconOnly or IconDisplayMode.Both;
        set
        {
            var showPercentage = ShowPercentage;
            IconDisplayMode = (value, showPercentage) switch
            {
                (true, true) => IconDisplayMode.Both,
                (true, false) => IconDisplayMode.IconOnly,
                (false, true) => IconDisplayMode.PercentageOnly,
                // At least one must be selected; default to percentage if both unchecked
                (false, false) => IconDisplayMode.PercentageOnly
            };
        }
    }

    /// <summary>
    /// Whether the percentage text is shown in the tray icon.
    /// Combined with ShowIcon to compute the effective IconDisplayMode.
    /// </summary>
    public bool ShowPercentage
    {
        get => IconDisplayMode is IconDisplayMode.PercentageOnly or IconDisplayMode.Both;
        set
        {
            var showIcon = ShowIcon;
            IconDisplayMode = (showIcon, value) switch
            {
                (true, true) => IconDisplayMode.Both,
                (true, false) => IconDisplayMode.IconOnly,
                (false, true) => IconDisplayMode.PercentageOnly,
                // At least one must be selected; default to icon if both unchecked
                (false, false) => IconDisplayMode.IconOnly
            };
        }
    }

    // --- Refresh interval options for ComboBox binding ---

    public List<RefreshIntervalOption> RefreshIntervalOptions { get; } = new()
    {
        new(60, "1 min"),
        new(120, "2 min"),
        new(180, "3 min"),
        new(300, "5 min"),
        new(600, "10 min")
    };

    // =====================================================
    // Auth tab: Account management
    // =====================================================

    /// <summary>
    /// Observable collection of account display items for the ListBox.
    /// </summary>
    public ObservableCollection<AccountDisplayItem> Accounts { get; } = new();

    private AccountDisplayItem? _selectedAccount;
    /// <summary>
    /// Currently selected account in the list.
    /// When changed, populates the edit fields.
    /// </summary>
    public AccountDisplayItem? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (SetProperty(ref _selectedAccount, value))
            {
                OnPropertyChanged(nameof(HasSelectedAccount));
                (RemoveAccountCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (SetCurrentAccountCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (TestConnectionCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                PopulateEditFields();
            }
        }
    }

    /// <summary>Whether an account is selected in the list.</summary>
    public bool HasSelectedAccount => SelectedAccount != null;

    // --- Edit fields (populated when an account is selected) ---

    private string _editDisplayName = string.Empty;
    public string EditDisplayName
    {
        get => _editDisplayName;
        set => SetProperty(ref _editDisplayName, value);
    }

    private string _editOrgId = string.Empty;
    public string EditOrgId
    {
        get => _editOrgId;
        set => SetProperty(ref _editOrgId, value);
    }

    private string _editSessionKey = string.Empty;
    public string EditSessionKey
    {
        get => _editSessionKey;
        set => SetProperty(ref _editSessionKey, value);
    }

    // --- Manual entry fields ---

    private string _newOrgId = string.Empty;
    public string NewOrgId
    {
        get => _newOrgId;
        set
        {
            if (SetProperty(ref _newOrgId, value))
                (AddAccountCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private string _newSessionKey = string.Empty;
    public string NewSessionKey
    {
        get => _newSessionKey;
        set
        {
            if (SetProperty(ref _newSessionKey, value))
                (AddAccountCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private string _newDisplayName = string.Empty;
    public string NewDisplayName
    {
        get => _newDisplayName;
        set => SetProperty(ref _newDisplayName, value);
    }

    // --- Diagnostic ---

    private string _diagnosticResult = string.Empty;
    public string DiagnosticResult
    {
        get => _diagnosticResult;
        set => SetProperty(ref _diagnosticResult, value);
    }

    private bool _isDiagnosticRunning;
    public bool IsDiagnosticRunning
    {
        get => _isDiagnosticRunning;
        set => SetProperty(ref _isDiagnosticRunning, value);
    }

    private bool? _diagnosticSuccess;
    /// <summary>
    /// Nullable bool for diagnostic result coloring.
    /// True = green, False = red, null = default.
    /// </summary>
    public bool? DiagnosticSuccess
    {
        get => _diagnosticSuccess;
        set => SetProperty(ref _diagnosticSuccess, value);
    }

    // --- Commands ---

    public ICommand AddAccountCommand { get; }
    public ICommand RemoveAccountCommand { get; }
    public ICommand SetCurrentAccountCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand SaveAccountChangesCommand { get; }

    // --- Command implementations ---

    private bool CanExecuteAddAccount() =>
        !string.IsNullOrWhiteSpace(NewOrgId) && !string.IsNullOrWhiteSpace(NewSessionKey);

    private void ExecuteAddAccount()
    {
        var orgId = NewOrgId.Trim();
        var sessionKey = NewSessionKey.Trim();
        var displayName = string.IsNullOrWhiteSpace(NewDisplayName) ? null : NewDisplayName.Trim();

        var success = _accountManager.AddAccount(sessionKey, orgId, displayName ?? orgId, displayName);
        if (success)
        {
            NewOrgId = string.Empty;
            NewSessionKey = string.Empty;
            NewDisplayName = string.Empty;
            RefreshAccountsList();
        }
        else
        {
            MessageBox.Show("Failed to add account. Please check your input and try again.",
                "Add Account", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool CanExecuteRemoveAccount() => SelectedAccount != null;

    private void ExecuteRemoveAccount()
    {
        if (SelectedAccount == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to remove the account \"{SelectedAccount.DisplayName}\"?",
            "Remove Account",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        var success = _accountManager.RemoveAccount(SelectedAccount.Id);
        if (success)
        {
            SelectedAccount = null;
            RefreshAccountsList();
        }
        else
        {
            MessageBox.Show("Failed to remove account.",
                "Remove Account", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool CanExecuteSetCurrentAccount() =>
        SelectedAccount != null && !SelectedAccount.IsCurrent;

    private void ExecuteSetCurrentAccount()
    {
        if (SelectedAccount == null) return;

        var success = _accountManager.SwitchAccount(SelectedAccount.Id);
        if (success)
        {
            RefreshAccountsList();
        }
    }

    private bool CanExecuteTestConnection() => !IsDiagnosticRunning;

    private async Task ExecuteTestConnectionAsync()
    {
        var currentAccount = _accountManager.CurrentAccount;
        if (currentAccount == null)
        {
            DiagnosticResult = "No active account configured. Please add an account first.";
            DiagnosticSuccess = false;
            return;
        }

        IsDiagnosticRunning = true;
        DiagnosticResult = string.Empty;
        DiagnosticSuccess = null;

        try
        {
            var organizations = await _claudeApiService.FetchOrganizationsAsync(currentAccount.SessionKey);
            if (organizations.Count > 0)
            {
                var orgNames = string.Join(", ", organizations.Select(o => o.Name));
                DiagnosticResult = $"Connection successful!\nOrganizations: {orgNames}";
                DiagnosticSuccess = true;
            }
            else
            {
                DiagnosticResult = "Connection successful, but no organizations found.";
                DiagnosticSuccess = true;
            }
        }
        catch (UsageError ex)
        {
            DiagnosticResult = $"Connection failed: {ex.Message}";
            DiagnosticSuccess = false;
            Debug.WriteLine($"[SettingsViewModel] Connection test failed: {ex}");
        }
        catch (Exception ex)
        {
            DiagnosticResult = $"Unexpected error: {ex.Message}";
            DiagnosticSuccess = false;
            Debug.WriteLine($"[SettingsViewModel] Connection test unexpected error: {ex}");
        }
        finally
        {
            IsDiagnosticRunning = false;
        }
    }

    private bool CanExecuteSaveAccountChanges() => SelectedAccount != null;

    private void ExecuteSaveAccountChanges()
    {
        if (SelectedAccount == null) return;

        var account = SelectedAccount.Account;

        // Update alias
        var newAlias = string.IsNullOrWhiteSpace(EditDisplayName) ? null : EditDisplayName.Trim();
        _accountManager.UpdateAlias(account.Id, newAlias);

        // Update session key if changed
        var newSessionKey = EditSessionKey.Trim();
        if (!string.IsNullOrEmpty(newSessionKey) && newSessionKey != account.SessionKey)
        {
            // Re-add the account with the new session key (AddAccount handles updates for same org ID)
            _accountManager.AddAccount(newSessionKey, account.OrganizationId, account.OrganizationName, newAlias);
        }

        RefreshAccountsList();
    }

    // --- Helpers ---

    /// <summary>
    /// Populate edit fields from the currently selected account.
    /// </summary>
    private void PopulateEditFields()
    {
        if (SelectedAccount == null)
        {
            EditDisplayName = string.Empty;
            EditOrgId = string.Empty;
            EditSessionKey = string.Empty;
            return;
        }

        EditDisplayName = SelectedAccount.Alias ?? string.Empty;
        EditOrgId = SelectedAccount.OrganizationId;
        EditSessionKey = SelectedAccount.SessionKey;
    }

    /// <summary>
    /// Reload the accounts list from AccountManager.
    /// </summary>
    private void RefreshAccountsList()
    {
        var currentId = _accountManager.CurrentAccount?.Id;
        var previousSelectedId = SelectedAccount?.Id;

        Accounts.Clear();
        foreach (var account in _accountManager.Accounts)
        {
            var isCurrent = account.Id == currentId;
            Accounts.Add(new AccountDisplayItem(account, isCurrent));
        }

        // Restore selection if possible
        if (previousSelectedId != null)
        {
            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == previousSelectedId);
        }
    }

    private void Save()
    {
        _settingsService.Save();
    }

    // =====================================================
    // About tab: Update check
    // =====================================================

    private string _updateStatusText = string.Empty;
    public string UpdateStatusText
    {
        get => _updateStatusText;
        private set => SetProperty(ref _updateStatusText, value);
    }

    private bool _isUpdateAvailable;
    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set => SetProperty(ref _isUpdateAvailable, value);
    }

    private string _updateUrl = string.Empty;
    public string UpdateUrl
    {
        get => _updateUrl;
        private set => SetProperty(ref _updateUrl, value);
    }

    private bool _isCheckingForUpdate;
    public bool IsCheckingForUpdate
    {
        get => _isCheckingForUpdate;
        private set => SetProperty(ref _isCheckingForUpdate, value);
    }

    public ICommand CheckForUpdateCommand { get; }
    public ICommand OpenUpdateUrlCommand { get; }

    private async Task ExecuteCheckForUpdate()
    {
        IsCheckingForUpdate = true;
        UpdateStatusText = "Checking for updates...";

        try
        {
            var updateService = App.Current.Services.GetRequiredService<UpdateCheckService>();
            var result = await updateService.CheckForUpdateAsync(forceCheck: true);

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                UpdateStatusText = $"Check failed: {result.ErrorMessage}";
                IsUpdateAvailable = false;
            }
            else if (result.IsUpToDate)
            {
                UpdateStatusText = $"You're up to date! (v{result.CurrentVersion})";
                IsUpdateAvailable = false;
            }
            else
            {
                UpdateStatusText = $"Update available: v{result.LatestVersion}";
                UpdateUrl = result.ReleaseUrl ?? "";
                IsUpdateAvailable = true;
            }
        }
        finally
        {
            IsCheckingForUpdate = false;
        }
    }

    private void ExecuteOpenUpdateUrl()
    {
        if (string.IsNullOrEmpty(UpdateUrl)) return;

        // Validate URL is a legitimate HTTPS GitHub URL to prevent injection
        if (!Uri.TryCreate(UpdateUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = UpdateUrl,
            UseShellExecute = true
        });
    }
}

public record RefreshIntervalOption(int Seconds, string DisplayName);
