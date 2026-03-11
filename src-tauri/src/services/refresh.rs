// REVIEW: PASS
use std::time::Duration;
use tokio::time;
use tokio_util::sync::CancellationToken;
use tauri::{AppHandle, Emitter, Manager};
use crate::AppState;
use crate::services::keyring_store::KeyringService;

/// 새로고침 서비스 시작
pub fn start_refresh_service(
    app: AppHandle,
    cancel_token: CancellationToken,
) -> tokio::task::JoinHandle<()> {
    tokio::spawn(async move {
        let mut interval = time::interval(Duration::from_secs(60)); // 기본 1분

        loop {
            tokio::select! {
                _ = cancel_token.cancelled() => {
                    break; // 앱 종료 시 루프 탈출
                }
                _ = interval.tick() => {
                    do_refresh(&app).await;
                }
            }
        }
    })
}

/// 실제 새로고침 로직
pub async fn do_refresh(app: &AppHandle) {
    let state = app.state::<AppState>();

    // 1. 현재 활성 계정 조회
    let accounts = match KeyringService::load_accounts() {
        Ok(accounts) => accounts,
        Err(_) => return,
    };

    let active_account = match accounts.iter().find(|a| a.is_active) {
        Some(account) => account.clone(),
        None => return, // 활성 계정 없으면 스킵
    };

    // 2. 세션키 조회
    let session_key = match KeyringService::get_session_key(&active_account.id) {
        Ok(key) => key,
        Err(_) => return,
    };

    // 3. API 호출
    match state.api_service.fetch_all_usage(&active_account.org_id, &session_key).await {
        Ok(data) => {
            // 성공: 에러 카운트 리셋
            if let Ok(mut count) = state.consecutive_errors.lock() {
                *count = 0;
            }
            // AppState 업데이트
            if let Ok(mut usage) = state.usage.lock() {
                *usage = Some(data.clone());
            }
            // 프론트엔드에 이벤트 발생
            let _ = app.emit("usage-updated", &data);
        }
        Err(e) => {
            // 실패: 에러 카운트 증가
            if let Ok(mut count) = state.consecutive_errors.lock() {
                *count += 1;
            }
            let _ = app.emit("api-error", e.to_string());
        }
    }
}

/// 수동 새로고침 (쿨다운 10초)
pub async fn manual_refresh(app: &AppHandle) -> Result<(), String> {
    let state = app.state::<AppState>();

    // 쿨다운 확인
    {
        let last = state.last_refresh.lock().map_err(|e| e.to_string())?;
        if let Some(last_time) = *last {
            if last_time.elapsed() < Duration::from_secs(10) {
                return Err("Refresh cooldown (10s)".to_string());
            }
        }
    }

    // 쿨다운 업데이트
    {
        let mut last = state.last_refresh.lock().map_err(|e| e.to_string())?;
        *last = Some(std::time::Instant::now());
    }

    do_refresh(app).await;
    Ok(())
}
