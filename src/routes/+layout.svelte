<script lang="ts">
  import { onMount } from 'svelte';
  import { initUsageListener } from '$lib/state/usage.svelte';
  import { initAccountsListener } from '$lib/state/accounts.svelte';
  import { settingsStore } from '$lib/state/settings.svelte';
  import { initI18n } from '$lib/i18n';

  let { children } = $props();

  // Initialize i18n synchronously (registers locales, sets initial locale from navigator)
  initI18n();

  onMount(async () => {
    initUsageListener();
    initAccountsListener();
    await settingsStore.load();
    // After settings loaded, update locale if user has a saved preference
    if (settingsStore.data?.language && settingsStore.data.language !== 'system') {
      const { locale } = await import('svelte-i18n');
      locale.set(settingsStore.data.language);
    }
  });
</script>

{@render children()}
