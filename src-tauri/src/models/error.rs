#![allow(dead_code)]

use serde::Serialize;
use thiserror::Error;

#[derive(Error, Debug)]
pub enum AppError {
    #[error("Authentication failed (401)")]
    Unauthorized,
    #[error("Access denied (403) - possible Cloudflare block")]
    Forbidden,
    #[error("Rate limited (429) - retry after {retry_after}s")]
    RateLimited { retry_after: u64 },
    #[error("Cloudflare HTML response detected")]
    CloudflareBlock,
    #[error("Network error: {0}")]
    Network(String),
    #[error("Parse error: {0}")]
    Parse(String),
    #[error("Keyring error: {0}")]
    Keyring(String),
    #[error("Settings error: {0}")]
    Settings(String),
}

// Tauri command에서 사용하기 위한 Serialize 구현
impl Serialize for AppError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}
