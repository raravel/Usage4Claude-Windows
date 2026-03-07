using System.Threading;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Usage4Claude.Services;
using Usage4Claude.ViewModels;
using Usage4Claude.Views;

namespace Usage4Claude;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Typed accessor for the current Application instance.
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// The application-wide DI service provider.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    private static Mutex? _mutex;
    private TaskbarIcon? _notifyIcon;
    private PopupWindow? _activePopup;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single instance check
        const string mutexName = "Usage4Claude-Windows-SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            _mutex.Dispose();
            _mutex = null;
            MessageBox.Show("Usage4Claude is already running.", "Usage4Claude",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Configure DI container
        Services = ConfigureServices();

        // Initialize system tray icon
        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

        // Wire up the exit menu item via code-behind
        WireUpContextMenu();

        _notifyIcon.ForceCreate(enablesEfficiencyMode: false);

        // Initialize the icon manager to handle dynamic tray icon updates
        var iconManager = Services.GetRequiredService<IconManager>();
        iconManager.Initialize(_notifyIcon);

        // Start periodic data refresh
        var refreshService = Services.GetRequiredService<DataRefreshService>();
        refreshService.Start();

        // Wire up left-click on tray icon to show the popup window
        _notifyIcon.TrayLeftMouseDown += (_, _) => ShowPopupWindow();
    }

    private void ShowPopupWindow()
    {
        // Close existing popup if open (toggle behavior)
        if (_activePopup is { IsLoaded: true })
        {
            _activePopup.Close();
            _activePopup = null;
            return;
        }

        var viewModel = Services.GetRequiredService<MainViewModel>();

        // Trigger a smart refresh (throttled to 30s minimum)
        var refreshService = Services.GetRequiredService<DataRefreshService>();
        _ = refreshService.RefreshOnPopoverOpenAsync();

        _activePopup = new PopupWindow { DataContext = viewModel };
        _activePopup.Show();
        _activePopup.PositionNearTray();
        _activePopup.Activate(); // Ensure focus so Deactivated event works
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services (Singleton)
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ClaudeApiService>();
        services.AddSingleton<CredentialService>();
        services.AddSingleton<AccountManager>();
        services.AddSingleton<SmartMonitorService>();
        services.AddSingleton<DataRefreshService>();
        services.AddSingleton<IconManager>();
        services.AddSingleton<AutoStartService>();
        services.AddSingleton<LoginService>();
        // Future services to be registered as they are implemented:
        // services.AddSingleton<NotificationService>();
        // services.AddSingleton<LocalizationService>();
        // services.AddSingleton<UpdateCheckService>();

        // ViewModels (Singleton - subscribes to service events)
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    private void WireUpContextMenu()
    {
        if (_notifyIcon?.ContextMenu is not ContextMenu contextMenu) return;

        foreach (var item in contextMenu.Items)
        {
            if (item is MenuItem menuItem && menuItem.Tag is string tag)
            {
                switch (tag)
                {
                    case "Exit":
                        menuItem.Click += (_, _) => Shutdown();
                        break;
                    case "Refresh":
                        menuItem.Click += async (_, _) =>
                        {
                            var refreshService = Services.GetService<DataRefreshService>();
                            if (refreshService != null)
                                await refreshService.ManualRefreshAsync();
                        };
                        break;
                    case "Settings":
                        menuItem.Click += (_, _) => ShowSettingsWindow();
                        break;
                }
            }
        }
    }

    private void ShowSettingsWindow(int tabIndex = 0)
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.NavigateToTab(tabIndex);
            _settingsWindow.Activate();
            return;
        }

        var viewModel = Services.GetRequiredService<SettingsViewModel>();
        _settingsWindow = new SettingsWindow { DataContext = viewModel };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.NavigateToTab(tabIndex);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Stop background services
        Services.GetService<DataRefreshService>()?.Stop();
        Services.GetService<IconManager>()?.Dispose();

        _notifyIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
