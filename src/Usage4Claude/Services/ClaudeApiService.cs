using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Usage4Claude.Models;

namespace Usage4Claude.Services;

public class ClaudeApiService
{
    private const string BaseUrl = "https://claude.ai/api";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly List<DiagnosticReport> _recentReports = new();
    private const int MaxReportHistory = 50;

    /// <summary>
    /// Recent API call diagnostic reports for debugging.
    /// </summary>
    public IReadOnlyList<DiagnosticReport> RecentDiagnostics => _recentReports.AsReadOnly();

    public ClaudeApiService()
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Fetch usage data for the given account.
    /// </summary>
    public async Task<UsageData> FetchUsageAsync(Account account, CancellationToken cancellationToken = default)
    {
        // Validate credentials
        if (string.IsNullOrWhiteSpace(account.SessionKey) || string.IsNullOrWhiteSpace(account.OrganizationId))
            throw new UsageError(UsageErrorType.NoCredentials);

        // Fetch main usage with retry, and extra usage in parallel
        var usageTask = RetryHandler.ExecuteWithRetryAsync(
            ct => FetchMainUsageAsync(account, ct),
            maxRetries: 2,
            cancellationToken: cancellationToken);
        var extraTask = FetchExtraUsageAsync(account, cancellationToken);

        UsageResponse usageResponse;
        ExtraUsageResponse? extraResponse = null;

        try
        {
            usageResponse = await usageTask;
        }
        catch
        {
            throw; // Main usage failure is fatal
        }

        try
        {
            extraResponse = await extraTask;
        }
        catch (Exception ex)
        {
            // Extra usage failure is non-fatal
            Debug.WriteLine($"[ClaudeApiService] Extra usage fetch failed: {ex.Message}");
        }

        return ConvertToUsageData(usageResponse, extraResponse);
    }

