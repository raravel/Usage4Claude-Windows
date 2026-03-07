using System.Text.Json.Serialization;

namespace Usage4Claude.Models;

// GET /api/organizations/{orgId}/overage_spend_limit
public class ExtraUsageResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("spend_limit_currency")]
    public string SpendLimitCurrency { get; set; } = "usd";

    [JsonPropertyName("spend_limit_amount_cents")]
    public int? SpendLimitAmountCents { get; set; }

    [JsonPropertyName("balance_cents")]
    public int? BalanceCents { get; set; }
}
