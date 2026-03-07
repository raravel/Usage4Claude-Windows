namespace Usage4Claude.Models;

// Internal normalized model used by the app
public class UsageData
{
    public LimitData? FiveHour { get; set; }
    public LimitData? SevenDay { get; set; }
    public LimitData? Opus { get; set; }
    public LimitData? Sonnet { get; set; }
    public ExtraUsageData? ExtraUsage { get; set; }
}

public class LimitData
{
    public double Percentage { get; set; }  // 0-100
    public DateTime? ResetsAt { get; set; }
}

public class ExtraUsageData
{
    public bool Enabled { get; set; }
    public double? Used { get; set; }       // In USD
    public double? Limit { get; set; }      // In USD
    public string Currency { get; set; } = "usd";

    public double? Percentage => (Used.HasValue && Limit.HasValue && Limit.Value > 0)
        ? (Used.Value / Limit.Value) * 100.0
        : null;
}
