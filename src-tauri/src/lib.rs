mod tray;
mod models;
mod services;
mod commands;

use std::sync::Mutex;
use tauri::Manager;
use tokio::task::JoinHandle;
use tokio_util::sync::CancellationToken;
use models::settings::UserSettings;
use models::usage::UsageData;
use services::claude_api::ClaudeApiService;

pub struct AppState {
    pub tray_icon: Mutex<Option<tauri::tray::TrayIcon<tauri::Wry>>>,
    pub settings: Mutex<UserSettings>,
    pub api_service: ClaudeApiService,
    pub consecutive_errors: Mutex<u32>,
    pub usage: Mutex<Option<UsageData>>,
    pub refresh_handle: Mutex<Option<JoinHandle<()>>>,
    pub cancel_token: CancellationToken,
    pub last_refresh: Mutex<Option<std::time::Instant>>,
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            // 두 번째 인스턴스 시작 시 기존 앱 포커스
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.set_focus();
            }
        }))
        .setup(|app| {
            let settings = services::settings::SettingsService::load(app.handle());
            let tray = tray::create_tray(app.handle())?;
            let api_service = ClaudeApiService::new()
                .expect("Failed to initialize Claude API service");

            let cancel_token = CancellationToken::new();
            let handle = services::refresh::start_refresh_service(
                app.handle().clone(),
                cancel_token.clone(),
            );

            let state = AppState {
                tray_icon: Mutex::new(Some(tray)),
                settings: Mutex::new(settings),
                api_service,
                consecutive_errors: Mutex::new(0),
                usage: Mutex::new(None),
                refresh_handle: Mutex::new(Some(handle)),
                cancel_token,
                last_refresh: Mutex::new(None),
            };
            app.manage(state);
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::settings::get_settings,
            commands::settings::update_settings,
            commands::usage::fetch_usage,
            commands::usage::fetch_organizations,
            commands::usage::manual_refresh,
            commands::account::get_accounts,
            commands::account::validate_session,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
