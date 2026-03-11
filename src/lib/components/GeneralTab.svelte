<script lang="ts">
  import type { UserSettings } from '$lib/types';
  import { updateSettings } from '$lib/api';
  import { enable, disable } from '@tauri-apps/plugin-autostart';

  let { settings = $bindable() }: { settings: UserSettings } = $props();

  async function save() {
    await updateSettings(settings);
  }

  async function handleLaunchAtLogin() {
    await save();
    if (settings.launchAtLogin) {
      await enable();
    } else {
      await disable();
    }
  }
</script>

<div class="general-tab">
  <!-- 표시 설정 -->
  <section class="settings-group">
    <h3>표시 설정</h3>
    <div class="setting-row">
      <label for="displayMode">표시 모드</label>
      <select id="displayMode" bind:value={settings.displayMode} onchange={save}>
        <option value="percentOnly">퍼센트만</option>
        <option value="iconOnly">아이콘만</option>
        <option value="iconAndPercent">아이콘 + 퍼센트</option>
      </select>
    </div>
    <div class="setting-row">
      <label for="iconTheme">아이콘 테마</label>
      <select id="iconTheme" bind:value={settings.iconTheme} onchange={save}>
        <option value="colorTranslucent">컬러 투명</option>
        <option value="colorWithBackground">컬러 배경</option>
        <option value="monochrome">모노크롬</option>
      </select>
    </div>
    <div class="setting-row">
      <label for="displayContent">표시 내용</label>
      <select id="displayContent" bind:value={settings.displayContent} onchange={save}>
        <option value="smart">스마트 (자동)</option>
        <option value="custom">커스텀</option>
      </select>
    </div>
  </section>

  <!-- 새로고침 -->
  <section class="settings-group">
    <h3>새로고침</h3>
    <div class="setting-row">
      <label for="refreshMode">새로고침 모드</label>
      <select id="refreshMode" bind:value={settings.refreshMode} onchange={save}>
        <option value="smart">스마트 모니터링</option>
        <option value="fixed">고정 간격</option>
      </select>
    </div>
    {#if settings.refreshMode === 'fixed'}
      <div class="setting-row">
        <label for="refreshInterval">간격 (분)</label>
        <select id="refreshInterval" bind:value={settings.refreshIntervalMinutes} onchange={save}>
          {#each [1, 2, 3, 5, 10, 15, 20, 30] as min}
            <option value={min}>{min}분</option>
          {/each}
        </select>
      </div>
    {/if}
  </section>

  <!-- 외관 -->
  <section class="settings-group">
    <h3>외관</h3>
    <div class="setting-row">
      <label for="theme">테마</label>
      <select id="theme" bind:value={settings.theme} onchange={save}>
        <option value="system">시스템</option>
        <option value="light">밝음</option>
        <option value="dark">어두움</option>
      </select>
    </div>
    <div class="setting-row">
      <label for="timeFormat">시간 형식</label>
      <select id="timeFormat" bind:value={settings.timeFormat} onchange={save}>
        <option value="system">시스템</option>
        <option value="twelveHour">12시간</option>
        <option value="twentyFourHour">24시간</option>
      </select>
    </div>
    <div class="setting-row">
      <label for="language">언어</label>
      <select id="language" bind:value={settings.language} onchange={save}>
        <option value="en">English</option>
        <option value="ko">한국어</option>
        <option value="ja">日本語</option>
        <option value="zh-Hans">简体中文</option>
        <option value="zh-Hant">繁體中文</option>
      </select>
    </div>
  </section>

  <!-- 시스템 -->
  <section class="settings-group">
    <h3>시스템</h3>
    <div class="setting-row checkbox">
      <label>
        <input type="checkbox" bind:checked={settings.launchAtLogin} onchange={handleLaunchAtLogin} />
        Windows 시작 시 자동 실행
      </label>
    </div>
    <div class="setting-row checkbox">
      <label>
        <input type="checkbox" bind:checked={settings.notificationsEnabled} onchange={save} />
        사용량 경고 알림
      </label>
    </div>
    <div class="setting-row checkbox">
      <label>
        <input type="checkbox" bind:checked={settings.resetNotifications} onchange={save} />
        리셋 알림
      </label>
    </div>
  </section>
</div>

<style>
  .general-tab {
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

  .setting-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    min-height: 32px;
  }

  .setting-row label {
    font-size: 13px;
    color: #e0e0e0;
    flex: 1;
  }

  .setting-row select {
    background: #3a3a3a;
    border: 1px solid #555;
    border-radius: 4px;
    color: #e0e0e0;
    font-size: 13px;
    padding: 4px 8px;
    min-width: 160px;
    cursor: pointer;
    outline: none;
    transition: border-color 0.15s;
  }

  .setting-row select:hover {
    border-color: #0078d4;
  }

  .setting-row select:focus {
    border-color: #0078d4;
    box-shadow: 0 0 0 2px rgba(0, 120, 212, 0.3);
  }

  .setting-row.checkbox {
    justify-content: flex-start;
  }

  .setting-row.checkbox label {
    display: flex;
    align-items: center;
    gap: 10px;
    cursor: pointer;
    user-select: none;
  }

  .setting-row.checkbox input[type='checkbox'] {
    width: 16px;
    height: 16px;
    cursor: pointer;
    accent-color: #0078d4;
  }
</style>
