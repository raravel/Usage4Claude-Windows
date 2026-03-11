use std::fs;
use std::path::PathBuf;
use tauri::Manager;
use crate::models::settings::UserSettings;

pub struct SettingsService;

impl SettingsService {
    /// settings.json 파일 경로
    fn settings_path(app: &tauri::AppHandle) -> tauri::Result<PathBuf> {
        let config_dir = app.path().app_config_dir()?;
        fs::create_dir_all(&config_dir)?;
        Ok(config_dir.join("settings.json"))
    }

    /// 설정 로드. 파일 없으면 기본값 반환
    pub fn load(app: &tauri::AppHandle) -> UserSettings {
        match Self::settings_path(app) {
            Ok(path) => {
                if path.exists() {
                    match fs::read_to_string(&path) {
                        Ok(content) => serde_json::from_str(&content).unwrap_or_default(),
                        Err(_) => UserSettings::default(),
                    }
                } else {
                    UserSettings::default()
                }
            }
            Err(_) => UserSettings::default(),
        }
    }

    /// 설정 저장
    pub fn save(app: &tauri::AppHandle, settings: &UserSettings) -> Result<(), String> {
        let path = Self::settings_path(app).map_err(|e| e.to_string())?;
        let json = serde_json::to_string_pretty(settings).map_err(|e| e.to_string())?;
        fs::write(&path, json).map_err(|e| e.to_string())
    }
}
