// REVIEW: PASS — env!("CARGO_PKG_VERSION")를 사용해 Cargo.toml 버전을 컴파일 타임에 읽어옴. invoke_handler에 등록 확인됨.
#[tauri::command]
pub fn get_app_version() -> String {
    env!("CARGO_PKG_VERSION").to_string()
}

// REVIEW: PASS — [완료조건2] check_for_updates 커맨드가 lib.rs invoke_handler에 정상 등록됨. UpdateInfo 또는 AppError 반환.
#[tauri::command]
pub async fn check_for_updates() -> Result<crate::services::update_checker::UpdateInfo, crate::models::error::AppError> {
    let checker = crate::services::update_checker::UpdateChecker::new();
    checker.check_for_updates().await
}
