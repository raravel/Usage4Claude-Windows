<script lang="ts">
  import { onMount } from 'svelte';
  import { getAppVersion } from '$lib/api';
  import { open } from '@tauri-apps/plugin-shell';

  let version = $state('...');

  onMount(async () => {
    version = await getAppVersion();
  });

  async function openLink(url: string) {
    await open(url);
  }
</script>

<div class="about-tab">
  <!-- App Info -->
  <section class="settings-group hero-section">
    <div class="app-icon">U4C</div>
    <div class="app-title">Usage4Claude for Windows</div>
    <div class="app-version">버전 {version}</div>
  </section>

  <!-- External Links -->
  <section class="settings-group">
    <h3>링크</h3>
    <div class="link-row">
      <span class="link-label">GitHub 저장소</span>
      <button
        class="link-button"
        onclick={() => openLink('https://github.com/raravel/Usage4Claude-Windows')}
      >
        raravel/Usage4Claude-Windows
      </button>
    </div>
    <div class="link-row">
      <span class="link-label">원본 macOS 프로젝트</span>
      <button
        class="link-button"
        onclick={() => openLink('https://github.com/f-is-h/Usage4Claude')}
      >
        f-is-h/Usage4Claude
      </button>
    </div>
    <div class="link-row">
      <span class="link-label">Ko-fi 후원</span>
      <button
        class="link-button"
        onclick={() => openLink('https://ko-fi.com/raravel')}
      >
        ko-fi.com/raravel
      </button>
    </div>
    <div class="link-row">
      <span class="link-label">GitHub Sponsors</span>
      <button
        class="link-button"
        onclick={() => openLink('https://github.com/sponsors/raravel')}
      >
        github.com/sponsors/raravel
      </button>
    </div>
  </section>

  <!-- Credits -->
  <section class="settings-group">
    <h3>크레딧</h3>
    <div class="credit-row">
      <span class="credit-text">원본 macOS 앱 제작</span>
      <button
        class="link-button"
        onclick={() => openLink('https://github.com/f-is-h/Usage4Claude')}
      >
        f-is-h
      </button>
    </div>
    <div class="credit-row">
      <span class="credit-text">Windows 포팅</span>
      <span class="credit-value">raravel</span>
    </div>
    <div class="credit-row">
      <span class="credit-text">라이선스</span>
      <span class="credit-value">MIT License</span>
    </div>
  </section>
</div>

<style>
  .about-tab {
    padding: 16px;
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .settings-group {
    background: #2a2a2a;
    border-radius: 8px;
    padding: 12px 16px;
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  .settings-group h3 {
    margin: 0 0 8px 0;
    font-size: 12px;
    font-weight: 600;
    color: #888;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }

  .hero-section {
    align-items: center;
    padding: 24px 16px;
    gap: 6px;
  }

  .app-icon {
    width: 64px;
    height: 64px;
    background: #0078d4;
    border-radius: 14px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 20px;
    font-weight: 700;
    color: #ffffff;
    letter-spacing: -1px;
    margin-bottom: 8px;
  }

  .app-title {
    font-size: 16px;
    font-weight: 600;
    color: #e0e0e0;
  }

  .app-version {
    font-size: 13px;
    color: #888;
  }

  .link-row,
  .credit-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    min-height: 32px;
  }

  .link-label,
  .credit-text {
    font-size: 13px;
    color: #e0e0e0;
    flex: 1;
  }

  .credit-value {
    font-size: 13px;
    color: #888;
  }

  .link-button {
    background: none;
    border: none;
    cursor: pointer;
    font-size: 13px;
    color: #0078d4;
    padding: 4px 0;
    text-decoration: underline;
    text-underline-offset: 2px;
    transition: color 0.15s;
  }

  .link-button:hover {
    color: #2196f3;
  }

  .link-button:active {
    color: #005fa3;
  }
</style>
