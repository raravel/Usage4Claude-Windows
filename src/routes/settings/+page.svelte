<!-- REVIEW: PASS -->
<script lang="ts">
  import { onMount } from 'svelte';
  import { getSettings, updateSettings } from '$lib/api';
  import type { UserSettings } from '$lib/types';

  let activeTab = $state<'general' | 'auth' | 'about'>('general');
  let settings = $state<UserSettings | null>(null);

  onMount(async () => {
    settings = await getSettings();
  });

  async function saveSettings() {
    if (settings) {
      await updateSettings(settings);
    }
  }
</script>

<div class="settings-window">
  <div class="tab-bar">
    <button class:active={activeTab === 'general'} onclick={() => (activeTab = 'general')}>
      일반
    </button>
    <button class:active={activeTab === 'auth'} onclick={() => (activeTab = 'auth')}>
      인증
    </button>
    <button class:active={activeTab === 'about'} onclick={() => (activeTab = 'about')}>
      정보
    </button>
  </div>

  <div class="tab-content">
    {#if activeTab === 'general'}
      <div class="tab-panel">일반 설정 (다음 태스크에서 구현)</div>
    {:else if activeTab === 'auth'}
      <div class="tab-panel">인증 설정 (다음 태스크에서 구현)</div>
    {:else}
      <div class="tab-panel">정보 (다음 태스크에서 구현)</div>
    {/if}
  </div>
</div>

<style>
  :global(body) {
    margin: 0;
    padding: 0;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
  }

  .settings-window {
    display: flex;
    flex-direction: column;
    height: 100vh;
    background: #f5f5f5;
    color: #1a1a1a;
  }

  .tab-bar {
    display: flex;
    background: #e0e0e0;
    border-bottom: 1px solid #c0c0c0;
    padding: 0 8px;
    gap: 2px;
  }

  .tab-bar button {
    background: none;
    border: none;
    border-bottom: 2px solid transparent;
    cursor: pointer;
    font-size: 14px;
    padding: 10px 16px;
    color: #555;
    transition: color 0.15s, border-color 0.15s;
  }

  .tab-bar button:hover {
    color: #1a1a1a;
  }

  .tab-bar button.active {
    color: #1a1a1a;
    border-bottom-color: #0078d4;
    font-weight: 600;
  }

  .tab-content {
    flex: 1;
    overflow-y: auto;
  }

  .tab-panel {
    padding: 24px;
    color: #666;
    font-size: 14px;
  }
</style>
