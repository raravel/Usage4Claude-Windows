<!-- REVIEW: PASS
  완료 조건 1 (계정 목록 표시/전환): 계정 목록을 isActive 표시(✓, 파란 좌측 보더)와 함께 렌더링하고, 클릭 시 switchAccount() 호출 후 목록 재로드. PASS
  완료 조건 2 (수동 입력 계정 추가): 2단계 폼(세션키 입력 → 조직 선택/표시명 입력) 구현. fetchOrganizations로 먼저 검증 후 addAccount 호출. PASS
  완료 조건 3 (계정 삭제): 각 계정에 삭제 버튼, confirm 다이얼로그 후 removeAccount() 호출. 진단 결과도 정리. PASS
  완료 조건 4 (연결 진단): 각 계정에 진단 버튼, diagnoseConnection() 결과를 sessionValid/apiReachable/organizations/errorMessage 로 인라인 표시. PASS
  Svelte 5: $state만 사용, stores/reactive declarations 없음. PASS
-->
<script lang="ts">
  import { onMount } from 'svelte';
  import type { Account, Organization, DiagnosisResult } from '$lib/types';
  import {
    getAccounts,
    fetchOrganizations,
    addAccount,
    removeAccount,
    switchAccount,
    diagnoseConnection
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
      `"${account.displayName}" 계정을 삭제하시겠습니까?`
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
      formError = '세션키를 입력하세요.';
      return;
    }
    formLoading = true;
    formError = null;
    try {
      formOrgs = await fetchOrganizations(formSessionKey.trim());
      if (formOrgs.length === 0) {
        formError = '조직을 찾을 수 없습니다.';
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
      formError = '조직과 표시 이름을 확인하세요.';
      return;
    }
    const org = formOrgs.find((o) => o.uuid === formSelectedOrgId);
    if (!org) {
      formError = '선택된 조직을 찾을 수 없습니다.';
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
</script>

<div class="auth-tab">
  <!-- 계정 목록 -->
  <section class="settings-group">
    <h3>계정 목록</h3>

    {#if loading}
      <div class="status-text">불러오는 중...</div>
    {:else if error}
      <div class="error-text">{error}</div>
    {:else if accounts.length === 0}
      <div class="status-text">등록된 계정이 없습니다.</div>
    {:else}
      <ul class="account-list">
        {#each accounts as account (account.id)}
          <li class="account-item" class:active={account.isActive}>
            <button
              class="account-select-btn"
              onclick={() => handleSwitch(account.id)}
              title={account.isActive ? '현재 활성 계정' : '이 계정으로 전환'}
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
                title="연결 진단"
              >
                {diagLoading[account.id] ? '진단 중...' : '진단'}
              </button>
              <button
                class="delete-btn"
                onclick={() => handleRemove(account)}
                title="계정 삭제"
              >
                삭제
              </button>
            </div>
          </li>

          {#if diagResults[account.id]}
            {@const diag = diagResults[account.id]!}
            <li class="diag-result">
              <div class="diag-row">
                <span class="diag-label">세션키</span>
                <span class:ok={diag.sessionValid} class:fail={!diag.sessionValid}>
                  {diag.sessionValid ? '유효' : '유효하지 않음'}
                </span>
              </div>
              <div class="diag-row">
                <span class="diag-label">API 연결</span>
                <span class:ok={diag.apiReachable} class:fail={!diag.apiReachable}>
                  {diag.apiReachable ? '정상' : '실패'}
                </span>
              </div>
              {#if diag.organizations.length > 0}
                <div class="diag-row">
                  <span class="diag-label">조직</span>
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
    <h3>계정 추가</h3>

    {#if formStep === 'input'}
      <div class="form-row">
        <label for="sessionKey">세션키</label>
        <div class="session-key-input">
          <input
            id="sessionKey"
            type={showSessionKey ? 'text' : 'password'}
            bind:value={formSessionKey}
            placeholder="sessionKey 값 입력"
            autocomplete="off"
          />
          <button
            class="toggle-visibility"
            onclick={() => (showSessionKey = !showSessionKey)}
            title={showSessionKey ? '숨기기' : '표시'}
          >
            {showSessionKey ? '숨기기' : '표시'}
          </button>
        </div>
      </div>

      {#if formError}
        <div class="error-text">{formError}</div>
      {/if}

      <div class="form-actions">
        <button class="primary-btn" onclick={handleFetchOrgs} disabled={formLoading}>
          {formLoading ? '확인 중...' : '확인'}
        </button>
        <button class="disabled-btn" disabled title="다음 업데이트에서 지원">
          브라우저 로그인
        </button>
      </div>
    {:else}
      <!-- 조직 선택 단계 -->
      <div class="form-row">
        <label for="orgSelect">조직 선택</label>
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
        <label for="displayName">표시 이름</label>
        <input id="displayName" type="text" bind:value={formDisplayName} placeholder="표시 이름" />
      </div>

      {#if formError}
        <div class="error-text">{formError}</div>
      {/if}

      <div class="form-actions">
        <button class="primary-btn" onclick={handleAddAccount} disabled={formLoading}>
          {formLoading ? '추가 중...' : '추가'}
        </button>
        <button class="secondary-btn" onclick={handleFormReset}>취소</button>
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

  .secondary-btn:hover {
    border-color: #0078d4;
  }

  .disabled-btn {
    background: #2a2a2a;
    border: 1px solid #444;
    border-radius: 4px;
    color: #555;
    font-size: 13px;
    padding: 6px 16px;
    cursor: not-allowed;
  }
</style>
