// REVIEW: PASS — env!("CARGO_PKG_VERSION")를 사용해 Cargo.toml 버전을 컴파일 타임에 읽어옴. invoke_handler에 등록 확인됨.
#[tauri::command]
pub fn get_app_version() -> String {
    env!("CARGO_PKG_VERSION").to_string()
}

#[tauri::command]
pub async fn check_for_updates() -> Result<crate::services::update_checker::UpdateInfo, crate::models::error::AppError> {
    let checker = crate::services::update_checker::UpdateChecker::new();
    checker.check_for_updates().await
}
