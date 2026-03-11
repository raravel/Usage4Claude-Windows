<script lang="ts">
  // REVIEW: PASS — [완료조건1] /welcome 라우트의 위저드 페이지. 4단계(환영·계정·테마·완료)로 구성됨.
  // REVIEW: PASS — [완료조건2] openLoginWindow + polling(getLoginResult) 브라우저 로그인 구현 확인.
  // REVIEW: PASS — [완료조건3] 테마·displayMode·iconTheme 셀렉터 + saveSettings(updateSettings 호출) 구현 확인.
  // REVIEW: PASS — [완료조건4] finish()/skipAll() 모두 completeFirstLaunch() 호출 후 창 닫기 → 재실행 시 미표시 보장.
  // REVIEW: PASS — Svelte 5 Runes($state, $derived 미사용 시 $state만) 사용. $: 반응식 없음.
  import { onMount, onDestroy } from 'svelte';
  import { t } from 'svelte-i18n';
  import { getCurrentWindow } from '@tauri-apps/api/window';
  import type { Organization, UserSettings } from '$lib/types';
  import {
    completeFirstLaunch,
    getSettings,
    updateSettings,
    fetchOrganizations,
    addAccount,
    openLoginWindow,
    closeLoginWindow,
    getLoginResult
  } from '$lib/api';

  // Wizard step: 1=Welcome, 2=Account, 3=Theme, 4=Complete
  let step = $state(1);

  // Settings (loaded in onMount, used in step 3)
  let settings = $state<UserSettings | null>(null);

  // ── Step 2: Account setup ──────────────────────────────────
  let formSessionKey = $state('');
  let showSessionKey = $state(false);
  let formOrgs = $state<Organization[]>([]);
  let formSelectedOrgId = $state('');
  let formDisplayName = $state('');
  let formLoading = $state(false);
  let formError = $state<string | null>(null);
  let formStep = $state<'input' | 'select-org'>('input');

  // Browser login state
  let browserLoginActive = $state(false);
  let browserLoginStatus = $state('');
  let browserLoginOrgs = $state<Organization[]>([]);
  let browserLoginSessionKey = $state('');
  let browserLoginSelectedOrgId = $state('');
  let browserLoginDisplayName = $state('');
  let showBrowserOrgSelect = $state(false);
  let loginPollingTimer: ReturnType<typeof setInterval> | null = null;

  onMount(async () => {
    settings = await getSettings();
  });

  onDestroy(() => {
    stopLoginPolling();
  });

  async function finish() {
    await completeFirstLaunch();
    await getCurrentWindow().close();
  }

  async function skipAll() {
    await completeFirstLaunch();
    await getCurrentWindow().close();
  }

  // ── Account step helpers ───────────────────────────────────

  async function handleFetchOrgs() {
    if (!formSessionKey.trim()) {
      formError = $t('settings.auth.sessionKeyPlaceholder');
      return;
    }
    formLoading = true;
    formError = null;
    try {
      formOrgs = await fetchOrganizations(formSessionKey.trim());
      if (formOrgs.length === 0) {
        formError = $t('settings.auth.noOrgsFound');
        return;
      }
      formSelectedOrgId = formOrgs[0].uuid;
      formDisplayName = formOrgs[0].name;
      formStep = 'select-org';
    } catch (e) {
      formError = String(e);
    } finally {
      formLoading = false;
    }
  }

  function handleOrgChange() {
    const org = formOrgs.find((o) => o.uuid === formSelectedOrgId);
    if (org) formDisplayName = org.name;
  }

  async function handleAddAccount() {
    if (!formSelectedOrgId || !formDisplayName.trim()) {
      formError = $t('settings.auth.selectOrg');
      return;
    }
    const org = formOrgs.find((o) => o.uuid === formSelectedOrgId);
    if (!org) {
      formError = $t('settings.auth.noOrgsFound');
      return;
    }
    formLoading = true;
    formError = null;
    try {
      await addAccount(formSessionKey.trim(), org.uuid, formDisplayName.trim(), org.name);
      formSessionKey = '';
      formOrgs = [];
      formSelectedOrgId = '';
      formDisplayName = '';
      formStep = 'input';
      // Advance to next step after successful account add
      step = 3;
    } catch (e) {
      formError = String(e);
    } finally {
      formLoading = false;
    }
  }

  function handleFormReset() {
    formStep = 'input';
    formOrgs = [];
    formSelectedOrgId = '';
    formDisplayName = '';
    formError = null;
  }

  // Browser login
  async function handleBrowserLogin() {
    formError = null;
    try {
      browserLoginActive = true;
      browserLoginStatus = $t('settings.auth.openingLogin');
      await openLoginWindow();
      browserLoginStatus = $t('settings.auth.pleaseLogin');
      startLoginPolling();
    } catch (e) {
      formError = String(e);
      browserLoginActive = false;
      browserLoginStatus = '';
    }
  }

  function startLoginPolling() {
    if (loginPollingTimer) clearInterval(loginPollingTimer);
    loginPollingTimer = setInterval(async () => {
      try {
        const sessionKey = await getLoginResult();
        if (sessionKey) {
          stopLoginPolling();
          browserLoginStatus = $t('settings.auth.extracting');
          await handleLoginSuccess(sessionKey);
        }
      } catch {
        // ignore polling errors
      }
    }, 2000);
  }

  function stopLoginPolling() {
    if (loginPollingTimer) {
      clearInterval(loginPollingTimer);
      loginPollingTimer = null;
    }
  }

  async function handleLoginSuccess(sessionKey: string) {
    try {
      const orgs = await fetchOrganizations(sessionKey);
      if (orgs.length === 0) {
        formError = $t('settings.auth.noOrgsFound');
        browserLoginActive = false;
        browserLoginStatus = '';
        await closeLoginWindow();
        return;
      }
      if (orgs.length === 1) {
        browserLoginStatus = $t('settings.auth.registering');
        await addAccount(sessionKey, orgs[0].uuid, orgs[0].name, orgs[0].name);
        await closeLoginWindow();
        browserLoginActive = false;
        browserLoginStatus = '';
        step = 3;
      } else {
        browserLoginOrgs = orgs;
        browserLoginSessionKey = sessionKey;
        browserLoginSelectedOrgId = orgs[0].uuid;
        browserLoginDisplayName = orgs[0].name;
        showBrowserOrgSelect = true;
        browserLoginStatus = $t('settings.auth.selectOrg');
      }
    } catch (e) {
      formError = String(e);
      browserLoginActive = false;
      browserLoginStatus = '';
      await closeLoginWindow();
    }
  }

  async function handleBrowserOrgConfirm() {
    const org = browserLoginOrgs.find((o) => o.uuid === browserLoginSelectedOrgId);
    if (!org) return;
    try {
      await addAccount(
        browserLoginSessionKey,
        org.uuid,
        browserLoginDisplayName.trim() || org.name,
        org.name
      );
      await closeLoginWindow();
      browserLoginActive = false;
      showBrowserOrgSelect = false;
      browserLoginStatus = '';
      browserLoginSessionKey = '';
      browserLoginOrgs = [];
      step = 3;
    } catch (e) {
      formError = String(e);
    }
  }

  function handleBrowserOrgCancel() {
    stopLoginPolling();
    showBrowserOrgSelect = false;
    browserLoginActive = false;
    browserLoginStatus = '';
    browserLoginSessionKey = '';
    browserLoginOrgs = [];
    closeLoginWindow();
  }

  function handleBrowserOrgChange() {
    const org = browserLoginOrgs.find((o) => o.uuid === browserLoginSelectedOrgId);
    if (org) browserLoginDisplayName = org.name;
  }

  // ── Theme step helper ───────────────────────────────────────

  async function saveSettings() {
    if (settings) {
      await updateSettings(settings);
    }
  }
