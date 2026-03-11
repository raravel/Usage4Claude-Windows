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
