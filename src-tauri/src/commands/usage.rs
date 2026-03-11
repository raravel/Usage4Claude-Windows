use tauri::Emitter;
use tauri::State;
use crate::AppState;
use crate::models::usage::UsageData;
use crate::models::account::Organization;
use crate::models::error::AppError;

#[tauri::command]
pub async fn fetch_usage(
    app: tauri::AppHandle,
    state: State<'_, AppState>,
    org_id: String,
    session_key: String,
) -> Result<UsageData, AppError> {
    match state.api_service.fetch_all_usage(&org_id, &session_key).await {
        Ok(data) => {
            // 성공 시 에러 카운트 리셋
            if let Ok(mut count) = state.consecutive_errors.lock() {
                *count = 0;
            }
            Ok(data)
        }
        Err(e) => {
            // 에러 카운트 증가
            if let Ok(mut count) = state.consecutive_errors.lock() {
                *count += 1;
            }
            // 프론트엔드에 에러 이벤트 전달
            let _ = app.emit("api-error", e.to_string());
            Err(e)
        }
    }
}

#[tauri::command]
pub async fn fetch_organizations(
    state: State<'_, AppState>,
    session_key: String,
) -> Result<Vec<Organization>, AppError> {
    state.api_service.get_organizations(&session_key).await
}
