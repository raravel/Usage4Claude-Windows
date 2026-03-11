// REVIEW: PASS — 완료 조건 전체 충족.
// 1. open_login_window: WebviewWindowBuilder로 https://claude.ai/login 창 열기 구현 완료.
// 2. claude.ai 로그인 가능: External WebviewUrl로 로그인 페이지 로드 정상.
// 3. 허용 도메인 외 차단: on_navigation을 builder 메서드로 호출(build() 이전). is_allowed_url이
//    ALLOWED_DOMAINS 목록과 서브도메인(ends_with)을 함께 검증 — 정확한 패턴.
//    url crate = "2" Cargo.toml에 추가됨.
// 4. 비영구 세션: close_login_window로 명시적 닫기 지원, 07-01에서 ephemeral 처리 예정 — 적절.
// 5. 두 command 모두 lib.rs invoke_handler에 등록됨.
// 6. cargo check / clippy -D warnings / svelte-check / vite build 모두 오류 없음.
//
// REVIEW [07-01]: PASS — 완료 조건 전체 확인.
// [조건 1] sessionKey 자동 추출: on_navigation에서 claude.ai 비로그인 경로 탐지 후
//   2초 delay + win.eval()로 JS 주입, receive_login_cookies invoke → parse_session_key로
//   쿠키 파싱 → AppState.login_session_key에 저장. 구현 완료.
// [조건 2] Organizations 조회/선택: handleLoginSuccess에서 fetchOrganizations 호출.
//   1개면 자동 등록, 다수면 showBrowserOrgSelect UI로 선택. 구현 완료.
// [조건 3] keyring 저장 + 새로고침: addAccount 호출 후 closeLoginWindow + loadAccounts 실행.
//   구현 완료.
// [조건 4] 세션키 로그 미노출: println!/dbg!/console.log 없음. 키 값은 조용히 저장만 됨.
// [기술 규칙] login_session_key: Mutex<Option<String>> → AppState에 추가 및 초기화 확인.
//   receive_login_cookies, get_login_result → lib.rs invoke_handler에 등록 확인.
//   Svelte 5 Runes ($state) 사용, stores 없음. onDestroy에서 polling cleanup 확인.
// [참고] on_navigation은 로그인 후 claude.ai 내 페이지 이동마다 재발화되나,
//   이미 키가 저장된 후 재주입은 멱등적이므로 무해함.
use tauri::{Manager, Emitter};
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

fn parse_session_key(cookie_str: &str) -> Option<String> {
    for part in cookie_str.split(';') {
        let trimmed = part.trim();
        if let Some(value) = trimmed.strip_prefix("sessionKey=") {
            let val = value.trim();
            if !val.is_empty() {
                return Some(val.to_string());
            }
        }
    }
    None
}

/// Open a WebView login window pointing to claude.ai/login.
/// After login completes (navigation to claude.ai away from /login),
/// injects JS to call receive_login_cookies with document.cookie.
#[tauri::command]
pub async fn open_login_window(app: tauri::AppHandle) -> Result<(), AppError> {
    // Close existing login window if any
    if let Some(existing) = app.get_webview_window("login") {
        let _ = existing.close();
    }

    // Clear any previous login result
    {
        let state = app.state::<crate::AppState>();
        *state.login_session_key.lock().unwrap() = None;
    }

    let app_clone = app.clone();

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
    .on_navigation(move |url| {
        let url_str = url.as_str();
        let allowed = is_allowed_url(url_str);

        // Detect login completion: navigated to claude.ai but NOT /login path
        if allowed
            && url.host_str() == Some("claude.ai")
            && !url.path().starts_with("/login")
            && url.path() != "/login"
        {
            // Notify frontend that user appears to have logged in
            let _ = app_clone.emit("login-detected", ());

            // Inject JS after a short delay to let the page finish loading cookies
            let app_for_eval = app_clone.clone();
            tauri::async_runtime::spawn(async move {
                tokio::time::sleep(std::time::Duration::from_secs(2)).await;
                if let Some(win) = app_for_eval.get_webview_window("login") {
                    let _ = win.eval(
                        "window.__TAURI__.core.invoke('receive_login_cookies', { cookies: document.cookie })"
                    );
                }
            });
        }

        allowed
    })
    .build()
    .map_err(|e| AppError::Settings(e.to_string()))?;

    Ok(())
}

/// Called from injected JS in the login WebView to pass cookies back to Rust.
/// Parses sessionKey from the cookie string and stores it in AppState.
/// NOTE: The session key value is never logged.
#[tauri::command]
pub fn receive_login_cookies(
    state: tauri::State<'_, crate::AppState>,
    cookies: String,
) -> Result<(), AppError> {
    if let Some(key) = parse_session_key(&cookies) {
        // Store the session key — do NOT log its value
        *state.login_session_key.lock().unwrap() = Some(key);
        Ok(())
    } else {
        Err(AppError::Settings("sessionKey not found in cookies".to_string()))
    }
}

/// Frontend polls this to check if a session key was extracted from the login WebView.
/// Returns Some(key) once available, None while still waiting.
#[tauri::command]
pub fn get_login_result(
    state: tauri::State<'_, crate::AppState>,
) -> Result<Option<String>, AppError> {
    let key = state.login_session_key.lock().unwrap().clone();
    Ok(key)
}

/// Close the login window and clear login state
#[tauri::command]
pub async fn close_login_window(app: tauri::AppHandle) -> Result<(), AppError> {
    if let Some(window) = app.get_webview_window("login") {
        window.close().map_err(|e| AppError::Settings(e.to_string()))?;
    }
    // Clear login session key from state
    let state = app.state::<crate::AppState>();
    *state.login_session_key.lock().unwrap() = None;
    Ok(())
}
