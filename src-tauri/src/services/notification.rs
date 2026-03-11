use std::collections::HashMap;
use crate::models::usage::{UsageData, LimitType};
use crate::models::settings::UserSettings;

const WARNING_THRESHOLD: f64 = 0.9; // 90%
const RESET_DROP_THRESHOLD: f64 = 0.5; // percentage drop > 50% = likely reset

/// Tracks notification state to prevent duplicates
pub struct NotificationTracker {
    /// Per limit-type: resets_at string when 90% warning was sent
    warned_periods: HashMap<String, String>,
    /// Per limit-type: resets_at string when reset notification was sent
    reset_notified_periods: HashMap<String, String>,
    /// Previous usage data for detecting resets
    previous_data: Option<UsageData>,
}

impl NotificationTracker {
    pub fn new() -> Self {
        Self {
            warned_periods: HashMap::new(),
            reset_notified_periods: HashMap::new(),
            previous_data: None,
        }
    }

    /// Check usage data and return notifications to send
    pub fn check(&mut self, data: &UsageData, settings: &UserSettings) -> Vec<NotificationInfo> {
        let mut notifications = Vec::new();

        // Collect all limits from both limits vec and optional extra
        let mut all_limits: Vec<_> = data.limits.iter().collect();
        if let Some(extra) = &data.extra {
            all_limits.push(extra);
        }

        for limit in &all_limits {
            let type_key = format!("{:?}", limit.limit_type);

            // 90% warning
            if settings.notifications_enabled && limit.percentage >= WARNING_THRESHOLD {
                let already_warned = self.warned_periods
                    .get(&type_key)
                    .map(|p| p == &limit.resets_at)
                    .unwrap_or(false);

                if !already_warned {
                    notifications.push(NotificationInfo {
                        title: "Usage Warning".to_string(),
                        body: format!(
                            "{} usage at {:.0}%",
                            limit_type_label(&limit.limit_type),
                            limit.percentage * 100.0
                        ),
                    });
                    self.warned_periods.insert(type_key.clone(), limit.resets_at.clone());
                }
            }

            // Reset detection
            if settings.reset_notifications {
                if let Some(prev) = &self.previous_data {
                    let prev_limit = prev.limits.iter()
                        .chain(prev.extra.iter())
                        .find(|l| l.limit_type == limit.limit_type);

                    if let Some(prev_l) = prev_limit {
                        // Reset detected: percentage dropped significantly AND resets_at changed
                        if prev_l.percentage > 0.3
                            && limit.percentage < prev_l.percentage - RESET_DROP_THRESHOLD
                            && limit.resets_at != prev_l.resets_at
                        {
                            let already_notified = self.reset_notified_periods
                                .get(&type_key)
                                .map(|p| p == &limit.resets_at)
                                .unwrap_or(false);

                            if !already_notified {
                                notifications.push(NotificationInfo {
                                    title: "Quota Reset".to_string(),
                                    body: format!(
                                        "{} quota has been reset",
                                        limit_type_label(&limit.limit_type)
                                    ),
                                });
                                self.reset_notified_periods.insert(type_key.clone(), limit.resets_at.clone());
                            }
                        }
                    }
                }
            }
        }

        // Store current data as previous for next check
        self.previous_data = Some(data.clone());

        notifications
    }
}

pub struct NotificationInfo {
    pub title: String,
    pub body: String,
}

fn limit_type_label(lt: &LimitType) -> &'static str {
    match lt {
        LimitType::FiveHour => "5-Hour",
        LimitType::SevenDay => "7-Day",
        LimitType::Opus => "Opus",
        LimitType::Sonnet => "Sonnet",
        LimitType::Extra => "Extra",
    }
}
