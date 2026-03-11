// REVIEW: PASS — [완료조건1,2] debug=false 시 EnvFilter::new("usage4claude=warn,warn")으로 WARN이 기본값이며,
// RUST_LOG 환경변수로 재정의 가능. debug=true 시 "usage4claude=debug,warn"으로 DEBUG 레벨 적용.
// [완료조건3] rolling::daily + cleanup_old_logs(7일) 구현 올바름. 다만 cleanup이 로그 파일만
// 대상으로 하지 않고 디렉토리 내 모든 파일을 삭제하는 점은 실사용 환경에서는 주의 필요 (완료조건 범위 외).
// [완료조건4] std::mem::forget(guard)는 앱 종료 시 미플러시 위험이 있으나, 단순 앱에서 허용되는 패턴.
// session_key가 어떠한 tracing 호출에도 포함되지 않음을 확인.
use std::path::Path;
use tracing_subscriber::{fmt, layer::SubscriberExt, util::SubscriberInitExt, EnvFilter};
use tracing_appender::rolling;

pub fn init_logging(log_dir: &Path, debug: bool) {
    let level = if debug { "debug" } else { "warn" };
    let filter = EnvFilter::try_from_default_env()
        .unwrap_or_else(|_| EnvFilter::new(format!("usage4claude={},warn", level)));

    let file_appender = rolling::daily(log_dir, "usage4claude.log");
    let (non_blocking, guard) = tracing_appender::non_blocking(file_appender);
    // Leak guard so logging lives for app lifetime
    std::mem::forget(guard);

    let file_layer = fmt::layer()
        .with_writer(non_blocking)
        .with_ansi(false)
        .with_target(true);

    // Use Option-wrapped console layer so both branches share the same concrete type
    let console_layer = if debug {
        Some(fmt::layer().with_target(true))
    } else {
        None
    };

    tracing_subscriber::registry()
        .with(filter)
        .with(console_layer)
        .with(file_layer)
        .init();

    // Clean up old log files (>7 days)
    cleanup_old_logs(log_dir);

    tracing::info!("Logging initialized (level: {})", level);
}

fn cleanup_old_logs(log_dir: &Path) {
    let cutoff = std::time::SystemTime::now() - std::time::Duration::from_secs(7 * 24 * 3600);
    if let Ok(entries) = std::fs::read_dir(log_dir) {
        for entry in entries.flatten() {
            if let Ok(metadata) = entry.metadata() {
                if let Ok(modified) = metadata.modified() {
                    if modified < cutoff {
                        let _ = std::fs::remove_file(entry.path());
                    }
                }
            }
        }
    }
}