    /// <summary>
    /// Fetch list of organizations for the given session key.
    /// </summary>
    public async Task<List<Organization>> FetchOrganizationsAsync(string sessionKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionKey))
            throw new UsageError(UsageErrorType.NoCredentials);

        var url = $"{BaseUrl}/organizations";
        var request = CreateRequest(HttpMethod.Get, url, sessionKey);
        var response = await ExecuteRequestAsync<List<Organization>>(request, cancellationToken);
        return response;
    }

    private async Task<UsageResponse> FetchMainUsageAsync(Account account, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/organizations/{account.OrganizationId}/usage";
        var request = CreateRequest(HttpMethod.Get, url, account.SessionKey);
        return await ExecuteRequestAsync<UsageResponse>(request, cancellationToken);
    }

    private async Task<ExtraUsageResponse?> FetchExtraUsageAsync(Account account, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/organizations/{account.OrganizationId}/overage_spend_limit";
        var request = CreateRequest(HttpMethod.Get, url, account.SessionKey);

        try
        {
            return await ExecuteRequestAsync<ExtraUsageResponse>(request, cancellationToken);
        }
        catch (UsageError ex) when (ex.StatusCode is 403 or 404)
        {
            // Extra usage not available for this account
            return null;
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string sessionKey)
    {
        var request = new HttpRequestMessage(method, url);

        // Browser simulation headers (critical for Cloudflare bypass)
        request.Headers.TryAddWithoutValidation("accept", "*/*");
        request.Headers.TryAddWithoutValidation("accept-language", "zh-CN,zh;q=0.9,en;q=0.8");
        request.Headers.TryAddWithoutValidation("content-type", "application/json");
        request.Headers.TryAddWithoutValidation("anthropic-client-platform", "web_claude_ai");
        request.Headers.TryAddWithoutValidation("anthropic-client-version", "1.0.0");
        request.Headers.TryAddWithoutValidation("user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("origin", "https://claude.ai");
        request.Headers.TryAddWithoutValidation("referer", "https://claude.ai/settings/usage");
        request.Headers.TryAddWithoutValidation("sec-fetch-dest", "empty");
        request.Headers.TryAddWithoutValidation("sec-fetch-mode", "cors");
        request.Headers.TryAddWithoutValidation("sec-fetch-site", "same-origin");

        // Session key as cookie
        request.Headers.TryAddWithoutValidation("Cookie", $"sessionKey={sessionKey}");

        return request;
    }

    private async Task<T> ExecuteRequestAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var endpoint = request.RequestUri?.PathAndQuery ?? "unknown";
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            RecordDiagnostic(endpoint, null, stopwatch.Elapsed, false,
                nameof(UsageErrorType.NetworkError), "Request timed out");
            throw new UsageError(UsageErrorType.NetworkError, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            RecordDiagnostic(endpoint, null, stopwatch.Elapsed, false,
                nameof(UsageErrorType.NetworkError), ex.Message);
            throw new UsageError(UsageErrorType.NetworkError, ex.Message);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        stopwatch.Stop();
        var responsePreview = content.Length > 200 ? content[..200] : content;

        // Check for Cloudflare HTML response
        if (content.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
            content.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
        {
            RecordDiagnostic(endpoint, (int)response.StatusCode, stopwatch.Elapsed, false,
                nameof(UsageErrorType.CloudflareBlocked), "Cloudflare HTML response detected", responsePreview);
            throw new UsageError(UsageErrorType.CloudflareBlocked, statusCode: (int)response.StatusCode);
        }

        // Check HTTP status
        if (!response.IsSuccessStatusCode)
        {
            // Try to parse error response
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, JsonOptions);
                if (errorResponse?.Error.Type == "permission_error")
                {
                    RecordDiagnostic(endpoint, (int)response.StatusCode, stopwatch.Elapsed, false,
                        nameof(UsageErrorType.SessionExpired), "Permission error", responsePreview);
                    throw new UsageError(UsageErrorType.SessionExpired);
                }
            }
            catch (JsonException) { /* Not a JSON error response */ }

            var statusCode = (int)response.StatusCode;
            var error = statusCode switch
            {
                401 => new UsageError(UsageErrorType.Unauthorized, statusCode: 401),
                403 => new UsageError(UsageErrorType.CloudflareBlocked, statusCode: 403),
                429 => new UsageError(UsageErrorType.RateLimited, statusCode: 429),
                _ => new UsageError(UsageErrorType.HttpError, $"HTTP {statusCode}", statusCode)
            };

            RecordDiagnostic(endpoint, statusCode, stopwatch.Elapsed, false,
                error.ErrorType.ToString(), error.Message, responsePreview);
            throw error;
        }

        // Deserialize response
        try
        {
            var result = JsonSerializer.Deserialize<T>(content, JsonOptions)
                         ?? throw new UsageError(UsageErrorType.NoData);
            RecordDiagnostic(endpoint, (int)response.StatusCode, stopwatch.Elapsed, true);
            return result;
        }
        catch (JsonException ex)
        {
            RecordDiagnostic(endpoint, (int)response.StatusCode, stopwatch.Elapsed, false,
                nameof(UsageErrorType.DecodingError), ex.Message, responsePreview);
            throw new UsageError(UsageErrorType.DecodingError, ex.Message);
        }
    }

    private void RecordDiagnostic(string endpoint, int? httpStatusCode, TimeSpan responseTime,
        bool isSuccess, string? errorType = null, string? errorMessage = null, string? responsePreview = null)
    {
        var report = new DiagnosticReport
        {
            Endpoint = endpoint,
            HttpStatusCode = httpStatusCode,
            ResponseTime = responseTime,
            IsSuccess = isSuccess,
            ErrorType = errorType,
            ErrorMessage = errorMessage,
            ResponsePreview = responsePreview
        };

        _recentReports.Add(report);

        // Trim old reports
        while (_recentReports.Count > MaxReportHistory)
            _recentReports.RemoveAt(0);

        Debug.WriteLine(report.ToString());
    }

    /// <summary>
    /// Clear all stored diagnostic reports.
    /// </summary>
    public void ClearDiagnostics() => _recentReports.Clear();

    private static UsageData ConvertToUsageData(UsageResponse usage, ExtraUsageResponse? extra)
    {
        return new UsageData
        {
            FiveHour = ConvertLimitUsage(usage.FiveHour),
            SevenDay = ConvertLimitUsage(usage.SevenDay),
            Opus = ConvertLimitUsage(usage.SevenDayOpus),
            Sonnet = ConvertLimitUsage(usage.SevenDaySonnet),
            ExtraUsage = ConvertExtraUsage(extra)
        };
    }

    private static LimitData? ConvertLimitUsage(LimitUsage? limit)
    {
        if (limit == null) return null;

        return new LimitData
        {
            Percentage = limit.Utilization,
            ResetsAt = ParseIso8601(limit.ResetsAt)
        };
    }

    private static ExtraUsageData? ConvertExtraUsage(ExtraUsageResponse? extra)
    {
        if (extra == null) return null;

        return new ExtraUsageData
        {
            Enabled = true,
            Used = extra.BalanceCents.HasValue ? extra.BalanceCents.Value / 100.0 : null,
            Limit = extra.SpendLimitAmountCents.HasValue ? extra.SpendLimitAmountCents.Value / 100.0 : null,
            Currency = extra.SpendLimitCurrency
        };
    }

    private static DateTime? ParseIso8601(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return null;

        if (DateTime.TryParse(dateString, null, System.Globalization.DateTimeStyles.RoundtripKind, out var result))
        {
            // Round to nearest second
            return new DateTime(result.Year, result.Month, result.Day,
                result.Hour, result.Minute, result.Second, result.Kind);
        }

        return null;
    }
}
