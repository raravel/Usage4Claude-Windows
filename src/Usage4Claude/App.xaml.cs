using System.Threading;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Usage4Claude.Services;

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
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Services (Singleton)
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ClaudeApiService>();
        services.AddSingleton<CredentialService>();
        services.AddSingleton<AccountManager>();
        // Future services to be registered as they are implemented:
        // services.AddSingleton<DataRefreshService>();
        // services.AddSingleton<NotificationService>();
        // services.AddSingleton<LocalizationService>();
        // services.AddSingleton<UpdateCheckService>();

        // ViewModels (Transient) - to be registered as they are implemented:
        // services.AddTransient<MainViewModel>();
        // services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }

    private void WireUpContextMenu()
    {
        if (_notifyIcon?.ContextMenu is ContextMenu contextMenu)
        {
            foreach (var item in contextMenu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Tag is string tag && tag == "Exit")
                {
                    menuItem.Click += ExitApplication_Click;
                }
            }
        }
    }

    private void ExitApplication_Click(object sender, RoutedEventArgs e)
    {
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
