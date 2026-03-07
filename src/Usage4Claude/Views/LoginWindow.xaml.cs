using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace Usage4Claude.Views;

/// <summary>
/// WebView2-based login window that opens the Claude.ai login page.
/// Monitors for the sessionKey cookie to detect successful authentication.
/// </summary>
public partial class LoginWindow : Window
{
    private DispatcherTimer? _cookieTimer;
    private bool _sessionKeyFound;

    /// <summary>
    /// The extracted session key from the Claude.ai cookie, available after successful login.
    /// </summary>
    public string? ExtractedSessionKey { get; private set; }

    /// <summary>
    /// Fires when the session key is successfully extracted from cookies.
    /// </summary>
    public event EventHandler<string>? SessionKeyExtracted;

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += LoginWindow_Loaded;
        Closing += LoginWindow_Closing;
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await InitializeWebView2Async();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoginWindow] WebView2 initialization failed: {ex}");
            StatusText.Text = $"Error: {ex.Message}";
            MessageBox.Show(
                $"Failed to initialize the browser component.\n\n{ex.Message}\n\nPlease ensure WebView2 Runtime is installed.",
                "Login Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task InitializeWebView2Async()
    {
        StatusText.Text = "Initializing browser...";

        // Set up user data folder so WebView2 state persists
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Usage4Claude",
            "WebView2");

        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder);

        await WebView.EnsureCoreWebView2Async(environment);

        StatusText.Text = "Loading login page...";

        // Navigate to Claude.ai login
        WebView.CoreWebView2.Navigate("https://claude.ai/login");

        // Start cookie polling timer
        _cookieTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _cookieTimer.Tick += CookieTimer_Tick;
        _cookieTimer.Start();

        StatusText.Text = "Waiting for login...";
    }

    private async void CookieTimer_Tick(object? sender, EventArgs e)
    {
        if (_sessionKeyFound || WebView.CoreWebView2 == null)
            return;

        try
        {
            var cookies = await WebView.CoreWebView2.CookieManager
                .GetCookiesAsync("https://claude.ai");

            foreach (var cookie in cookies)
            {
                if (cookie.Name == "sessionKey" && !string.IsNullOrEmpty(cookie.Value))
                {
                    _sessionKeyFound = true;
                    _cookieTimer?.Stop();

                    ExtractedSessionKey = cookie.Value;

                    StatusText.Text = "Login detected! Fetching organizations...";

                    // Delete the sessionKey cookie from WebView2 storage for security
                    WebView.CoreWebView2.CookieManager.DeleteCookie(cookie);

                    // Fire event
                    SessionKeyExtracted?.Invoke(this, ExtractedSessionKey);

                    // Close as successful dialog
                    DialogResult = true;
                    Close();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoginWindow] Cookie check failed: {ex.Message}");
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void LoginWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Stop the timer
        _cookieTimer?.Stop();
        _cookieTimer = null;

        // Dispose WebView2
        try
        {
            WebView?.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoginWindow] WebView2 dispose error: {ex.Message}");
        }
    }
}
