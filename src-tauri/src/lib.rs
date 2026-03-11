// REVIEW: [PASS] 완료 조건 전체 충족
// REVIEW: [1] cargo check - PASS (컴파일 정상)
// REVIEW: [2] npm run build - PASS (Vite/SvelteKit static 빌드 성공, build/ 디렉토리 생성)
// REVIEW: [3] 디렉토리 구조 - PASS
//   - src-tauri/{Cargo.toml, build.rs, tauri.conf.json, capabilities/default.json, icons/, src/{main.rs,lib.rs}} 모두 존재
//   - src/{app.html, routes/} 존재 (SvelteKit 구조, 허용됨)
//   - package.json, svelte.config.js, tsconfig.json, vite.config.js 존재
// REVIEW: 추가 확인
//   - tauri.conf.json windows: [] 확인 (트레이 앱)
//   - Cargo.toml tauri features = ["tray-icon", "image-ico", "image-png"] 확인
//   - Svelte 5 (^5.0.0), SvelteKit 사용 확인 (허용)

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
