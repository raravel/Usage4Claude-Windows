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
            // REVIEW: _mutex is assigned above but this process does NOT own it.
            // When Shutdown() triggers OnExit(), _mutex.ReleaseMutex() will throw
            // ApplicationException ("Object synchronization method was called from
            // an unsynchronized block of code.").
            // Fix: set _mutex = null here before calling Shutdown(), or guard
            // ReleaseMutex() in OnExit with a flag indicating ownership.
            MessageBox.Show("Usage4Claude is already running.", "Usage4Claude",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Initialize system tray icon
        _notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

        // REVIEW: [Minor] Wiring by Header string match is fragile. If the menu text
        // changes, this silently breaks. Consider using Tag="Exit" in XAML and matching
        // on Tag, or using a Command/ICommand binding pattern instead.
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
        // REVIEW: ReleaseMutex() is redundant when followed by Dispose() -- Dispose()
        // already releases the mutex. Also, if the second instance reaches here,
        // ReleaseMutex() will throw ApplicationException because this process does
        // not own the mutex. Consider removing ReleaseMutex() and relying on Dispose().
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
