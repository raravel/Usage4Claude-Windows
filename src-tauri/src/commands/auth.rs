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
