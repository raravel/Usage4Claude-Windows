use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    Manager, Runtime,
};

pub fn create_tray<R: Runtime>(
    app: &tauri::AppHandle<R>,
) -> tauri::Result<tauri::tray::TrayIcon<R>> {
    let quit_i = MenuItem::with_id(app, "quit", "종료", true, None::<&str>)?;
    let settings_i = MenuItem::with_id(app, "settings", "설정...", true, None::<&str>)?;
    let menu = Menu::with_items(app, &[&settings_i, &quit_i])?;

    TrayIconBuilder::new()
        .menu(&menu)
        .tooltip("Usage4Claude")
        .show_menu_on_left_click(false)
        .icon(app.default_window_icon().cloned().unwrap())
        .on_menu_event(|app, event| match event.id.as_ref() {
            "quit" => {
                let state = app.state::<crate::AppState>();
                state.cancel_token.cancel();
                app.exit(0);
            }
            "settings" => {
                // TODO: 설정 윈도우 열기 (이후 태스크)
            }
            _ => {}
        })
        .on_tray_icon_event(|_tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                // TODO: 팝업 윈도우 열기 (이후 태스크)
            }
        })
        .build(app)
}
