using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace Usage4Claude.Views;

/// <summary>
/// WebView2-based login window that opens the Claude.ai login page.
/// Monitors for the sessionKey cookie to detect successful authentication
/// via both cookie polling and navigation-based detection.
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
            StatusProgress.Visibility = Visibility.Collapsed;
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
        StatusProgress.Visibility = Visibility.Visible;

        // Set up user data folder so WebView2 state persists
        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Usage4Claude",
            "WebView2");

        var environment = await CoreWebView2Environment.CreateAsync(
            browserExecutableFolder: null,
            userDataFolder: userDataFolder);

        await WebView.EnsureCoreWebView2Async(environment);

        // Subscribe to navigation-based detection events
        WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
        WebView.CoreWebView2.SourceChanged += OnSourceChanged;

        StatusText.Text = "Loading login page...";

        // Navigate to Claude.ai login
        WebView.CoreWebView2.Navigate("https://claude.ai/login");

        // Start cookie polling timer as a fallback
        _cookieTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _cookieTimer.Tick += CookieTimer_Tick;
        _cookieTimer.Start();

        StatusText.Text = "Waiting for login...";
    }

    /// <summary>
    /// Triggered when a navigation completes. If the user has navigated to a non-login page,
    /// they have likely logged in -- check cookies immediately.
    /// </summary>
    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_sessionKeyFound || WebView?.CoreWebView2 == null) return;

        var uri = WebView.CoreWebView2.Source;
        if (IsLoggedInUrl(uri))
        {
            await CheckForSessionKeyCookieAsync();
        }
    }

    /// <summary>
    /// Triggered when the source URL changes. If it changed from a login URL to a main URL,
    /// trigger immediate cookie check.
    /// </summary>
    private async void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (_sessionKeyFound || WebView?.CoreWebView2 == null) return;

        var uri = WebView.CoreWebView2.Source;
        if (IsLoggedInUrl(uri))
        {
            await CheckForSessionKeyCookieAsync();
        }
    }

    /// <summary>
    /// Determines if a URL indicates the user has moved past the login page.
    /// </summary>
    private static bool IsLoggedInUrl(string? uri)
    {
        if (string.IsNullOrEmpty(uri)) return false;
        if (!uri.Contains("claude.ai")) return false;
        // If the URL does NOT contain login or oauth paths, the user has likely logged in
        return !uri.Contains("/login") && !uri.Contains("/oauth");
    }

    private async void CookieTimer_Tick(object? sender, EventArgs e)
    {
        await CheckForSessionKeyCookieAsync();
    }

    /// <summary>
    /// Reusable method to check cookies for the sessionKey.
    /// Both the timer and navigation handlers call this.
    /// Re-entrant safe via the _sessionKeyFound flag.
    /// </summary>
    private async Task CheckForSessionKeyCookieAsync()
    {
        if (_sessionKeyFound || WebView?.CoreWebView2 == null)
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
                    StatusProgress.Visibility = Visibility.Collapsed;

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

        // Unsubscribe from events
        if (WebView?.CoreWebView2 != null)
        {
            WebView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
            WebView.CoreWebView2.SourceChanged -= OnSourceChanged;
        }

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
