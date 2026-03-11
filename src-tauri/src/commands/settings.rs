use tauri::State;
use crate::AppState;
use crate::models::settings::UserSettings;
use crate::services::settings::SettingsService;

#[tauri::command]
pub fn get_settings(state: State<'_, AppState>) -> Result<UserSettings, String> {
    let settings = state.settings.lock().map_err(|e| e.to_string())?;
    Ok(settings.clone())
}

#[tauri::command]
pub fn update_settings(
    app: tauri::AppHandle,
    state: State<'_, AppState>,
    settings: UserSettings,
) -> Result<(), String> {
    SettingsService::save(&app, &settings)?;
    let mut current = state.settings.lock().map_err(|e| e.to_string())?;
    *current = settings;
    Ok(())
}

// REVIEW: PASS — [완료조건1,4] is_first_launch: !first_launch_done 반환. complete_first_launch: 플래그를 true로 설정하고 즉시 저장.
//   2회차 실행 시 first_launch_done=true이므로 환영 윈도우 미표시 보장.
#[tauri::command]
pub fn is_first_launch(state: State<'_, AppState>) -> bool {
    let settings = state.settings.lock().unwrap();
    !settings.first_launch_done
}

#[tauri::command]
pub fn complete_first_launch(
    state: State<'_, AppState>,
    app: tauri::AppHandle,
) -> Result<(), String> {
    let mut settings = state.settings.lock().map_err(|e| e.to_string())?;
    settings.first_launch_done = true;
    SettingsService::save(&app, &settings)
}

#[tauri::command]
pub fn update_tray_language(
    app: tauri::AppHandle,
    state: State<'_, AppState>,
    language: String,
) -> Result<(), String> {
    // Update language in state
    {
        let mut settings = state.settings.lock().map_err(|e| e.to_string())?;
        settings.language = language;
        SettingsService::save(&app, &settings)?;
    }
    // Rebuild tray menu with new language
    crate::tray::rebuild_tray_menu(&app).map_err(|e| e.to_string())
}
