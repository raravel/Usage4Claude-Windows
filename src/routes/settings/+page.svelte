<script lang="ts">
  import { onMount } from 'svelte';
  import { getSettings } from '$lib/api';
  import type { UserSettings } from '$lib/types';
  import GeneralTab from '$lib/components/GeneralTab.svelte';
  import AuthTab from '$lib/components/AuthTab.svelte';

  let activeTab = $state<'general' | 'auth' | 'about'>('general');
  let settings = $state<UserSettings | null>(null);

  onMount(async () => {
    settings = await getSettings();
  });
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
    {#if activeTab === 'general' && settings}
      <GeneralTab bind:settings={settings} />
    {:else if activeTab === 'general'}
      <div class="tab-panel">설정을 불러오는 중...</div>
    {:else if activeTab === 'auth'}
      <AuthTab />
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
    background: #1e1e1e;
    color: #e0e0e0;
  }

  .tab-bar {
    display: flex;
    background: #252525;
    border-bottom: 1px solid #3a3a3a;
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
    color: #888;
    transition: color 0.15s, border-color 0.15s;
  }

  .tab-bar button:hover {
    color: #e0e0e0;
  }

  .tab-bar button.active {
    color: #e0e0e0;
    border-bottom-color: #0078d4;
    font-weight: 600;
  }

  .tab-content {
    flex: 1;
    overflow-y: auto;
  }

  .tab-panel {
    padding: 24px;
    color: #888;
    font-size: 14px;
  }
</style>
