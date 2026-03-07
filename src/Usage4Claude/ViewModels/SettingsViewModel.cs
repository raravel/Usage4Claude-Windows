using Usage4Claude.Models;
using Usage4Claude.Services;

namespace Usage4Claude.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly IconManager _iconManager;

    public SettingsViewModel(SettingsService settingsService, IconManager iconManager)
    {
        _settingsService = settingsService;
        _iconManager = iconManager;
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
            _settingsService.Settings.CustomDisplayTypes = value;
            OnPropertyChanged();
            Save();
        }
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
            if (_settingsService.Settings.LaunchAtLogin != value)
            {
                _settingsService.Settings.LaunchAtLogin = value;
                OnPropertyChanged();
                Save();
            }
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

    private void Save()
    {
        _settingsService.Save();
    }
}

public record RefreshIntervalOption(int Seconds, string DisplayName);
