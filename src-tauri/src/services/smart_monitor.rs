// REVIEW: PASS
use std::time::Duration;

#[derive(Debug, Clone, PartialEq)]
pub enum MonitorState {
    Active,     // 1분
    ShortIdle,  // 3분
    MediumIdle, // 5분
    LongIdle,   // 10분
}

impl MonitorState {
    pub fn interval(&self) -> Duration {
        match self {
            MonitorState::Active => Duration::from_secs(60),
            MonitorState::ShortIdle => Duration::from_secs(180),
            MonitorState::MediumIdle => Duration::from_secs(300),
            MonitorState::LongIdle => Duration::from_secs(600),
        }
    }
}

pub struct SmartMonitor {
    state: MonitorState,
    unchanged_count: u32,
    last_percentage: Option<f64>,
}

const CHANGE_THRESHOLD: f64 = 0.01; // 0.01%

impl SmartMonitor {
    pub fn new() -> Self {
        Self {
            state: MonitorState::Active,
            unchanged_count: 0,
            last_percentage: None,
        }
    }

    /// 새로운 사용량 데이터를 받아서 상태를 업데이트하고, 다음 간격을 반환
    pub fn update(&mut self, max_percentage: f64) -> Duration {
        let changed = match self.last_percentage {
            Some(last) => (max_percentage - last).abs() > CHANGE_THRESHOLD,
            None => false, // 첫 번째 데이터는 변화 없음으로 처리
        };

        self.last_percentage = Some(max_percentage);

        if changed {
            self.state = MonitorState::Active;
            self.unchanged_count = 0;
        } else {
            self.unchanged_count += 1;
            self.state = match self.unchanged_count {
                0..=2 => MonitorState::Active,
                3..=5 => MonitorState::ShortIdle,
                6..=11 => MonitorState::MediumIdle,
                _ => MonitorState::LongIdle,
            };
        }

        self.state.interval()
    }

    pub fn current_state(&self) -> &MonitorState {
        &self.state
    }

    /// Active 상태로 강제 복귀 (수동 새로고침 등)
    #[allow(dead_code)]
    pub fn reset(&mut self) {
        self.state = MonitorState::Active;
        self.unchanged_count = 0;
    }
}
