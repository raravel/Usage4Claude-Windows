// REVIEW: [PASS]
// 완료조건 1: load()/save() 구현 완료. app_config_dir()로 settings.json 경로 결정 ✓
// 완료조건 2: UserSettings::default() 구현. 파일 없음/파싱 실패 모두 기본값 반환 ✓
// 완료조건 3: get_settings, update_settings 커맨드 존재, lib.rs invoke_handler 등록 ✓
// 추가: 모든 enum에 Serialize/Deserialize 파생. AppState에 settings: Mutex<UserSettings> ✓
// cargo check / cargo clippy -D warnings 모두 통과 ✓
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
