use tauri::{
    menu::{CheckMenuItem, Menu, MenuItem, PredefinedMenuItem, Submenu},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    AppHandle, Emitter, Manager,
};
use tauri_plugin_positioner::{Position, WindowExt};

use crate::services::keyring_store::KeyringService;

pub fn create_tray(
    app: &AppHandle,
    language: &str,
) -> tauri::Result<tauri::tray::TrayIcon> {
    let menu = build_menu_with_lang(app, language)?;

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
                if let Some(window) = app.get_webview_window("settings") {
                    let _ = window.show();
                    let _ = window.set_focus();
                } else {
                    let _ = tauri::WebviewWindowBuilder::new(
                        app,
                        "settings",
                        tauri::WebviewUrl::App("/settings".into()),
                    )
                    .title("Settings")
                    .inner_size(500.0, 600.0)
                    .resizable(false)
                    .center()
                    .build();
                }
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
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                let app = tray.app_handle();
                if let Some(window) = app.get_webview_window("popup") {
                    if window.is_visible().unwrap_or(false) {
                        let _ = window.hide();
                    } else {
                        let _ = window.move_window(Position::TrayCenter);
                        let _ = window.show();
                        let _ = window.set_focus();
                    }
                }
            }
        })
        .build(app)
}

/// Returns a localised tray label for the given key and language code.
fn get_tray_label(key: &str, language: &str) -> &'static str {
    match (key, language) {
        ("refresh", "ko") => "새로고침",
        ("refresh", "ja") => "更新",
        ("refresh", "zh-Hans") => "刷新",
        ("refresh", "zh-Hant") => "重新整理",
        ("refresh", _) => "Refresh",

        ("accounts", "ko") => "계정",
        ("accounts", "ja") => "アカウント",
        ("accounts", "zh-Hans") => "账户",
        ("accounts", "zh-Hant") => "帳戶",
        ("accounts", _) => "Accounts",

        ("addAccount", "ko") => "계정 추가...",
        ("addAccount", "ja") => "アカウント追加...",
        ("addAccount", "zh-Hans") => "添加账户...",
        ("addAccount", "zh-Hant") => "新增帳戶...",
        ("addAccount", _) => "Add Account...",

        ("settings", "ko") => "설정...",
        ("settings", "ja") => "設定...",
        ("settings", "zh-Hans") => "设置...",
        ("settings", "zh-Hant") => "設定...",
        ("settings", _) => "Settings...",

        ("quit", "ko") => "종료",
        ("quit", "ja") => "終了",
        ("quit", "zh-Hans") => "退出",
        ("quit", "zh-Hant") => "結束",
        ("quit", _) => "Quit",

        (_, _) => "???",
    }
}

/// Resolves "system" language to English for tray labels (tray is OS-native, no navigator).
fn resolve_lang(language: &str) -> &str {
    if language == "system" {
        "en"
    } else {
        language
    }
}

/// 메뉴를 현재 AppState에서 언어를 읽어 빌드한다 (AppState가 이미 manage된 이후에만 사용)
fn build_menu(app: &AppHandle) -> tauri::Result<Menu<tauri::Wry>> {
    let lang_owned = {
        let state = app.state::<crate::AppState>();
        let settings = state.settings.lock().unwrap();
        settings.language.clone()
    };
    build_menu_with_lang(app, &lang_owned)
}

/// 메뉴를 지정된 언어로 빌드한다
fn build_menu_with_lang(app: &AppHandle, language: &str) -> tauri::Result<Menu<tauri::Wry>> {
    let lang = resolve_lang(language);

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
    let refresh_i = MenuItem::with_id(app, "refresh", get_tray_label("refresh", lang), true, None::<&str>)?;

    let sep2 = PredefinedMenuItem::separator(app)?;

    // 계정 서브메뉴
    let accounts_submenu = build_accounts_submenu(app, &accounts, lang)?;

    let sep3 = PredefinedMenuItem::separator(app)?;

    // 설정
    let settings_i = MenuItem::with_id(app, "settings", get_tray_label("settings", lang), true, None::<&str>)?;

    let sep4 = PredefinedMenuItem::separator(app)?;

    // 종료
    let quit_i = MenuItem::with_id(app, "quit", get_tray_label("quit", lang), true, None::<&str>)?;

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
    lang: &str,
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
        MenuItem::with_id(app, "add-account", get_tray_label("addAccount", lang), true, None::<&str>)?;
    items.push(Box::new(add_account_i));

    let item_refs: Vec<&dyn tauri::menu::IsMenuItem<tauri::Wry>> =
        items.iter().map(|b| b.as_ref()).collect();

    Submenu::with_items(app, get_tray_label("accounts", lang), true, &item_refs)
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

    // 프론트엔드 accountsStore 갱신을 위해 이벤트 emit
    let _ = app.emit("account-changed", ());

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
