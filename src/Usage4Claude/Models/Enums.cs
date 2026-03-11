using System.Text.Json.Serialization;

namespace Usage4Claude.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IconDisplayMode
{
    PercentageOnly,
    IconOnly,
    Both
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IconStyleMode
{
    ColorTranslucent,
    ColorWithBackground,
    Monochrome
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RefreshMode
{
    Smart,
    Fixed
}

// Used by data refresh logic in task 03 (monitoring interval adaptation)
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MonitoringMode
{
    Active,
    IdleShort,
    IdleMedium,
    IdleLong
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LimitType
{
    FiveHour,
    SevenDay,
    ExtraUsage,
    OpusWeekly,
    SonnetWeekly
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisplayMode
{
    Smart,
    Custom
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimeFormatPreference
{
    System,
    TwelveHour,
    TwentyFourHour
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppAppearance
{
    System,
    Light,
    Dark
}

// Locale mapping: English=en, Japanese=ja, Chinese=zh-Hans, ChineseTraditional=zh-Hant, Korean=ko
// Language switching is handled by LocalizationService (task 08-00)
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppLanguage
{
    English,
    Japanese,
    Chinese,
    ChineseTraditional,
    Korean
}
