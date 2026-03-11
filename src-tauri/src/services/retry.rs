// REVIEW: PASS
use std::time::Duration;
use tokio::time::sleep;
use crate::models::error::AppError;

pub struct RetryConfig {
    pub max_retries: u32,
    pub base_delay: Duration,
}

impl Default for RetryConfig {
    fn default() -> Self {
        Self {
            max_retries: 3,
            base_delay: Duration::from_secs(1),
        }
    }
}

/// 지수 백오프로 비동기 함수 재시도
pub async fn retry_with_backoff<F, Fut, T>(
    config: &RetryConfig,
    f: F,
) -> Result<T, AppError>
where
    F: Fn() -> Fut,
    Fut: std::future::Future<Output = Result<T, AppError>>,
{
    let mut last_error = None;

    for attempt in 0..=config.max_retries {
        match f().await {
            Ok(result) => return Ok(result),
            Err(e) => {
                // 401/403은 재시도 없음
                match &e {
                    AppError::Unauthorized | AppError::Forbidden | AppError::CloudflareBlock => {
                        return Err(e);
                    }
                    AppError::RateLimited { retry_after } => {
                        // retry_after 존중
                        if attempt < config.max_retries {
                            sleep(Duration::from_secs(*retry_after)).await;
                        }
                        last_error = Some(e);
                        continue;
                    }
                    _ => {
                        // 네트워크/파싱 에러: 지수 백오프
                        if attempt < config.max_retries {
                            let delay = config.base_delay * 2u32.pow(attempt);
                            sleep(delay).await;
                        }
                        last_error = Some(e);
                    }
                }
            }
        }
    }

    Err(last_error.unwrap_or(AppError::Network("Max retries exceeded".to_string())))
}

/// 연속 에러 수에 따른 추천 새로고침 간격 (초)
#[allow(dead_code)]
pub fn recommended_interval(consecutive_errors: u32) -> u64 {
    match consecutive_errors {
        0..=4 => 60,     // 기본 1분
        5..=9 => 180,    // 3분
        10..=14 => 300,  // 5분
        _ => 600,        // 10분
    }
}
