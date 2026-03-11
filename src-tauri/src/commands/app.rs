// REVIEW: PASS — env!("CARGO_PKG_VERSION")를 사용해 Cargo.toml 버전을 컴파일 타임에 읽어옴. invoke_handler에 등록 확인됨.
#[tauri::command]
pub fn get_app_version() -> String {
    env!("CARGO_PKG_VERSION").to_string()
}
