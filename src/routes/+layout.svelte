<script lang="ts">
  import { onMount } from 'svelte';
  import { initUsageListener } from '$lib/state/usage.svelte';
  import { settingsStore } from '$lib/state/settings.svelte';

  let { children } = $props();

  onMount(() => {
    initUsageListener();
    settingsStore.load();
  });
</script>

{@render children()}
