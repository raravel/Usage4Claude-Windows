mod tray;

use std::sync::Mutex;
use tauri::Manager;

pub struct AppState {
    pub tray_icon: Mutex<Option<tauri::tray::TrayIcon<tauri::Wry>>>,
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
            let tray = tray::create_tray(app.handle())?;
            let state = AppState {
                tray_icon: Mutex::new(Some(tray)),
            };
            app.manage(state);
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
