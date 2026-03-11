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
