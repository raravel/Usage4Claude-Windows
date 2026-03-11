// REVIEW: [PASS]
// 완료 조건 검증 결과:
// 1. TrayIconBuilder로 트레이 아이콘 생성 — PASS (TrayIconBuilder::new().build(app))
// 2. 우클릭 컨텍스트 메뉴(설정, 종료) — PASS (MenuItem "settings", "quit" 포함)
// 3. 종료 메뉴 이벤트에서 app.exit(0) — PASS ("quit" 이벤트 핸들러에서 app.exit(0) 호출)
// 4. tauri-plugin-single-instance 등록 — PASS (Cargo.toml 포함, lib.rs에서 플러그인 등록)
// 추가 확인:
// - tray.rs 파일 존재, lib.rs에서 mod tray 임포트 — PASS
// - AppState에 TrayIcon 핸들 저장 — PASS (Mutex<Option<TrayIcon<Wry>>> 필드)
// - cargo check — PASS
// - cargo clippy -D warnings — PASS (경고 없음)
use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    Runtime,
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
