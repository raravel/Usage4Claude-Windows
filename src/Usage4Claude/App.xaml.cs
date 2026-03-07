using System.Threading;
using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;

namespace Usage4Claude;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex;
    private TaskbarIcon? _notifyIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single instance check
        const string mutexName = "Usage4Claude-Windows-SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            MessageBox.Show("Usage4Claude is already running.", "Usage4Claude",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Initialize system tray icon
        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

        // Wire up the exit menu item via code-behind
        if (_notifyIcon.ContextMenu is ContextMenu contextMenu)
        {
            foreach (var item in contextMenu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Header is string header && header == "종료")
                {
                    menuItem.Click += ExitApplication_Click;
                }
            }
        }

        _notifyIcon.ForceCreate(enablesEfficiencyMode: false);
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
