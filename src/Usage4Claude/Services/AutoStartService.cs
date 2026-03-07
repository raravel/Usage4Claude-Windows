using Microsoft.Win32;

namespace Usage4Claude.Services;

/// <summary>
/// Manages Windows auto-start via the Registry (HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run).
/// </summary>
public class AutoStartService
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "Usage4Claude";

    /// <summary>
    /// Check if auto-start is currently enabled in the Registry.
    /// </summary>
    public bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
        return key?.GetValue(AppName) != null;
    }

    /// <summary>
    /// Enable auto-start by adding a Registry entry pointing to the current executable.
    /// </summary>
    public void EnableAutoStart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
        if (key != null)
        {
            var exePath = Environment.ProcessPath
                          ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            key.SetValue(AppName, $"\"{exePath}\"");
        }
    }

    /// <summary>
    /// Disable auto-start by removing the Registry entry.
    /// </summary>
    public void DisableAutoStart()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
        key?.DeleteValue(AppName, false);
    }
}
