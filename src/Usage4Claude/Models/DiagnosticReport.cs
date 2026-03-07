using System.Text;

namespace Usage4Claude.Models;

public class DiagnosticReport
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Endpoint { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public string? ErrorType { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public bool IsSuccess { get; set; }
    public string? ResponsePreview { get; set; }  // First 200 chars of response for debugging

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Endpoint}");
        sb.AppendLine($"  Status: {(IsSuccess ? "OK" : "FAILED")} ({HttpStatusCode?.ToString() ?? "N/A"})");
        sb.AppendLine($"  Time: {ResponseTime.TotalMilliseconds:F0}ms");
        if (!IsSuccess)
        {
            sb.AppendLine($"  Error: {ErrorType} - {ErrorMessage}");
            if (!string.IsNullOrWhiteSpace(ResponsePreview))
                sb.AppendLine($"  Response: {ResponsePreview}");
        }
        return sb.ToString();
    }
}
