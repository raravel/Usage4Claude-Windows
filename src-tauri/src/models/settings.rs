use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UserSettings {
    pub display_mode: DisplayMode,
    pub icon_theme: IconTheme,
    pub display_content: DisplayContent,
    pub refresh_mode: RefreshMode,
    pub refresh_interval_minutes: u32,
    pub theme: AppTheme,
    pub time_format: TimeFormat,
    pub language: String,
    pub launch_at_login: bool,
    pub notifications_enabled: bool,
    pub reset_notifications: bool,
    pub first_launch_done: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub enum DisplayMode {
    #[default]
    PercentOnly,
    IconOnly,
    IconAndPercent,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub enum IconTheme {
    #[default]
    ColorTranslucent,
    ColorWithBackground,
    Monochrome,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub enum DisplayContent {
    #[default]
    Smart,
    Custom,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub enum RefreshMode {
    #[default]
    Smart,
    Fixed,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub enum AppTheme {
    #[default]
    System,
    Light,
    Dark,
}

#[derive(Debug, Clone, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub enum TimeFormat {
    #[default]
    System,
    TwelveHour,
    TwentyFourHour,
}

impl Default for UserSettings {
    fn default() -> Self {
        Self {
            display_mode: DisplayMode::default(),
            icon_theme: IconTheme::default(),
            display_content: DisplayContent::default(),
            refresh_mode: RefreshMode::default(),
            refresh_interval_minutes: 5,
            theme: AppTheme::default(),
            time_format: TimeFormat::default(),
            language: "en".to_string(),
            launch_at_login: false,
            notifications_enabled: true,
            reset_notifications: true,
            first_launch_done: false,
        }
    }
}
