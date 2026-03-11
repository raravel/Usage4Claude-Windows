using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Usage4Claude.Views.Tabs;

/// <summary>
/// About tab displaying app info, version, links, and developer credits.
/// Binds to SettingsViewModel which is inherited from the parent SettingsWindow DataContext.
/// </summary>
public partial class AboutTab : UserControl
{
    public AboutTab()
    {
        InitializeComponent();

        // Set version from assembly info
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
        {
            VersionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AboutTab] Failed to open URL '{url}': {ex.Message}");
        }
    }

    private void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/f-is-h/Usage4Claude");
    }

    private void OpenKoFi_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://ko-fi.com/1atte");
    }

    private void OpenSponsors_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/sponsors/f-is-h?frequency=one-time");
    }

    private void OpenClaudeUsage_Click(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://claude.ai/settings/usage");
    }
}
