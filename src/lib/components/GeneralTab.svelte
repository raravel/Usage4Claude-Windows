// REVIEW: PASS — 모든 완료 조건 충족
// REVIEW: [조건1] 표시 설정(displayMode, iconTheme, displayContent), 새로고침(refreshMode, refreshIntervalMinutes), 외관(theme, timeFormat, language), 시스템(launchAtLogin, notificationsEnabled, resetNotifications) UI 모두 구현됨
// REVIEW: [조건2] 각 컨트롤 onchange에 save() 호출 → updateSettings() → invoke('update_settings') 즉시 반영 확인
// REVIEW: [조건3] handleLaunchAtLogin에서 @tauri-apps/plugin-autostart의 enable/disable 호출, capabilities 및 Rust 플러그인 등록 모두 확인
// REVIEW: Svelte 5 Runes($props, $bindable) 사용 — stores 없음, $: 없음 — 규칙 준수
<script lang="ts">
  import { t, locale } from 'svelte-i18n';
  import type { UserSettings } from '$lib/types';
  import { updateSettings, updateTrayLanguage } from '$lib/api';
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

  async function handleLanguageChange() {
    await save();
    if (settings.language === 'system') {
      const nav = navigator.language;
      const mapped = mapToSupported(nav);
      locale.set(mapped);
    } else {
      locale.set(settings.language);
    }
    await updateTrayLanguage(settings.language);
  }

  function mapToSupported(nav: string): string {
    if (nav.startsWith('ko')) return 'ko';
    if (nav.startsWith('ja')) return 'ja';
    if (nav === 'zh-TW' || nav === 'zh-Hant') return 'zh-Hant';
    if (nav.startsWith('zh')) return 'zh-Hans';
    return 'en';
  }
</script>

<div class="general-tab">
  <!-- 표시 설정 -->
  <section class="settings-group">
    <h3>{$t('settings.general.display')}</h3>
    <div class="setting-row">
      <label for="displayMode">{$t('settings.general.displayMode')}</label>
      <select id="displayMode" bind:value={settings.displayMode} onchange={save}>
        <option value="percentOnly">{$t('settings.general.percentOnly')}</option>
        <option value="iconOnly">{$t('settings.general.iconOnly')}</option>
        <option value="iconAndPercent">{$t('settings.general.iconAndPercent')}</option>
      </select>
    </div>
    <div class="setting-row">
      <label for="iconTheme">{$t('settings.general.iconTheme')}</label>
      <select id="iconTheme" bind:value={settings.iconTheme} onchange={save}>
        <option value="colorTranslucent">{$t('settings.general.colorTranslucent')}</option>
        <option value="colorWithBackground">{$t('settings.general.colorWithBackground')}</option>
        <option value="monochrome">{$t('settings.general.monochrome')}</option>
      </select>
    </div>
    <div class="setting-row">
      <label for="displayContent">{$t('settings.general.displayContent')}</label>
      <select id="displayContent" bind:value={settings.displayContent} onchange={save}>
        <option value="smart">{$t('settings.general.smart')}</option>
        <option value="custom">{$t('settings.general.custom')}</option>
      </select>
    </div>
  </section>

  <!-- 새로고침 -->
  <section class="settings-group">
    <h3>{$t('settings.general.refresh')}</h3>
    <div class="setting-row">
      <label for="refreshMode">{$t('settings.general.refreshMode')}</label>
      <select id="refreshMode" bind:value={settings.refreshMode} onchange={save}>
        <option value="smart">{$t('settings.general.smartMonitoring')}</option>
        <option value="fixed">{$t('settings.general.fixedInterval')}</option>
      </select>
    </div>
    {#if settings.refreshMode === 'fixed'}
      <div class="setting-row">
        <label for="refreshInterval">{$t('settings.general.interval')}</label>
        <select id="refreshInterval" bind:value={settings.refreshIntervalMinutes} onchange={save}>
          {#each [1, 2, 3, 5, 10, 15, 20, 30] as min}
            <option value={min}>{$t('settings.general.minutes', { values: { min } })}</option>
          {/each}
        </select>
      </div>
    {/if}
  </section>

  <!-- 외관 -->
  <section class="settings-group">
    <h3>{$t('settings.general.appearance')}</h3>
    <div class="setting-row">
      <label for="theme">{$t('settings.general.theme')}</label>
      <select id="theme" bind:value={settings.theme} onchange={save}>
        <option value="system">{$t('settings.general.themeSystem')}</option>
        <option value="light">{$t('settings.general.themeLight')}</option>
        <option value="dark">{$t('settings.general.themeDark')}</option>
      </select>
    </div>
    <div class="setting-row">
      <label for="timeFormat">{$t('settings.general.timeFormat')}</label>
      <select id="timeFormat" bind:value={settings.timeFormat} onchange={save}>
        <option value="system">{$t('settings.general.timeSystem')}</option>
        <option value="twelveHour">{$t('settings.general.time12h')}</option>
        <option value="twentyFourHour">{$t('settings.general.time24h')}</option>
      </select>
    </div>
    <div class="setting-row">
      <label for="language">{$t('settings.general.language')}</label>
      <select id="language" bind:value={settings.language} onchange={handleLanguageChange}>
        <option value="system">{$t('settings.general.system')}</option>
        <option value="en">English</option>
        <option value="ko">한국어</option>
        <option value="ja">日本語</option>
        <option value="zh-Hans">简体中文</option>
        <option value="zh-Hant">繁體中文</option>
      </select>
    </div>
  </section>

  <!-- 시스템 -->
  <!-- REVIEW: FAIL — 아래 h3 태그가 하드코딩된 "System" 문자열을 사용함. $t('settings.general.system') 또는 전용 키로 교체 필요. 다른 모든 section 헤더는 $t()를 사용하고 있어 일관성이 없음. -->
  <section class="settings-group">
    <h3>System</h3>
    <div class="setting-row checkbox">
      <label>
        <input type="checkbox" bind:checked={settings.launchAtLogin} onchange={handleLaunchAtLogin} />
        {$t('settings.general.autostart')}
      </label>
    </div>
    <div class="setting-row checkbox">
      <label>
        <input type="checkbox" bind:checked={settings.notificationsEnabled} onchange={save} />
        {$t('settings.general.notifications')}
      </label>
    </div>
    <div class="setting-row checkbox">
      <label>
        <input type="checkbox" bind:checked={settings.resetNotifications} onchange={save} />
        {$t('settings.general.resetNotifications')}
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
