use reqwest::Client;
use crate::models::usage::{UsageData, UsageResponse};
use crate::models::account::Organization;
use crate::models::error::AppError;
use crate::services::retry::{retry_with_backoff, RetryConfig};

pub struct ClaudeApiService {
    client: Client,
}

impl ClaudeApiService {
    pub fn new() -> Result<Self, AppError> {
        let mut headers = reqwest::header::HeaderMap::new();
        headers.insert(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
                .parse()
                .unwrap(),
        );
        headers.insert("Accept", "application/json".parse().unwrap());
        headers.insert("Origin", "https://claude.ai".parse().unwrap());
        headers.insert("Referer", "https://claude.ai/".parse().unwrap());
        headers.insert(
            "sec-ch-ua",
            "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\""
                .parse()
                .unwrap(),
        );
        headers.insert("sec-ch-ua-mobile", "?0".parse().unwrap());
        headers.insert("sec-ch-ua-platform", "\"Windows\"".parse().unwrap());
        headers.insert("sec-fetch-dest", "empty".parse().unwrap());
        headers.insert("sec-fetch-mode", "cors".parse().unwrap());
        headers.insert("sec-fetch-site", "same-origin".parse().unwrap());

        let client = Client::builder()
            .default_headers(headers)
            .cookie_store(true)
            .build()
            .map_err(|e| AppError::Network(e.to_string()))?;

        Ok(Self { client })
    }

    /// 사용량 조회
    pub async fn get_usage(&self, org_id: &str, session_key: &str) -> Result<UsageResponse, AppError> {
        let url = format!("https://claude.ai/api/organizations/{}/usage", org_id);
        let response = self
            .client
            .get(&url)
            .header("Cookie", format!("sessionKey={}", session_key))
            .send()
            .await
            .map_err(|e| AppError::Network(e.to_string()))?;

        self.handle_response(response).await
    }

    /// 추가 사용량 조회
    pub async fn get_extra_usage(&self, org_id: &str, session_key: &str) -> Result<UsageResponse, AppError> {
        let url = format!("https://claude.ai/api/organizations/{}/usage?type=extra", org_id);
        let response = self
            .client
            .get(&url)
            .header("Cookie", format!("sessionKey={}", session_key))
            .send()
            .await
            .map_err(|e| AppError::Network(e.to_string()))?;

        self.handle_response(response).await
    }

    /// 조직 목록 조회
    pub async fn get_organizations(&self, session_key: &str) -> Result<Vec<Organization>, AppError> {
        let response = self
            .client
            .get("https://claude.ai/api/organizations")
            .header("Cookie", format!("sessionKey={}", session_key))
            .send()
            .await
            .map_err(|e| AppError::Network(e.to_string()))?;

        match response.status().as_u16() {
            200 => {
                let text = response
                    .text()
                    .await
                    .map_err(|e| AppError::Network(e.to_string()))?;
                // Cloudflare HTML 차단 감지
                if text.trim_start().starts_with('<') {
                    return Err(AppError::CloudflareBlock);
                }
                serde_json::from_str(&text).map_err(|e| AppError::Parse(e.to_string()))
            }
            401 => Err(AppError::Unauthorized),
            403 => Err(AppError::Forbidden),
            429 => {
                let retry_after = response
                    .headers()
                    .get("retry-after")
                    .and_then(|v| v.to_str().ok())
                    .and_then(|v| v.parse().ok())
                    .unwrap_or(60);
                Err(AppError::RateLimited { retry_after })
            }
            _ => Err(AppError::Parse(format!(
                "Unexpected status: {}",
                response.status()
            ))),
        }
    }

    /// 사용량 + 추가 사용량 요청 (usage는 재시도, extra는 1회만)
    pub async fn fetch_all_usage(&self, org_id: &str, session_key: &str) -> Result<UsageData, AppError> {
        let config = RetryConfig::default();

        let org_id_owned = org_id.to_string();
        let session_key_owned = session_key.to_string();

        let usage_result = retry_with_backoff(&config, || {
            self.get_usage(&org_id_owned, &session_key_owned)
        }).await;

        // extra는 재시도 없이 1회만
        let extra_result = self.get_extra_usage(org_id, session_key).await.ok();

        let usage = usage_result?;
        Ok(UsageData::from_response(&usage, extra_result.as_ref()))
    }

    /// 공통 응답 처리
    async fn handle_response(&self, response: reqwest::Response) -> Result<UsageResponse, AppError> {
        match response.status().as_u16() {
            200 => {
                let text = response
                    .text()
                    .await
                    .map_err(|e| AppError::Network(e.to_string()))?;
                // Cloudflare HTML 차단 감지
                if text.trim_start().starts_with('<') {
                    return Err(AppError::CloudflareBlock);
                }
                serde_json::from_str(&text).map_err(|e| AppError::Parse(e.to_string()))
            }
            401 => Err(AppError::Unauthorized),
            403 => Err(AppError::Forbidden),
            429 => {
                let retry_after = response
                    .headers()
                    .get("retry-after")
                    .and_then(|v| v.to_str().ok())
                    .and_then(|v| v.parse().ok())
                    .unwrap_or(60);
                Err(AppError::RateLimited { retry_after })
            }
            _ => Err(AppError::Parse(format!(
                "Unexpected status: {}",
                response.status()
            ))),
        }
    }
}
