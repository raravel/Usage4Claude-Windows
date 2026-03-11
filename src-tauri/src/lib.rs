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
use services::icon_renderer::IconRenderer;

pub struct AppState {
    pub tray_icon: Mutex<Option<tauri::tray::TrayIcon<tauri::Wry>>>,
    pub settings: Mutex<UserSettings>,
    pub api_service: ClaudeApiService,
    pub consecutive_errors: Mutex<u32>,
    pub usage: Mutex<Option<UsageData>>,
    pub refresh_handle: Mutex<Option<JoinHandle<()>>>,
    pub cancel_token: CancellationToken,
    pub last_refresh: Mutex<Option<std::time::Instant>>,
    pub icon_renderer: Mutex<IconRenderer>,
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        // REVIEW: PASS — tauri_plugin_shell::init() 올바르게 등록됨. Cargo.toml, capabilities/default.json의 "shell:allow-open" 권한도 확인됨.
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_autostart::init(tauri_plugin_autostart::MacosLauncher::LaunchAgent, None))
        .plugin(tauri_plugin_positioner::init())
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
                icon_renderer: Mutex::new(IconRenderer::new()),
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
            commands::account::add_account,
            commands::account::remove_account,
            commands::account::switch_account,
            commands::account::diagnose_connection,
            commands::app::get_app_version,
            commands::auth::open_login_window,
            commands::auth::close_login_window,
        ])
        .on_window_event(|window, event| {
            if window.label() == "popup" {
                if let tauri::WindowEvent::Focused(false) = event {
                    let _ = window.hide();
                }
            }
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
