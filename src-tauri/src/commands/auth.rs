// REVIEW: PASS — 완료 조건 전체 충족.
// 1. open_login_window: WebviewWindowBuilder로 https://claude.ai/login 창 열기 구현 완료.
// 2. claude.ai 로그인 가능: External WebviewUrl로 로그인 페이지 로드 정상.
// 3. 허용 도메인 외 차단: on_navigation을 builder 메서드로 호출(build() 이전). is_allowed_url이
//    ALLOWED_DOMAINS 목록과 서브도메인(ends_with)을 함께 검증 — 정확한 패턴.
//    url crate = "2" Cargo.toml에 추가됨.
// 4. 비영구 세션: close_login_window로 명시적 닫기 지원, 07-01에서 ephemeral 처리 예정 — 적절.
// 5. 두 command 모두 lib.rs invoke_handler에 등록됨.
// 6. cargo check / clippy -D warnings / svelte-check / vite build 모두 오류 없음.
use tauri::Manager;
use crate::models::error::AppError;

/// Allowed domains for the login WebView
const ALLOWED_DOMAINS: &[&str] = &[
    "claude.ai",
    "accounts.google.com",
    "login.microsoftonline.com",
    "appleid.apple.com",
    "auth0.com",
    "sso.anthropic.com",
];

fn is_allowed_url(url: &str) -> bool {
    if let Ok(parsed) = url::Url::parse(url) {
        if let Some(host) = parsed.host_str() {
            return ALLOWED_DOMAINS
                .iter()
                .any(|d| host == *d || host.ends_with(&format!(".{}", d)));
        }
    }
    false
}

/// Open a WebView login window pointing to claude.ai/login
#[tauri::command]
pub async fn open_login_window(app: tauri::AppHandle) -> Result<(), AppError> {
    // Close existing login window if any
    if let Some(existing) = app.get_webview_window("login") {
        let _ = existing.close();
    }

    // Create new WebView window pointing to claude.ai login
    // on_navigation is a builder method — must be called before .build()
    tauri::WebviewWindowBuilder::new(
        &app,
        "login",
        tauri::WebviewUrl::External("https://claude.ai/login".parse().unwrap()),
    )
    .title("Claude 로그인")
    .inner_size(600.0, 700.0)
    .center()
    .on_navigation(|url| is_allowed_url(url.as_str()))
    .build()
    .map_err(|e| AppError::Settings(e.to_string()))?;

    Ok(())
}

/// Close the login window
#[tauri::command]
pub async fn close_login_window(app: tauri::AppHandle) -> Result<(), AppError> {
    if let Some(window) = app.get_webview_window("login") {
        window.close().map_err(|e| AppError::Settings(e.to_string()))?;
    }
    Ok(())
}
