use crate::models::account::{Account, DiagnosisResult};
use crate::models::error::AppError;
use crate::services::keyring_store::KeyringService;
use tauri::Emitter;

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

/// 수동 입력으로 새 계정 추가
/// 1. session_key로 조직 목록 조회 (유효성 검증)
/// 2. Account 생성 (UUID)
/// 3. keyring에 계정 목록 + 세션키 저장
/// 4. 트레이 메뉴 재구성
#[tauri::command]
pub async fn add_account(
    state: tauri::State<'_, crate::AppState>,
    app: tauri::AppHandle,
    session_key: String,
    org_id: String,
    display_name: String,
    org_name: String,
) -> Result<Account, AppError> {
    // 조직 목록 조회로 session_key 유효성 확인
    state.api_service.get_organizations(&session_key).await?;

    let new_account = Account {
        id: uuid::Uuid::new_v4().to_string(),
        display_name,
        org_id,
        org_name,
        is_active: false,
    };

    // 기존 계정 목록 로드 후 추가
    let mut accounts = KeyringService::load_accounts()?;

    // 첫 계정이면 활성 계정으로 설정
    let mut account = new_account.clone();
    if accounts.is_empty() {
        account.is_active = true;
    }

    // 세션키 저장
    KeyringService::store_session_key(&account.id, &session_key)?;

    accounts.push(account.clone());
    KeyringService::store_accounts(&accounts)?;

    tracing::info!("Account added: {}", account.display_name);

    // 트레이 메뉴 재구성
    let _ = crate::tray::rebuild_tray_menu(&app);

    // 프론트엔드에 계정 변경 이벤트 emit
    let _ = app.emit("account-changed", ());

    Ok(account)
}

/// 계정 삭제
/// 1. 계정 목록에서 제거
/// 2. 세션키 keyring에서 삭제
/// 3. 삭제된 계정이 활성 계정이었으면 첫 번째 남은 계정을 활성화
/// 4. 계정 목록 저장, 트레이 메뉴 재구성
#[tauri::command]
pub fn remove_account(
    state: tauri::State<'_, crate::AppState>,
    app: tauri::AppHandle,
    account_id: String,
) -> Result<(), AppError> {
    let _ = state; // AppState는 여기서 직접 사용하지 않으나 일관성 유지
    let mut accounts = KeyringService::load_accounts()?;

    let removed = accounts.iter().find(|a| a.id == account_id).cloned();
    accounts.retain(|a| a.id != account_id);

    // 세션키 삭제 (실패해도 계속 진행)
    let _ = KeyringService::delete_session_key(&account_id);

    // 삭제된 계정이 활성 계정이었으면 첫 번째 계정을 활성화
    if let Some(removed_account) = removed {
        if removed_account.is_active && !accounts.is_empty() {
            accounts[0].is_active = true;
        }
    }

    KeyringService::store_accounts(&accounts)?;

    tracing::info!("Account removed: {}", account_id);

    // 트레이 메뉴 재구성
    let _ = crate::tray::rebuild_tray_menu(&app);

    // 프론트엔드에 계정 변경 이벤트 emit
    let _ = app.emit("account-changed", ());

    Ok(())
}

/// 활성 계정 전환
/// 1. 계정 목록에서 대상을 활성화, 나머지를 비활성화
/// 2. 계정 목록 저장, 트레이 메뉴 재구성
/// 3. 즉시 새로고침
#[tauri::command]
pub async fn switch_account(
    state: tauri::State<'_, crate::AppState>,
    app: tauri::AppHandle,
    account_id: String,
) -> Result<(), AppError> {
    let _ = state;
    let mut accounts = KeyringService::load_accounts()?;

    for account in &mut accounts {
        account.is_active = account.id == account_id;
    }

    KeyringService::store_accounts(&accounts)?;

    tracing::info!("Switched to account: {}", account_id);

    // 트레이 메뉴 재구성
    let _ = crate::tray::rebuild_tray_menu(&app);

    // 프론트엔드에 계정 변경 이벤트 emit
    let _ = app.emit("account-changed", ());

    // 즉시 새로고침
    crate::services::refresh::do_refresh(&app).await;

    Ok(())
}

/// 계정 연결 진단
/// API 도달 가능 여부, 세션키 유효성, 조직 정보 반환
#[tauri::command]
pub async fn diagnose_connection(
    state: tauri::State<'_, crate::AppState>,
    account_id: String,
) -> Result<DiagnosisResult, AppError> {
    // 세션키 조회
    let session_key = match KeyringService::get_session_key(&account_id) {
        Ok(key) => key,
        Err(_) => {
            return Ok(DiagnosisResult {
                session_valid: false,
                api_reachable: false,
                organizations: vec![],
                error_message: Some("세션키를 찾을 수 없습니다.".to_string()),
            });
        }
    };

    // 조직 목록 조회로 API 도달 가능 여부 + 세션키 유효성 검증
    match state.api_service.get_organizations(&session_key).await {
        Ok(orgs) => Ok(DiagnosisResult {
            session_valid: true,
            api_reachable: true,
            organizations: orgs,
            error_message: None,
        }),
        Err(AppError::Unauthorized) => Ok(DiagnosisResult {
            session_valid: false,
            api_reachable: true,
            organizations: vec![],
            error_message: Some("세션키가 유효하지 않습니다 (401).".to_string()),
        }),
        Err(AppError::Forbidden) => Ok(DiagnosisResult {
            session_valid: false,
            api_reachable: true,
            organizations: vec![],
            error_message: Some("접근이 거부되었습니다 (403).".to_string()),
        }),
        Err(AppError::CloudflareBlock) => Ok(DiagnosisResult {
            session_valid: false,
            api_reachable: false,
            organizations: vec![],
            error_message: Some("Cloudflare 차단이 감지되었습니다.".to_string()),
        }),
        Err(AppError::Network(msg)) => Ok(DiagnosisResult {
            session_valid: false,
            api_reachable: false,
            organizations: vec![],
            error_message: Some(format!("네트워크 오류: {}", msg)),
        }),
        Err(e) => Ok(DiagnosisResult {
            session_valid: false,
            api_reachable: false,
            organizations: vec![],
            error_message: Some(e.to_string()),
        }),
    }
}
