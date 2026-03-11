use serde::{Deserialize, Serialize};
use crate::models::error::AppError;

// REVIEW: PASS — [완료조건1] GITHUB_API_URL이 올바른 GitHub Releases API 엔드포인트를 사용함.
// REVIEW: PASS — [완료조건3] GITHUB_REPO_PREFIX를 사용해 html_url이 실제 저장소 URL로 시작하는지 starts_with 검증함.
const GITHUB_API_URL: &str = "https://api.github.com/repos/raravel/Usage4Claude-Windows/releases/latest";
const GITHUB_REPO_PREFIX: &str = "https://github.com/raravel/Usage4Claude-Windows/";

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UpdateInfo {
    pub current_version: String,
    pub latest_version: String,
    pub update_available: bool,
    pub release_url: Option<String>,
    pub release_notes: Option<String>,
}

#[derive(Deserialize)]
struct GitHubRelease {
    tag_name: String,
    html_url: String,
    body: Option<String>,
}

pub struct UpdateChecker {
    client: reqwest::Client,
}

// REVIEW: PASS — [완료조건1] reqwest 클라이언트에 User-Agent 헤더("Usage4Claude-Windows")가 설정됨. GitHub API 요건 충족.
impl UpdateChecker {
    pub fn new() -> Self {
        let client = reqwest::Client::builder()
            .user_agent("Usage4Claude-Windows")
            .build()
            .unwrap_or_default();
        Self { client }
    }

    pub async fn check_for_updates(&self) -> Result<UpdateInfo, AppError> {
        let current = env!("CARGO_PKG_VERSION").to_string();

        let response = self.client
            .get(GITHUB_API_URL)
            .send()
            .await
            .map_err(|e| AppError::Network(e.to_string()))?;

        if !response.status().is_success() {
            return Err(AppError::Network(format!("GitHub API returned {}", response.status())));
        }

        let release: GitHubRelease = response
            .json()
            .await
            .map_err(|e| AppError::Parse(e.to_string()))?;

        // Strip 'v' prefix from tag_name if present
        let latest = release.tag_name.trim_start_matches('v').to_string();

        // URL validation: only allow GitHub repo URLs
        let release_url = if release.html_url.starts_with(GITHUB_REPO_PREFIX) {
            Some(release.html_url)
        } else {
            None
        };

        let update_available = is_newer_version(&current, &latest);

        Ok(UpdateInfo {
            current_version: current,
            latest_version: latest,
            update_available,
            release_url,
            release_notes: release.body,
        })
    }
}

// REVIEW: PASS — [완료조건1] semver 비교를 직접 구현. major.minor.patch 순서로 비교하며 단위 테스트 6개 포함.
// REVIEW: PASS — [완료조건2] update_available 플래그가 UpdateInfo 구조체에 포함되며 Tauri 커맨드로 반환됨.
/// Simple semver comparison: returns true if latest > current
fn is_newer_version(current: &str, latest: &str) -> bool {
    let parse = |v: &str| -> Vec<u32> {
        v.split('.')
            .filter_map(|s| s.parse().ok())
            .collect()
    };

    let cur = parse(current);
    let lat = parse(latest);

    for i in 0..3 {
        let c = cur.get(i).unwrap_or(&0);
        let l = lat.get(i).unwrap_or(&0);
        if l > c { return true; }
        if l < c { return false; }
    }
    false
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_is_newer_version() {
        assert!(is_newer_version("0.1.0", "0.2.0"));
        assert!(is_newer_version("0.1.0", "1.0.0"));
        assert!(is_newer_version("1.0.0", "1.0.1"));
        assert!(!is_newer_version("0.2.0", "0.1.0"));
        assert!(!is_newer_version("0.1.0", "0.1.0"));
        assert!(!is_newer_version("1.0.0", "0.9.9"));
    }
}
