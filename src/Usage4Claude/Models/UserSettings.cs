namespace Usage4Claude.Models;

public class UserSettings
{
    // 표시 설정
    public IconDisplayMode IconDisplayMode { get; set; } = IconDisplayMode.PercentageOnly;
    public IconStyleMode IconStyleMode { get; set; } = IconStyleMode.ColorTranslucent;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Smart;
    public List<LimitType> CustomDisplayTypes { get; set; } = new() { LimitType.FiveHour, LimitType.SevenDay };

    // 새로고침 설정
    public RefreshMode RefreshMode { get; set; } = RefreshMode.Smart;
    public int RefreshIntervalSeconds { get; set; } = 180; // 3분

    // 외관
    public AppAppearance Appearance { get; set; } = AppAppearance.System;
    public TimeFormatPreference TimeFormat { get; set; } = TimeFormatPreference.System;
    public AppLanguage Language { get; set; } = AppLanguage.English;

    // 시스템
    public bool LaunchAtLogin { get; set; } = false;
    public bool NotificationsEnabled { get; set; } = true;
    public bool IsFirstLaunch { get; set; } = true;

    // 현재 계정 ID (다중 계정용)
    public string? CurrentAccountId { get; set; }
}
