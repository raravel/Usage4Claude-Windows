#![allow(dead_code)]

use crate::models::account::Account;
use crate::models::error::AppError;

const SERVICE_NAME: &str = "usage4claude";
const ACCOUNTS_KEY: &str = "accounts";

pub struct KeyringService;

impl KeyringService {
    /// 세션키 저장
    pub fn store_session_key(account_id: &str, session_key: &str) -> Result<(), AppError> {
        let entry = keyring::Entry::new(SERVICE_NAME, &format!("session:{}", account_id))
            .map_err(|e| AppError::Keyring(e.to_string()))?;
        entry
            .set_password(session_key)
            .map_err(|e| AppError::Keyring(e.to_string()))
    }

    /// 세션키 조회
    pub fn get_session_key(account_id: &str) -> Result<String, AppError> {
        let entry = keyring::Entry::new(SERVICE_NAME, &format!("session:{}", account_id))
            .map_err(|e| AppError::Keyring(e.to_string()))?;
        entry
            .get_password()
            .map_err(|e| AppError::Keyring(e.to_string()))
    }

    /// 세션키 삭제
    pub fn delete_session_key(account_id: &str) -> Result<(), AppError> {
        let entry = keyring::Entry::new(SERVICE_NAME, &format!("session:{}", account_id))
            .map_err(|e| AppError::Keyring(e.to_string()))?;
        entry
            .delete_credential()
            .map_err(|e| AppError::Keyring(e.to_string()))
    }

    /// 계정 목록 저장 (JSON 직렬화)
    pub fn store_accounts(accounts: &[Account]) -> Result<(), AppError> {
        let json =
            serde_json::to_string(accounts).map_err(|e| AppError::Keyring(e.to_string()))?;
        let entry = keyring::Entry::new(SERVICE_NAME, ACCOUNTS_KEY)
            .map_err(|e| AppError::Keyring(e.to_string()))?;
        entry
            .set_password(&json)
            .map_err(|e| AppError::Keyring(e.to_string()))
    }

    /// 계정 목록 로드
    pub fn load_accounts() -> Result<Vec<Account>, AppError> {
        let entry = keyring::Entry::new(SERVICE_NAME, ACCOUNTS_KEY)
            .map_err(|e| AppError::Keyring(e.to_string()))?;
        match entry.get_password() {
            Ok(json) => serde_json::from_str(&json).map_err(|e| AppError::Keyring(e.to_string())),
            Err(keyring::Error::NoEntry) => Ok(Vec::new()),
            Err(e) => Err(AppError::Keyring(e.to_string())),
        }
    }
}
