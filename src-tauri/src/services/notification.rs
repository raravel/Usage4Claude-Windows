use std::collections::HashMap;
use crate::models::usage::{UsageData, LimitType};
use crate::models::settings::UserSettings;

const WARNING_THRESHOLD: f64 = 0.9; // 90%
const RESET_DROP_THRESHOLD: f64 = 0.5; // percentage drop > 50% = likely reset
// REVIEW: PASS — WARNING_THRESHOLD(0.9)로 90% 경고 조건 올바르게 설정됨. RESET_DROP_THRESHOLD(0.5)는 50%p 이상 하락을 의미하며 resets_at 변경을 추가 조건으로 요구하므로 오탐 가능성이 낮음.

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
    // REVIEW: PASS — check()가 상태 변경(warned_periods, reset_notified_periods, previous_data)을 모두 담당하며 notifications를 Vec으로 반환해 호출자가 실제 전송을 담당하는 설계가 명확함.
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
            // REVIEW: PASS — notifications_enabled 설정으로 90% 경고 알림 비활성화 가능. limit_type + resets_at 조합으로 리셋 주기당 1회만 전송되어 중복 방지 충족.
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
            // REVIEW: PASS — reset_notifications 설정으로 리셋 알림 비활성화 가능. resets_at 변경 + 50%p 이상 하락 두 조건을 AND로 결합해 신뢰도 높은 리셋 감지. reset_notified_periods로 새 주기당 1회 보장.
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