</script>

<div class="wizard">
  <!-- Step indicator -->
  <div class="step-dots">
    {#each [1, 2, 3, 4] as s}
      <span class="dot" class:active={step === s} class:done={step > s}></span>
    {/each}
  </div>

  <!-- ── Step 1: Welcome ─────────────────────────────────── -->
  {#if step === 1}
    <div class="step-content center">
      <div class="app-icon">
        <svg width="64" height="64" viewBox="0 0 64 64" fill="none">
          <rect width="64" height="64" rx="14" fill="#0078d4"/>
          <text x="32" y="44" text-anchor="middle" font-size="32" fill="white">U</text>
        </svg>
      </div>
      <h1>{$t('welcome.title')}</h1>
      <p class="subtitle">{$t('welcome.subtitle')}</p>
      <div class="actions">
        <button class="primary-btn" onclick={() => (step = 2)}>{$t('welcome.start')}</button>
        <button class="ghost-btn" onclick={skipAll}>{$t('welcome.skip')}</button>
      </div>
    </div>

  <!-- ── Step 2: Account Setup ─────────────────────────── -->
  {:else if step === 2}
    <div class="step-content">
      <h2>{$t('welcome.accountSetup')}</h2>
      <p class="step-desc">{$t('welcome.accountDesc')}</p>

      {#if formStep === 'input'}
        <div class="form-group">
          <label for="sessionKey">{$t('settings.auth.sessionKey')}</label>
          <div class="session-key-row">
            <input
              id="sessionKey"
              type={showSessionKey ? 'text' : 'password'}
              bind:value={formSessionKey}
              placeholder={$t('settings.auth.sessionKeyPlaceholder')}
              autocomplete="off"
            />
            <button class="toggle-btn" onclick={() => (showSessionKey = !showSessionKey)}>
              {showSessionKey ? $t('settings.auth.hide') : $t('settings.auth.show')}
            </button>
          </div>
        </div>

        {#if formError}
          <div class="error-text">{formError}</div>
        {/if}

        <div class="form-actions">
          <button class="primary-btn" onclick={handleFetchOrgs} disabled={formLoading}>
            {formLoading ? $t('settings.auth.verifying') : $t('settings.auth.verify')}
          </button>
          <button class="secondary-btn" onclick={handleBrowserLogin} disabled={browserLoginActive}>
            {browserLoginActive ? $t('settings.auth.loggingIn') : $t('settings.auth.browserLogin')}
          </button>
        </div>

        {#if browserLoginActive}
          <div class="status-row">
            <span class="pulse-dot">●</span>
            <span>{browserLoginStatus}</span>
          </div>
        {/if}

        {#if showBrowserOrgSelect}
          <div class="org-select-box">
            <div class="form-group">
              <label for="browserOrg">{$t('settings.auth.orgSelect')}</label>
              <select id="browserOrg" bind:value={browserLoginSelectedOrgId} onchange={handleBrowserOrgChange}>
                {#each browserLoginOrgs as org (org.uuid)}
                  <option value={org.uuid}>{org.name}</option>
                {/each}
              </select>
            </div>
            <div class="form-group">
              <label for="browserDisplayName">{$t('settings.auth.displayName')}</label>
              <input id="browserDisplayName" type="text" bind:value={browserLoginDisplayName} />
            </div>
            <div class="form-actions">
              <button class="primary-btn" onclick={handleBrowserOrgConfirm}>{$t('settings.auth.confirm')}</button>
              <button class="secondary-btn" onclick={handleBrowserOrgCancel}>{$t('settings.auth.cancel')}</button>
            </div>
          </div>
        {/if}

      {:else}
        <!-- Org selection step (manual) -->
        <div class="form-group">
          <label for="orgSelect">{$t('settings.auth.orgSelect')}</label>
          {#if formOrgs.length === 1}
            <span class="org-name">{formOrgs[0].name}</span>
          {:else}
            <select id="orgSelect" bind:value={formSelectedOrgId} onchange={handleOrgChange}>
              {#each formOrgs as org (org.uuid)}
                <option value={org.uuid}>{org.name}</option>
              {/each}
            </select>
          {/if}
        </div>
        <div class="form-group">
          <label for="displayName">{$t('settings.auth.displayName')}</label>
          <input id="displayName" type="text" bind:value={formDisplayName} />
        </div>

        {#if formError}
          <div class="error-text">{formError}</div>
        {/if}

        <div class="form-actions">
          <button class="primary-btn" onclick={handleAddAccount} disabled={formLoading}>
            {formLoading ? $t('settings.auth.adding') : $t('settings.auth.add')}
          </button>
          <button class="secondary-btn" onclick={handleFormReset}>{$t('settings.auth.cancel')}</button>
        </div>
      {/if}

      <div class="step-nav">
        <button class="ghost-btn small" onclick={() => (step = 1)}>{$t('welcome.back')}</button>
        <button class="ghost-btn small" onclick={() => (step = 3)}>{$t('welcome.skip')}</button>
      </div>
    </div>

  <!-- ── Step 3: Theme Preview ──────────────────────────── -->
  {:else if step === 3}
    <div class="step-content">
      <h2>{$t('welcome.themeSetup')}</h2>
      <p class="step-desc">{$t('welcome.themeDesc')}</p>

      {#if settings}
        <div class="theme-settings">
          <div class="setting-row">
            <label for="theme">{$t('settings.general.theme')}</label>
            <select id="theme" bind:value={settings.theme} onchange={saveSettings}>
              <option value="system">{$t('settings.general.themeSystem')}</option>
              <option value="light">{$t('settings.general.themeLight')}</option>
              <option value="dark">{$t('settings.general.themeDark')}</option>
            </select>
          </div>
          <div class="setting-row">
            <label for="displayMode">{$t('settings.general.displayMode')}</label>
            <select id="displayMode" bind:value={settings.displayMode} onchange={saveSettings}>
              <option value="percentOnly">{$t('settings.general.percentOnly')}</option>
              <option value="iconOnly">{$t('settings.general.iconOnly')}</option>
              <option value="iconAndPercent">{$t('settings.general.iconAndPercent')}</option>
            </select>
          </div>
          <div class="setting-row">
            <label for="iconTheme">{$t('settings.general.iconTheme')}</label>
            <select id="iconTheme" bind:value={settings.iconTheme} onchange={saveSettings}>
              <option value="colorTranslucent">{$t('settings.general.colorTranslucent')}</option>
              <option value="colorWithBackground">{$t('settings.general.colorWithBackground')}</option>
              <option value="monochrome">{$t('settings.general.monochrome')}</option>
            </select>
          </div>
        </div>

        <!-- Preview box -->
        <div class="preview-box">
          <div class="preview-label">Preview</div>
          <div class="preview-content">
            <span class="preview-icon">
              {#if settings.iconTheme === 'monochrome'}
                ■
              {:else if settings.iconTheme === 'colorWithBackground'}
                🟦
              {:else}
                🔵
              {/if}
            </span>
            {#if settings.displayMode === 'percentOnly'}
              <span class="preview-text">72%</span>
            {:else if settings.displayMode === 'iconOnly'}
              <!-- icon only, no text -->
            {:else}
              <span class="preview-text">72%</span>
            {/if}
            <span class="preview-theme-badge">
              {settings.theme === 'system' ? $t('settings.general.themeSystem')
               : settings.theme === 'light' ? $t('settings.general.themeLight')
               : $t('settings.general.themeDark')}
            </span>
          </div>
        </div>
      {:else}
        <div class="loading-text">Loading...</div>
      {/if}

      <div class="step-nav">
        <button class="ghost-btn small" onclick={() => (step = 2)}>{$t('welcome.back')}</button>
        <button class="primary-btn" onclick={() => (step = 4)}>{$t('welcome.next')}</button>
      </div>
    </div>

  <!-- ── Step 4: Complete ───────────────────────────────── -->
  {:else if step === 4}
    <div class="step-content center">
      <div class="success-icon">✓</div>
      <h1>{$t('welcome.complete')}</h1>
      <p class="subtitle">{$t('welcome.completeDesc')}</p>
      <div class="actions">
        <button class="primary-btn" onclick={finish}>{$t('welcome.finish')}</button>
      </div>
    </div>
  {/if}
</div>

<style>
  :global(body) {
    margin: 0;
    padding: 0;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    background: #1e1e1e;
    color: #e0e0e0;
  }

  .wizard {
    display: flex;
    flex-direction: column;
    min-height: 100vh;
    padding: 24px;
    box-sizing: border-box;
    background: #1e1e1e;
    color: #e0e0e0;
  }

  /* Step dots */
  .step-dots {
    display: flex;
    justify-content: center;
    gap: 8px;
    margin-bottom: 28px;
  }

  .dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: #3a3a3a;
    transition: background 0.2s;
  }

  .dot.active {
    background: #0078d4;
  }

  .dot.done {
    background: #4caf50;
  }

  /* Step content */
  .step-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .step-content.center {
    align-items: center;
    justify-content: center;
    text-align: center;
    gap: 20px;
  }

  /* App icon (step 1) */
  .app-icon {
    margin-bottom: 8px;
  }

  h1 {
    margin: 0;
    font-size: 22px;
    font-weight: 700;
    color: #e0e0e0;
  }

  h2 {
    margin: 0 0 4px 0;
    font-size: 18px;
    font-weight: 600;
    color: #e0e0e0;
  }

  .subtitle {
    margin: 0;
    font-size: 13px;
    color: #888;
    line-height: 1.5;
    max-width: 360px;
  }

  .step-desc {
    margin: 0;
    font-size: 13px;
    color: #888;
    line-height: 1.5;
  }

  /* Success icon (step 4) */
  .success-icon {
    width: 64px;
    height: 64px;
    border-radius: 50%;
    background: #4caf50;
    color: white;
    font-size: 32px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
  }

  /* Form elements */
  .form-group {
    display: flex;
    flex-direction: column;
    gap: 6px;
  }

  .form-group label {
    font-size: 12px;
    color: #888;
    font-weight: 500;
    text-transform: uppercase;
    letter-spacing: 0.4px;
  }

  .session-key-row {
    display: flex;
    gap: 6px;
  }

  .session-key-row input {
    flex: 1;
  }

  input[type='text'],
  input[type='password'] {
    background: #2a2a2a;
    border: 1px solid #444;
    border-radius: 6px;
    color: #e0e0e0;
    font-size: 13px;
    padding: 8px 10px;
    outline: none;
    transition: border-color 0.15s;
    width: 100%;
    box-sizing: border-box;
  }

  input[type='text']:focus,
  input[type='password']:focus {
    border-color: #0078d4;
    box-shadow: 0 0 0 2px rgba(0, 120, 212, 0.25);
  }

  select {
    background: #2a2a2a;
    border: 1px solid #444;
    border-radius: 6px;
    color: #e0e0e0;
    font-size: 13px;
    padding: 8px 10px;
    outline: none;
    cursor: pointer;
    transition: border-color 0.15s;
    width: 100%;
    box-sizing: border-box;
  }

  select:hover,
  select:focus {
    border-color: #0078d4;
  }

  /* Actions */
  .actions {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
    width: 100%;
    max-width: 260px;
  }

  .form-actions {
    display: flex;
    gap: 8px;
  }

  .step-nav {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: auto;
    padding-top: 16px;
    border-top: 1px solid #2a2a2a;
  }

  /* Buttons */
  .primary-btn {
    background: #0078d4;
    border: none;
    border-radius: 6px;
    color: #fff;
    font-size: 14px;
    font-weight: 500;
    padding: 10px 24px;
    cursor: pointer;
    transition: background 0.15s;
    width: 100%;
  }

  .primary-btn:hover:not(:disabled) {
    background: #0066b8;
  }

  .primary-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  /* Non-full-width primary in form-actions */
  .form-actions .primary-btn {
    width: auto;
  }

  .secondary-btn {
    background: #2a2a2a;
    border: 1px solid #444;
    border-radius: 6px;
    color: #e0e0e0;
    font-size: 13px;
    padding: 8px 16px;
    cursor: pointer;
    transition: border-color 0.15s;
  }

  .secondary-btn:hover:not(:disabled) {
    border-color: #0078d4;
  }

  .secondary-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .ghost-btn {
    background: none;
    border: none;
    color: #888;
    font-size: 13px;
    cursor: pointer;
    padding: 6px 12px;
    border-radius: 4px;
    transition: color 0.15s;
  }

  .ghost-btn:hover {
    color: #e0e0e0;
  }

  .ghost-btn.small {
    font-size: 12px;
    padding: 4px 8px;
  }

  .toggle-btn {
    background: #2a2a2a;
    border: 1px solid #444;
    border-radius: 6px;
    color: #888;
    font-size: 12px;
    padding: 8px 12px;
    cursor: pointer;
    white-space: nowrap;
    transition: border-color 0.15s;
    flex-shrink: 0;
  }

  .toggle-btn:hover {
    border-color: #0078d4;
    color: #e0e0e0;
  }

  /* Status / error */
  .error-text {
    font-size: 12px;
    color: #f44747;
  }

  .status-row {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 12px;
    color: #888;
  }

  .pulse-dot {
    color: #0078d4;
    animation: pulse 1.5s infinite;
  }

  @keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.3; }
  }

  /* Org select box */
  .org-select-box {
    display: flex;
    flex-direction: column;
    gap: 12px;
    padding: 12px;
    background: #252525;
    border-radius: 8px;
    border: 1px solid #3a3a3a;
  }

  .org-name {
    font-size: 13px;
    color: #e0e0e0;
  }

  /* Theme settings (step 3) */
  .theme-settings {
    display: flex;
    flex-direction: column;
    gap: 12px;
    background: #252525;
    border-radius: 8px;
    padding: 14px;
  }

  .setting-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
  }

  .setting-row label {
    font-size: 13px;
    color: #e0e0e0;
    flex-shrink: 0;
    min-width: 100px;
  }

  .setting-row select {
    flex: 1;
    width: auto;
  }

  /* Preview box */
  .preview-box {
    background: #252525;
    border: 1px solid #3a3a3a;
    border-radius: 8px;
    padding: 12px 16px;
  }

  .preview-label {
    font-size: 11px;
    color: #666;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    margin-bottom: 8px;
  }

  .preview-content {
    display: flex;
    align-items: center;
    gap: 10px;
  }

  .preview-icon {
    font-size: 18px;
  }

  .preview-text {
    font-size: 14px;
    font-weight: 600;
    color: #e0e0e0;
  }

  .preview-theme-badge {
    margin-left: auto;
    font-size: 11px;
    color: #888;
    background: #333;
    padding: 2px 8px;
    border-radius: 10px;
  }

  .loading-text {
    color: #888;
    font-size: 13px;
    padding: 20px;
    text-align: center;
  }
</style>
