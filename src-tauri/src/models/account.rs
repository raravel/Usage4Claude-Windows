#![allow(dead_code)]

use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Account {
    pub id: String,          // UUID
    pub display_name: String,
    pub org_id: String,
    pub org_name: String,
    pub is_active: bool,
}

/// Claude API 조직 응답
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Organization {
    pub uuid: String,
    pub name: String,
}

/// 연결 진단 결과
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DiagnosisResult {
    pub session_valid: bool,
    pub api_reachable: bool,
    pub organizations: Vec<Organization>,
    pub error_message: Option<String>,
}
