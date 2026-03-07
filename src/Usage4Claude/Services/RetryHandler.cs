using System.Diagnostics;

namespace Usage4Claude.Services;

public static class RetryHandler
{
    /// <summary>
    /// Execute an async operation with retry logic for transient failures.
    /// </summary>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 2,
        TimeSpan? initialDelay = null,
        CancellationToken cancellationToken = default)
    {
        var delay = initialDelay ?? TimeSpan.FromSeconds(1);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Models.UsageError ex) when (IsTransient(ex) && attempt < maxRetries)
            {
                Debug.WriteLine($"[RetryHandler] Attempt {attempt + 1} failed ({ex.ErrorType}), retrying in {delay.TotalSeconds}s...");
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
        }

        // This shouldn't be reached, but just in case
        throw new Models.UsageError(Models.UsageErrorType.NetworkError, "All retry attempts exhausted");
    }

    private static bool IsTransient(Models.UsageError error) => error.ErrorType switch
    {
        Models.UsageErrorType.NetworkError => true,
        Models.UsageErrorType.RateLimited => true,
        Models.UsageErrorType.HttpError when error.StatusCode >= 500 => true,
        _ => false
    };
}
