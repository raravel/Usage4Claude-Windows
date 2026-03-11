use crate::models::account::Account;
use crate::models::error::AppError;
use crate::services::keyring_store::KeyringService;

#[tauri::command]
pub fn get_accounts() -> Result<Vec<Account>, AppError> {
    KeyringService::load_accounts()
}

#[tauri::command]
pub fn validate_session(account_id: String) -> Result<bool, AppError> {
    match KeyringService::get_session_key(&account_id) {
        Ok(_) => Ok(true),
        Err(_) => Ok(false),
    }
}
