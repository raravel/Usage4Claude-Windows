use std::time::Duration;
use tokio_util::sync::CancellationToken;
use tauri::{AppHandle, Emitter, Manager};
use tauri_plugin_notification::NotificationExt;
use crate::AppState;
use crate::models::settings::RefreshMode;
use crate::models::usage::UsageData;
use crate::services::icon_renderer::RENDERED_ICON_SIZE;
use crate::services::keyring_store::KeyringService;
use crate::services::smart_monitor::SmartMonitor;

/// 새로고침 서비스 시작
pub fn start_refresh_service(
    app: AppHandle,
    cancel_token: CancellationToken,
) -> tokio::task::JoinHandle<()> {
    tokio::spawn(async move {
        let mut monitor = SmartMonitor::new();

        // 시작 시 즉시 첫 새로고침
        let mut interval_duration = do_refresh_with_monitor(&app, &mut monitor).await;

        loop {
            tokio::select! {
                _ = cancel_token.cancelled() => {
                    break; // 앱 종료 시 루프 탈출
                }
                _ = tokio::time::sleep(interval_duration) => {
                    interval_duration = do_refresh_with_monitor(&app, &mut monitor).await;
                }
            }
        }
    })
}

/// 새로고침 실행 후 다음 폴링 간격을 반환
async fn do_refresh_with_monitor(app: &AppHandle, monitor: &mut SmartMonitor) -> Duration {
    let state = app.state::<AppState>();

    // 설정에서 RefreshMode 및 고정 간격 읽기
    let (refresh_mode, fixed_interval_mins) = {
        let settings = state.settings.lock().unwrap();
        (settings.refresh_mode.clone(), settings.refresh_interval_minutes)
    };

    // 1. 현재 활성 계정 조회
    let accounts = match KeyringService::load_accounts() {
        Ok(accounts) => accounts,
        Err(_) => {
            // 계정 로드 실패 — 현재 간격 유지
            return current_interval(&refresh_mode, fixed_interval_mins, monitor);
        }
    };

    let active_account = match accounts.iter().find(|a| a.is_active) {
        Some(account) => account.clone(),
        None => {
            // 활성 계정 없음 — 현재 간격 유지
            return current_interval(&refresh_mode, fixed_interval_mins, monitor);
        }
    };

    // 2. 세션키 조회
    let session_key = match KeyringService::get_session_key(&active_account.id) {
        Ok(key) => key,
        Err(_) => {
            return current_interval(&refresh_mode, fixed_interval_mins, monitor);
        }
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

            // 트레이 아이콘 업데이트
            update_tray_icon(app, &data);

            // 트레이 툴팁 업데이트
            crate::tray::update_tray_tooltip(app, &data);

            // 알림 확인 및 전송
            check_and_send_notifications(app, &data);

            // 다음 간격 결정
            match refresh_mode {
                RefreshMode::Smart => {
                    // 모든 limit 중 최대 percentage 계산
                    let max_pct = data
                        .limits
                        .iter()
                        .map(|l| l.percentage)
                        .chain(data.extra.iter().map(|e| e.percentage))
                        .fold(f64::NEG_INFINITY, f64::max);
                    let max_pct = if max_pct.is_infinite() { 0.0 } else { max_pct };
                    monitor.update(max_pct)
                }
                RefreshMode::Fixed => {
                    Duration::from_secs(fixed_interval_mins as u64 * 60)
                }
            }
        }
        Err(e) => {
            // 실패: 에러 카운트 증가
            if let Ok(mut count) = state.consecutive_errors.lock() {
                *count += 1;
            }
            let _ = app.emit("api-error", e.to_string());
            // 실패 시 현재 간격 유지
            current_interval(&refresh_mode, fixed_interval_mins, monitor)
        }
    }
}

/// 현재 모드에 따른 간격 반환 (상태 변경 없음)
fn current_interval(mode: &RefreshMode, fixed_mins: u32, monitor: &SmartMonitor) -> Duration {
    match mode {
        RefreshMode::Smart => monitor.current_state().interval(),
        RefreshMode::Fixed => Duration::from_secs(fixed_mins as u64 * 60),
    }
}

/// 실제 새로고침 로직 (외부 호출용, SmartMonitor 없이)
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

            // 트레이 아이콘 업데이트
            update_tray_icon(app, &data);

            // 트레이 툴팁 업데이트
            crate::tray::update_tray_tooltip(app, &data);

            // 알림 확인 및 전송
            check_and_send_notifications(app, &data);
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

/// 사용량 데이터를 확인하고 필요한 알림을 전송한다
// REVIEW: PASS — do_refresh_with_monitor와 do_refresh 두 경로 모두에서 호출됨. NotificationExt::notification().builder()로 Windows Toast 알림 전송, 실패 시 _ = 로 무시해 앱 흐름을 방해하지 않음.
fn check_and_send_notifications(app: &AppHandle, data: &UsageData) {
    let state = app.state::<AppState>();
    let settings = state.settings.lock().unwrap().clone();
    let mut tracker = state.notification_tracker.lock().unwrap();
    let notifications = tracker.check(data, &settings);

    for notif in notifications {
        let _ = app.notification()
            .builder()
            .title(&notif.title)
            .body(&notif.body)
            .show();
    }
}

/// 사용 데이터를 기반으로 트레이 아이콘을 업데이트한다
fn update_tray_icon(app: &AppHandle, data: &crate::models::usage::UsageData) {
    let state = app.state::<AppState>();

    // 설정 읽기
    let (display_mode, icon_theme) = {
        let settings = state.settings.lock().unwrap();
        (settings.display_mode.clone(), settings.icon_theme.clone())
    };

    // 가장 높은 사용률의 limit 선택 (limits + extra 통합)
    let max_limit = data
        .limits
        .iter()
        .chain(data.extra.iter())
        .max_by(|a, b| a.percentage.partial_cmp(&b.percentage).unwrap_or(std::cmp::Ordering::Equal));

    let Some(limit) = max_limit else {
        return;
    };

    // 아이콘 렌더링
    let rgba = {
        let mut renderer = state.icon_renderer.lock().unwrap();
        renderer.render(limit.percentage, &limit.limit_type, &icon_theme, &display_mode)
    };

    // 트레이 아이콘 설정: lock → 사용 → drop 순서를 명시적으로 관리
    {
        let tray_guard = state.tray_icon.lock().unwrap();
        if let Some(tray_icon) = tray_guard.as_ref() {
            let icon = tauri::image::Image::new_owned(
                rgba,
                RENDERED_ICON_SIZE,
                RENDERED_ICON_SIZE,
            );
            let _ = tray_icon.set_icon(Some(icon));
        }
    }
}
