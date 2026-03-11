mod tray;
mod models;
mod services;
mod commands;
mod logging;

use std::sync::Mutex;
use tauri::Manager;
use tokio::task::JoinHandle;
use tokio_util::sync::CancellationToken;
use models::settings::UserSettings;
use models::usage::UsageData;
use services::claude_api::ClaudeApiService;
use services::icon_renderer::IconRenderer;
use services::notification::NotificationTracker;

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
    pub login_session_key: Mutex<Option<String>>,
    pub notification_tracker: Mutex<NotificationTracker>,
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    // REVIEW: PASS — [완료조건2] std::env::args()로 "--debug" 플래그 감지. setup 클로저에 move로 캡처.
    let debug = std::env::args().any(|a| a == "--debug");

    tauri::Builder::default()
        // REVIEW: PASS — tauri_plugin_shell::init() 올바르게 등록됨. Cargo.toml, capabilities/default.json의 "shell:allow-open" 권한도 확인됨.
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_autostart::init(tauri_plugin_autostart::MacosLauncher::LaunchAgent, None))
        .plugin(tauri_plugin_positioner::init())
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            // 두 번째 인스턴스 시작 시 기존 앱 포커스
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.set_focus();
            }
        }))
        .setup(move |app| {
            // Initialize logging
            let log_dir = app.path().app_config_dir()
                .map(|p| p.join("logs"))
                .unwrap_or_else(|_| std::path::PathBuf::from("./logs"));
            std::fs::create_dir_all(&log_dir).ok();
            logging::init_logging(&log_dir, debug);
            tracing::info!("Usage4Claude starting");

            let settings = services::settings::SettingsService::load(app.handle());
            let first_launch = !settings.first_launch_done;
            let tray = tray::create_tray(app.handle(), &settings.language)?;
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
                login_session_key: Mutex::new(None),
                notification_tracker: Mutex::new(NotificationTracker::new()),
            };
            app.manage(state);

            // REVIEW: PASS — [완료조건1] first_launch_done=false 시 welcome 윈도우를 동적으로 생성. setup에서 state 관리 후 조건 분기 올바름.
            if first_launch {
                let _ = tauri::WebviewWindowBuilder::new(
                    app,
                    "welcome",
                    tauri::WebviewUrl::App("/welcome".into()),
                )
                .title("Welcome to Usage4Claude")
                .inner_size(500.0, 560.0)
                .center()
                .resizable(false)
                .build();
            }

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::settings::get_settings,
            commands::settings::update_settings,
            commands::settings::update_tray_language,
            commands::settings::is_first_launch,
            commands::settings::complete_first_launch,
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
            commands::app::check_for_updates,
            commands::auth::open_login_window,
            commands::auth::close_login_window,
            commands::auth::receive_login_cookies,
            commands::auth::get_login_result,
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
