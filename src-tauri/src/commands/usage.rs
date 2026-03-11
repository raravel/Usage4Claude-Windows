use tauri::State;
use crate::AppState;
use crate::models::usage::UsageData;
use crate::models::account::Organization;
use crate::models::error::AppError;

#[tauri::command]
pub async fn fetch_usage(
    state: State<'_, AppState>,
    org_id: String,
    session_key: String,
) -> Result<UsageData, AppError> {
    state.api_service.fetch_all_usage(&org_id, &session_key).await
}

#[tauri::command]
pub async fn fetch_organizations(
    state: State<'_, AppState>,
    session_key: String,
) -> Result<Vec<Organization>, AppError> {
    state.api_service.get_organizations(&session_key).await
}
