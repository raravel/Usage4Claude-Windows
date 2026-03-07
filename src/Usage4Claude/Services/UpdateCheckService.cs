using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace Usage4Claude.Services;

/// <summary>
/// Checks for application updates by querying GitHub releases.
/// </summary>
public class UpdateCheckService
{
    private const string GitHubApiUrl = "https://api.github.com/repos/raravel/Usage4Claude-Windows/releases/latest";

    private static readonly HttpClient _httpClient = new();

    // Cache the result to avoid hammering the API
    private UpdateCheckResult? _lastResult;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private static readonly TimeSpan CheckCooldown = TimeSpan.FromMinutes(30);

    static UpdateCheckService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Usage4Claude-Windows");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// Get the current application version (cached for performance).
    /// </summary>
    private static readonly Lazy<string> _currentVersion = new(() =>
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    });

    public static string CurrentVersion => _currentVersion.Value;

    /// <summary>
    /// Check for updates. Returns cached result if checked recently.
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdateAsync(bool forceCheck = false)
    {
        // Return cached result if within cooldown
        if (!forceCheck && _lastResult != null && (DateTime.UtcNow - _lastCheckTime) < CheckCooldown)
        {
            return _lastResult;
        }

        try
        {
            var response = await _httpClient.GetStringAsync(GitHubApiUrl);
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);

            if (release == null || string.IsNullOrEmpty(release.TagName))
            {
                _lastResult = new UpdateCheckResult { IsUpToDate = true, CurrentVersion = CurrentVersion };
                _lastCheckTime = DateTime.UtcNow;
                return _lastResult;
            }

            // Parse version from tag (e.g., "v1.2.3" or "v1.2.3-beta" -> "1.2.3")
            var latestVersionStr = release.TagName.TrimStart('v', 'V');
            // Strip prerelease suffix for comparison (e.g., "1.2.3-beta.1" -> "1.2.3")
            var dashIndex = latestVersionStr.IndexOf('-');
            if (dashIndex >= 0)
                latestVersionStr = latestVersionStr[..dashIndex];
            var isNewer = IsNewerVersion(latestVersionStr, CurrentVersion);

            _lastResult = new UpdateCheckResult
            {
                IsUpToDate = !isNewer,
                CurrentVersion = CurrentVersion,
                LatestVersion = latestVersionStr,
                ReleaseUrl = release.HtmlUrl ?? "https://github.com/raravel/Usage4Claude-Windows/releases/latest",
                ReleaseNotes = release.Body,
                PublishedAt = release.PublishedAt
            };
            _lastCheckTime = DateTime.UtcNow;
            return _lastResult;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UpdateCheckService] Check failed: {ex.Message}");
            return new UpdateCheckResult
            {
                IsUpToDate = true, // Assume up to date on error
                CurrentVersion = CurrentVersion,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Compare two version strings. Returns true if latest > current.
    /// </summary>
    private static bool IsNewerVersion(string latest, string current)
    {
        if (Version.TryParse(latest, out var latestVer) && Version.TryParse(current, out var currentVer))
        {
            return latestVer > currentVer;
        }
        return false;
    }
}

/// <summary>
/// Result of an update check.
/// </summary>
public class UpdateCheckResult
{
    public bool IsUpToDate { get; init; }
    public string CurrentVersion { get; init; } = string.Empty;
    public string? LatestVersion { get; init; }
    public string? ReleaseUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public string? PublishedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Minimal GitHub release response model.
/// </summary>
internal class GitHubRelease
{
    [System.Text.Json.Serialization.JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("body")]
    public string? Body { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("published_at")]
    public string? PublishedAt { get; set; }
}
