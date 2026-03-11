<script lang="ts">
  import { onMount, onDestroy } from 'svelte';
  import { t } from 'svelte-i18n';
  import type { Account, Organization, DiagnosisResult } from '$lib/types';
  import {
    getAccounts,
    fetchOrganizations,
    addAccount,
    removeAccount,
    switchAccount,
    diagnoseConnection,
    openLoginWindow,
    closeLoginWindow,
    getLoginResult
  } from '$lib/api';

  // 계정 목록 상태
  let accounts = $state<Account[]>([]);
  let loading = $state(false);
  let error = $state<string | null>(null);

  // 진단 결과 상태: accountId -> DiagnosisResult
  let diagResults = $state<Record<string, DiagnosisResult | null>>({});
  let diagLoading = $state<Record<string, boolean>>({});

  // 수동 입력 폼 상태
  let formSessionKey = $state('');
  let showSessionKey = $state(false);
  let formOrgs = $state<Organization[]>([]);
  let formSelectedOrgId = $state('');
  let formDisplayName = $state('');
  let formLoading = $state(false);
  let formError = $state<string | null>(null);
  let formStep = $state<'input' | 'select-org'>('input');

  // 브라우저 로그인 상태
  let browserLoginActive = $state(false);
  let browserLoginStatus = $state('');
  let loginPollingTimer: ReturnType<typeof setInterval> | null = null;

  // 브라우저 로그인 — 다중 조직 선택 상태
  let browserLoginOrgs = $state<Organization[]>([]);
  let browserLoginSessionKey = $state('');
  let browserLoginSelectedOrgId = $state('');
  let browserLoginDisplayName = $state('');
  let showBrowserOrgSelect = $state(false);

  async function loadAccounts() {
    loading = true;
    error = null;
    try {
      accounts = await getAccounts();
    } catch (e) {
      error = String(e);
    } finally {
      loading = false;
    }
  }

  onMount(loadAccounts);

  onDestroy(() => {
    stopLoginPolling();
  });

  async function handleSwitch(accountId: string) {
    try {
      await switchAccount(accountId);
      await loadAccounts();
    } catch (e) {
      error = String(e);
    }
  }

  async function handleRemove(account: Account) {
    const confirmed = window.confirm(
      $t('settings.auth.deleteConfirm', { values: { name: account.displayName } })
    );
    if (!confirmed) return;
    try {
      await removeAccount(account.id);
      // 진단 결과도 초기화
      const newDiag = { ...diagResults };
      delete newDiag[account.id];
      diagResults = newDiag;
      await loadAccounts();
    } catch (e) {
      error = String(e);
    }
  }

  async function handleDiagnose(accountId: string) {
    diagLoading = { ...diagLoading, [accountId]: true };
    try {
      const result = await diagnoseConnection(accountId);
      diagResults = { ...diagResults, [accountId]: result };
    } catch (e) {
      diagResults = {
        ...diagResults,
        [accountId]: {
          sessionValid: false,
          apiReachable: false,
          organizations: [],
          errorMessage: String(e)
        }
      };
    } finally {
      diagLoading = { ...diagLoading, [accountId]: false };
    }
  }

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
    if (org) {
      formDisplayName = org.name;
    }
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
      // 폼 초기화
      formSessionKey = '';
      formOrgs = [];
      formSelectedOrgId = '';
      formDisplayName = '';
      formStep = 'input';
      await loadAccounts();
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

  // ── 브라우저 로그인 ────────────────────────────────────────

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
        // 폴링 오류는 무시 — 다음 주기에 재시도
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
        // 조직이 1개면 자동 등록
        browserLoginStatus = $t('settings.auth.registering');
        await addAccount(sessionKey, orgs[0].uuid, orgs[0].name, orgs[0].name);
        await closeLoginWindow();
        browserLoginActive = false;
        browserLoginStatus = '';
        await loadAccounts();
      } else {
        // 조직이 여러 개면 선택 UI 표시
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
      await loadAccounts();
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
</script>

<div class="auth-tab">
  <!-- 계정 목록 -->
  <section class="settings-group">
    <h3>{$t('settings.auth.accounts')}</h3>

    {#if loading}
      <div class="status-text">{$t('popup.loading')}</div>
    {:else if error}
      <div class="error-text">{error}</div>
    {:else if accounts.length === 0}
      <div class="status-text">{$t('settings.auth.noAccounts')}</div>
    {:else}
      <ul class="account-list">
        {#each accounts as account (account.id)}
          <li class="account-item" class:active={account.isActive}>
            <button
              class="account-select-btn"
              onclick={() => handleSwitch(account.id)}
              title={account.isActive ? $t('settings.auth.activeAccount') : $t('settings.auth.switchTo')}
            >
              <span class="account-check">{account.isActive ? '✓' : ''}</span>
              <span class="account-info">
                <span class="account-name">{account.displayName}</span>
                <span class="account-org">{account.orgName}</span>
              </span>
            </button>

            <div class="account-actions">
              <button
                class="diag-btn"
                onclick={() => handleDiagnose(account.id)}
                disabled={diagLoading[account.id]}
                title={$t('settings.auth.diagnose')}
              >
                {diagLoading[account.id] ? $t('settings.auth.diagnosing') : $t('settings.auth.diagnose')}
              </button>
              <button
                class="delete-btn"
                onclick={() => handleRemove(account)}
                title={$t('settings.auth.delete')}
              >
                {$t('settings.auth.delete')}
              </button>
            </div>
          </li>

          {#if diagResults[account.id]}
            {@const diag = diagResults[account.id]!}
            <li class="diag-result">
              <div class="diag-row">
                <span class="diag-label">{$t('settings.auth.diagSession')}</span>
                <span class:ok={diag.sessionValid} class:fail={!diag.sessionValid}>
                  {diag.sessionValid ? $t('settings.auth.valid') : $t('settings.auth.invalid')}
                </span>
              </div>
              <div class="diag-row">
                <span class="diag-label">{$t('settings.auth.diagApi')}</span>
                <span class:ok={diag.apiReachable} class:fail={!diag.apiReachable}>
                  {diag.apiReachable ? $t('settings.auth.connected') : $t('settings.auth.failed')}
                </span>
              </div>
              {#if diag.organizations.length > 0}
                <div class="diag-row">
                  <span class="diag-label">{$t('settings.auth.diagOrgs')}</span>
                  <span class="diag-orgs">{diag.organizations.map((o) => o.name).join(', ')}</span>
                </div>
              {/if}
              {#if diag.errorMessage}
                <div class="diag-error">{diag.errorMessage}</div>
              {/if}
            </li>
          {/if}
        {/each}
      </ul>
    {/if}
  </section>

  <!-- 계정 추가 -->
  <section class="settings-group">
    <h3>{$t('settings.auth.addAccount')}</h3>

    {#if formStep === 'input'}
      <div class="form-row">
        <label for="sessionKey">{$t('settings.auth.sessionKey')}</label>
        <div class="session-key-input">
          <input
            id="sessionKey"
            type={showSessionKey ? 'text' : 'password'}
            bind:value={formSessionKey}
            placeholder={$t('settings.auth.sessionKeyPlaceholder')}
            autocomplete="off"
          />
          <button
            class="toggle-visibility"
            onclick={() => (showSessionKey = !showSessionKey)}
            title={showSessionKey ? $t('settings.auth.hide') : $t('settings.auth.show')}
          >
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
        <button
          class="secondary-btn"
          onclick={handleBrowserLogin}
          disabled={browserLoginActive}
        >
          {browserLoginActive ? $t('settings.auth.loggingIn') : $t('settings.auth.browserLogin')}
        </button>
      </div>

      {#if browserLoginActive}
        <div class="browser-login-status">
          <span class="status-indicator">●</span>
          <span>{browserLoginStatus}</span>
        </div>
      {/if}

      {#if showBrowserOrgSelect}
        <div class="browser-org-select">
          <div class="form-row">
            <label for="browserOrgSelect">{$t('settings.auth.orgSelect')}</label>
            <select
              id="browserOrgSelect"
              bind:value={browserLoginSelectedOrgId}
              onchange={handleBrowserOrgChange}
            >
              {#each browserLoginOrgs as org (org.uuid)}
                <option value={org.uuid}>{org.name}</option>
              {/each}
            </select>
          </div>
          <div class="form-row">
            <label for="browserDisplayName">{$t('settings.auth.displayName')}</label>
            <input
              id="browserDisplayName"
              type="text"
              bind:value={browserLoginDisplayName}
            />
          </div>
          <div class="form-actions">
            <button class="primary-btn" onclick={handleBrowserOrgConfirm}>{$t('settings.auth.confirm')}</button>
            <button class="secondary-btn" onclick={handleBrowserOrgCancel}>{$t('settings.auth.cancel')}</button>
          </div>
        </div>
      {/if}
    {:else}
      <!-- 조직 선택 단계 (수동 입력) -->
      <div class="form-row">
        <label for="orgSelect">{$t('settings.auth.orgSelect')}</label>
        {#if formOrgs.length === 1}
          <span class="org-name-only">{formOrgs[0].name}</span>
        {:else}
          <select id="orgSelect" bind:value={formSelectedOrgId} onchange={handleOrgChange}>
            {#each formOrgs as org (org.uuid)}
              <option value={org.uuid}>{org.name}</option>
            {/each}
          </select>
        {/if}
      </div>

      <div class="form-row">
        <label for="displayName">{$t('settings.auth.displayName')}</label>
        <input id="displayName" type="text" bind:value={formDisplayName} placeholder={$t('settings.auth.displayName')} />
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
  </section>
</div>

<style>
  .auth-tab {
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

  .status-text {
    font-size: 13px;
    color: #888;
    padding: 4px 0;
  }

  .error-text {
    font-size: 12px;
    color: #f44747;
    padding: 4px 0;
  }

  /* 계정 목록 */
  .account-list {
    list-style: none;
    margin: 0;
    padding: 0;
    display: flex;
    flex-direction: column;
    gap: 4px;
  }

  .account-item {
    display: flex;
    align-items: center;
    gap: 8px;
    border-radius: 6px;
    padding: 6px 8px;
    background: #333;
    transition: background 0.15s;
  }

  .account-item:hover {
    background: #3a3a3a;
  }

  .account-item.active {
    border-left: 3px solid #0078d4;
    padding-left: 5px;
  }

  .account-select-btn {
    flex: 1;
    background: none;
    border: none;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 8px;
    text-align: left;
    color: #e0e0e0;
    padding: 0;
  }

  .account-check {
    width: 16px;
    font-size: 14px;
    color: #0078d4;
    flex-shrink: 0;
  }

  .account-info {
    display: flex;
    flex-direction: column;
    gap: 1px;
  }

  .account-name {
    font-size: 13px;
    font-weight: 500;
    color: #e0e0e0;
  }

  .account-org {
    font-size: 11px;
    color: #888;
  }

  .account-actions {
    display: flex;
    gap: 4px;
    flex-shrink: 0;
  }

  .diag-btn {
    background: #3a3a3a;
    border: 1px solid #555;
    border-radius: 4px;
    color: #e0e0e0;
    font-size: 11px;
    padding: 3px 8px;
    cursor: pointer;
    transition: border-color 0.15s;
  }

  .diag-btn:hover:not(:disabled) {
    border-color: #0078d4;
  }

  .diag-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .delete-btn {
    background: transparent;
    border: 1px solid #555;
    border-radius: 4px;
    color: #f44747;
    font-size: 11px;
    padding: 3px 8px;
    cursor: pointer;
    transition: border-color 0.15s, background 0.15s;
  }

  .delete-btn:hover {
    border-color: #f44747;
    background: rgba(244, 71, 71, 0.1);
  }

  /* 진단 결과 */
  .diag-result {
    list-style: none;
    background: #252525;
    border-radius: 6px;
    padding: 8px 12px;
    display: flex;
    flex-direction: column;
    gap: 4px;
    margin-top: -2px;
  }

  .diag-row {
    display: flex;
    gap: 8px;
    font-size: 12px;
  }

  .diag-label {
    color: #888;
    min-width: 60px;
  }

  .ok {
    color: #4caf50;
  }

  .fail {
    color: #f44747;
  }

  .diag-orgs {
    color: #e0e0e0;
  }

  .diag-error {
    font-size: 12px;
    color: #f44747;
    margin-top: 2px;
  }

  /* 추가 폼 */
  .form-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 8px;
    min-height: 32px;
  }

  .form-row label {
    font-size: 13px;
    color: #e0e0e0;
    flex-shrink: 0;
    min-width: 60px;
  }

  .session-key-input {
    display: flex;
    flex: 1;
    gap: 4px;
  }

  .session-key-input input {
    flex: 1;
    min-width: 0;
  }

  .form-row input[type='text'],
  .form-row input[type='password'],
  .session-key-input input {
    background: #3a3a3a;
    border: 1px solid #555;
    border-radius: 4px;
    color: #e0e0e0;
    font-size: 13px;
    padding: 4px 8px;
    outline: none;
    transition: border-color 0.15s;
  }

  .form-row input[type='text']:focus,
  .form-row input[type='password']:focus,
  .session-key-input input:focus {
    border-color: #0078d4;
    box-shadow: 0 0 0 2px rgba(0, 120, 212, 0.3);
  }

  .form-row select {
    flex: 1;
    background: #3a3a3a;
    border: 1px solid #555;
    border-radius: 4px;
    color: #e0e0e0;
    font-size: 13px;
    padding: 4px 8px;
    cursor: pointer;
    outline: none;
    transition: border-color 0.15s;
  }

  .form-row select:hover {
    border-color: #0078d4;
  }

  .form-row select:focus {
    border-color: #0078d4;
    box-shadow: 0 0 0 2px rgba(0, 120, 212, 0.3);
  }

  .org-name-only {
    font-size: 13px;
    color: #e0e0e0;
  }

  .toggle-visibility {
    background: #3a3a3a;
    border: 1px solid #555;
    border-radius: 4px;
    color: #888;
    font-size: 11px;
    padding: 4px 8px;
    cursor: pointer;
    white-space: nowrap;
    transition: border-color 0.15s;
    flex-shrink: 0;
  }

  .toggle-visibility:hover {
    border-color: #0078d4;
    color: #e0e0e0;
  }

  .form-actions {
    display: flex;
    gap: 8px;
    padding-top: 4px;
  }

  .primary-btn {
    background: #0078d4;
    border: none;
    border-radius: 4px;
    color: #fff;
    font-size: 13px;
    padding: 6px 16px;
    cursor: pointer;
    transition: background 0.15s;
  }

  .primary-btn:hover:not(:disabled) {
    background: #0066b8;
  }

  .primary-btn:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .secondary-btn {
    background: #3a3a3a;
    border: 1px solid #555;
    border-radius: 4px;
    color: #e0e0e0;
    font-size: 13px;
    padding: 6px 16px;
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

  /* 브라우저 로그인 상태 표시 */
  .browser-login-status {
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 12px;
    color: #888;
    padding: 4px 0;
  }

  .status-indicator {
    color: #0078d4;
    animation: pulse 1.5s infinite;
  }

  @keyframes pulse {
    0%,
    100% {
      opacity: 1;
    }
    50% {
      opacity: 0.3;
    }
  }

  /* 브라우저 로그인 다중 조직 선택 */
  .browser-org-select {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 8px 0 0;
    border-top: 1px solid #3a3a3a;
  }
</style>
