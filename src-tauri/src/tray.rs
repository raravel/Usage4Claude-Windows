// REVIEW: PASS
use tauri::{
    menu::{CheckMenuItem, Menu, MenuItem, PredefinedMenuItem, Submenu},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    AppHandle, Manager,
};

use crate::services::keyring_store::KeyringService;

pub fn create_tray(
    app: &AppHandle,
) -> tauri::Result<tauri::tray::TrayIcon> {
    let menu = build_menu(app)?;

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
            "refresh" => {
                let app_clone = app.clone();
                tauri::async_runtime::spawn(async move {
                    let _ = crate::services::refresh::manual_refresh(&app_clone).await;
                });
            }
            "add-account" => {
                // TODO: 계정 추가 윈도우 열기 (이후 태스크)
            }
            id if id.starts_with("account:") => {
                let account_id = id.trim_start_matches("account:").to_string();
                let app_clone = app.clone();
                tauri::async_runtime::spawn(async move {
                    switch_account(&app_clone, &account_id);
                });
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

/// 메뉴를 새로 빌드한다
fn build_menu(app: &AppHandle) -> tauri::Result<Menu<tauri::Wry>> {
    let accounts = KeyringService::load_accounts().unwrap_or_default();
    let active_account = accounts.iter().find(|a| a.is_active);

    // 헤더: 앱 이름 + 현재 계정 (disabled)
    let header_label = if let Some(account) = active_account {
        format!("Usage4Claude — {}", account.display_name)
    } else {
        "Usage4Claude".to_string()
    };
    let header_i = MenuItem::with_id(app, "header", &header_label, false, None::<&str>)?;

    let sep1 = PredefinedMenuItem::separator(app)?;

    // 새로고침
    let refresh_i = MenuItem::with_id(app, "refresh", "새로고침", true, None::<&str>)?;

    let sep2 = PredefinedMenuItem::separator(app)?;

    // 계정 서브메뉴
    let accounts_submenu = build_accounts_submenu(app, &accounts)?;

    let sep3 = PredefinedMenuItem::separator(app)?;

    // 설정
    let settings_i = MenuItem::with_id(app, "settings", "설정...", true, None::<&str>)?;

    let sep4 = PredefinedMenuItem::separator(app)?;

    // 종료
    let quit_i = MenuItem::with_id(app, "quit", "종료", true, None::<&str>)?;

    Menu::with_items(
        app,
        &[
            &header_i,
            &sep1,
            &refresh_i,
            &sep2,
            &accounts_submenu,
            &sep3,
            &settings_i,
            &sep4,
            &quit_i,
        ],
    )
}

/// 계정 서브메뉴를 빌드한다
fn build_accounts_submenu(
    app: &AppHandle,
    accounts: &[crate::models::account::Account],
) -> tauri::Result<Submenu<tauri::Wry>> {
    let mut items: Vec<Box<dyn tauri::menu::IsMenuItem<tauri::Wry>>> = Vec::new();

    for account in accounts {
        let id = format!("account:{}", account.id);
        let item = CheckMenuItem::with_id(
            app,
            id,
            &account.display_name,
            true,
            account.is_active,
            None::<&str>,
        )?;
        items.push(Box::new(item));
    }

    // 계정이 있으면 구분선 추가
    if !accounts.is_empty() {
        let sep = PredefinedMenuItem::separator(app)?;
        items.push(Box::new(sep));
    }

    // 계정 추가 항목
    let add_account_i =
        MenuItem::with_id(app, "add-account", "계정 추가...", true, None::<&str>)?;
    items.push(Box::new(add_account_i));

    let item_refs: Vec<&dyn tauri::menu::IsMenuItem<tauri::Wry>> =
        items.iter().map(|b| b.as_ref()).collect();

    Submenu::with_items(app, "계정", true, &item_refs)
}

/// 계정을 전환한다
fn switch_account(app: &AppHandle, target_id: &str) {
    let mut accounts = match KeyringService::load_accounts() {
        Ok(a) => a,
        Err(_) => return,
    };

    // 활성 계정 변경
    for account in &mut accounts {
        account.is_active = account.id == target_id;
    }

    if KeyringService::store_accounts(&accounts).is_err() {
        return;
    }

    // 메뉴 재구성
    let _ = rebuild_tray_menu(app);

    // 즉시 데이터 갱신
    let app_clone = app.clone();
    tauri::async_runtime::spawn(async move {
        crate::services::refresh::do_refresh(&app_clone).await;
    });
}

/// 트레이 메뉴를 동적으로 재구성한다
pub fn rebuild_tray_menu(app: &AppHandle) -> tauri::Result<()> {
    let new_menu = build_menu(app)?;
    let state = app.state::<crate::AppState>();
    let tray_guard = state.tray_icon.lock().unwrap();
    if let Some(tray) = tray_guard.as_ref() {
        tray.set_menu(Some(new_menu))?;
    }
    Ok(())
}

/// 툴팁 문자열 포맷
pub fn format_tooltip(data: &crate::models::usage::UsageData) -> String {
    use crate::models::usage::LimitType;

    let mut parts = vec!["Usage4Claude".to_string()];

    for limit in &data.limits {
        let label = match limit.limit_type {
            LimitType::FiveHour => "5h",
            LimitType::SevenDay => "7d",
            LimitType::Opus => "Opus",
            LimitType::Sonnet => "Sonnet",
            LimitType::Extra => "Extra",
        };
        parts.push(format!("{}: {:.0}%", label, limit.percentage * 100.0));
    }

    if let Some(extra) = &data.extra {
        parts.push(format!("Extra: {:.0}%", extra.percentage * 100.0));
    }

    parts.join(" - ")
}

/// 트레이 툴팁을 업데이트한다
pub fn update_tray_tooltip(app: &AppHandle, data: &crate::models::usage::UsageData) {
    let tooltip = format_tooltip(data);
    let state = app.state::<crate::AppState>();
    let tray_guard = state.tray_icon.lock().unwrap();
    if let Some(tray) = tray_guard.as_ref() {
        let _ = tray.set_tooltip(Some(&tooltip));
    }
}
