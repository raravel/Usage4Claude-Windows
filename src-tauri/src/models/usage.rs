// REVIEW: [PASS] 모든 완료 조건 충족. UsageResponse/UsageLimit/UsageData/UsageLimitInfo/LimitType/Account/Organization/AppError 모두 존재. serde Serialize+Deserialize + camelCase 적용(Organization은 snake_case 필드가 없어 실질적 동일). LimitType에 colors()+color_for_percentage() 구현. cargo check/clippy -D warnings 클린. AppError thiserror+수동 Serialize 정상. chrono 의존성 확인.
#![allow(dead_code)]

use serde::{Deserialize, Serialize};

/// Claude API 직접 응답 모델
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UsageResponse {
    pub five_hour: Option<UsageLimit>,
    pub seven_day: Option<UsageLimit>,
    pub opus: Option<UsageLimit>,
    pub sonnet: Option<UsageLimit>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UsageLimit {
    pub percentage: f64,
    pub resets_at: String, // ISO 8601
}

/// 앱 내부 사용 모델 (프론트엔드로 전달)
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UsageData {
    pub limits: Vec<UsageLimitInfo>,
    pub extra: Option<UsageLimitInfo>,
    pub fetched_at: String, // ISO 8601
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UsageLimitInfo {
    pub limit_type: LimitType,
    pub percentage: f64,
    pub resets_at: String,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "camelCase")]
pub enum LimitType {
    FiveHour,
    SevenDay,
    Opus,
    Sonnet,
    Extra,
}

impl LimitType {
    /// 색상 매핑: (낮음 <50%, 중간 50-75%, 높음 >75%) — (r, g, b) 튜플
    pub fn colors(&self) -> [(u8, u8, u8); 3] {
        match self {
            LimitType::FiveHour => [
                (0x4C, 0xAF, 0x50), // 초록
                (0xFF, 0x98, 0x00), // 주황
                (0xF4, 0x43, 0x36), // 빨강
            ],
            LimitType::SevenDay => [
                (0xCE, 0x93, 0xD8), // 연보라
                (0x9C, 0x27, 0xB0), // 보라
                (0xE9, 0x1E, 0x63), // 자홍
            ],
            LimitType::Extra => [
                (0xF4, 0x8F, 0xB1), // 핑크
                (0xE9, 0x1E, 0x63), // 로즈
                (0xAD, 0x14, 0x57), // 자홍
            ],
            LimitType::Opus => [
                (0xFF, 0xB7, 0x4D), // 주황
                (0xFF, 0xA0, 0x00), // 앰버
                (0xE6, 0x51, 0x00), // 주황-빨강
            ],
            LimitType::Sonnet => [
                (0x64, 0xB5, 0xF6), // 연파랑
                (0x19, 0x76, 0xD2), // 파랑
                (0x28, 0x35, 0x93), // 인디고
            ],
        }
    }

    /// 퍼센티지에 따른 색상 반환
    pub fn color_for_percentage(&self, percentage: f64) -> (u8, u8, u8) {
        let colors = self.colors();
        if percentage < 50.0 {
            colors[0]
        } else if percentage < 75.0 {
            colors[1]
        } else {
            colors[2]
        }
    }
}

/// UsageResponse → UsageData 변환
impl UsageData {
    pub fn from_response(response: &UsageResponse, extra: Option<&UsageResponse>) -> Self {
        let mut limits = Vec::new();

        if let Some(fh) = &response.five_hour {
            limits.push(UsageLimitInfo {
                limit_type: LimitType::FiveHour,
                percentage: fh.percentage,
                resets_at: fh.resets_at.clone(),
            });
        }
        if let Some(sd) = &response.seven_day {
            limits.push(UsageLimitInfo {
                limit_type: LimitType::SevenDay,
                percentage: sd.percentage,
                resets_at: sd.resets_at.clone(),
            });
        }
        if let Some(op) = &response.opus {
            limits.push(UsageLimitInfo {
                limit_type: LimitType::Opus,
                percentage: op.percentage,
                resets_at: op.resets_at.clone(),
            });
        }
        if let Some(sn) = &response.sonnet {
            limits.push(UsageLimitInfo {
                limit_type: LimitType::Sonnet,
                percentage: sn.percentage,
                resets_at: sn.resets_at.clone(),
            });
        }

        // Extra usage from separate API call
        let extra_info = extra.and_then(|e| {
            e.five_hour.as_ref().map(|fh| UsageLimitInfo {
                limit_type: LimitType::Extra,
                percentage: fh.percentage,
                resets_at: fh.resets_at.clone(),
            })
        });

        Self {
            limits,
            extra: extra_info,
            fetched_at: chrono::Utc::now().to_rfc3339(),
        }
    }
}
