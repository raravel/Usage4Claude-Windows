using System.Text.Json.Serialization;

namespace Usage4Claude.Models;

// GET /api/organizations/{orgId}/usage
public class UsageResponse
{
    [JsonPropertyName("five_hour")]
    public LimitUsage FiveHour { get; set; } = new();

    [JsonPropertyName("seven_day")]
    public LimitUsage? SevenDay { get; set; }

    [JsonPropertyName("seven_day_oauth_apps")]
    public LimitUsage? SevenDayOAuthApps { get; set; }

    [JsonPropertyName("seven_day_opus")]
    public LimitUsage? SevenDayOpus { get; set; }

    [JsonPropertyName("seven_day_sonnet")]
    public LimitUsage? SevenDaySonnet { get; set; }
}

public class LimitUsage
{
    [JsonPropertyName("utilization")]
    public double Utilization { get; set; }  // 0-100 percentage

    [JsonPropertyName("resets_at")]
    public string? ResetsAt { get; set; }  // ISO 8601 format
}
