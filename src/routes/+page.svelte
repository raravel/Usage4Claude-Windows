<script lang="ts">
  import { onMount } from 'svelte';
  import { t } from 'svelte-i18n';
  import { usageStore } from '$lib/state/usage.svelte';
  import { manualRefresh } from '$lib/api';
  import { getCurrentWindow } from '@tauri-apps/api/window';
  import UsageRow from '$lib/components/UsageRow.svelte';

  let refreshing = $state(false);

  async function handleRefresh() {
    refreshing = true;
    try {
      await manualRefresh();
    } catch (e) {
      console.error(e);
    } finally {
      refreshing = false;
    }
  }

  function handleClose() {
    getCurrentWindow().hide();
  }

  function handleKeydown(e: KeyboardEvent) {
    if (e.key === 'Escape') {
      handleClose();
    }
  }

  onMount(() => {
    document.addEventListener('keydown', handleKeydown);
    return () => document.removeEventListener('keydown', handleKeydown);
  });
</script>

<div class="popup-container">
  <header class="popup-header">
    <h1>{$t('app.name')}</h1>
    <div class="header-actions">
      <button onclick={handleRefresh} disabled={refreshing} title={$t('popup.refresh')}>&#8635;</button>
      <button onclick={handleClose} title={$t('popup.close')}>&times;</button>
    </div>
  </header>

  <main class="popup-content">
    {#if usageStore.data}
      <div class="usage-grid">
        {#each usageStore.data.limits as limit}
          <UsageRow {limit} />
        {/each}
        {#if usageStore.data.extra}
          <UsageRow limit={usageStore.data.extra} />
        {/if}
      </div>
    {:else if usageStore.error}
      <div class="error-message">{usageStore.error}</div>
    {:else}
      <div class="no-data">{$t('popup.loading')}</div>
    {/if}
  </main>
</div>

<style>
  :global(body) {
    margin: 0;
    padding: 0;
    overflow: hidden;
  }

  .popup-container {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
    background: #1a1a2e;
    color: #e0e0e0;
    border-radius: 12px;
    overflow: hidden;
    height: 100vh;
    display: flex;
    flex-direction: column;
    user-select: none;
  }

  .popup-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 12px 16px;
    background: #16213e;
  }

  .popup-header h1 {
    font-size: 14px;
    margin: 0;
    font-weight: 600;
  }

  .header-actions {
    display: flex;
    gap: 8px;
  }

  .header-actions button {
    background: none;
    border: none;
    color: #e0e0e0;
    cursor: pointer;
    font-size: 16px;
    padding: 4px 8px;
    border-radius: 4px;
  }

  .header-actions button:hover {
    background: rgba(255, 255, 255, 0.1);
  }

  .header-actions button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }

  .popup-content {
    flex: 1;
    padding: 16px;
    display: flex;
    flex-direction: column;
    justify-content: center;
  }

  .usage-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 16px;
    justify-content: center;
  }

  .error-message {
    color: #f44336;
    text-align: center;
  }

  .no-data {
    text-align: center;
    color: #666;
  }
</style>
