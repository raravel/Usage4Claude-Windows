namespace Usage4Claude.Models;

public enum UsageErrorType
{
    InvalidUrl,
    NoData,
    SessionExpired,
    CloudflareBlocked,
    NoCredentials,
    NetworkError,
    DecodingError,
    Unauthorized,
    RateLimited,
    HttpError
}

public class UsageError : Exception
{
    public UsageErrorType ErrorType { get; }
    public int? StatusCode { get; }

    public UsageError(UsageErrorType errorType, string? message = null, int? statusCode = null)
        : base(message ?? GetDefaultMessage(errorType))
    {
        ErrorType = errorType;
        StatusCode = statusCode;
    }

    private static string GetDefaultMessage(UsageErrorType errorType) => errorType switch
    {
        UsageErrorType.InvalidUrl => "Invalid API URL",
        UsageErrorType.NoData => "No data received from API",
        UsageErrorType.SessionExpired => "Session has expired. Please re-authenticate.",
        UsageErrorType.CloudflareBlocked => "Request blocked by Cloudflare protection",
        UsageErrorType.NoCredentials => "No credentials configured",
        UsageErrorType.NetworkError => "Network connection failed",
        UsageErrorType.DecodingError => "Failed to parse API response",
        UsageErrorType.Unauthorized => "Unauthorized - invalid session key",
        UsageErrorType.RateLimited => "Too many requests - please wait",
        UsageErrorType.HttpError => "HTTP error occurred",
        _ => "Unknown error"
    };
}
