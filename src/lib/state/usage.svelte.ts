import { listen } from '@tauri-apps/api/event';
import type { UsageData } from '../types';

class UsageStore {
  data = $state<UsageData | null>(null);
  error = $state<string | null>(null);
  loading = $state(false);
}

export const usageStore = new UsageStore();

export function initUsageListener() {
  listen<UsageData>('usage-updated', (event) => {
    usageStore.data = event.payload;
    usageStore.error = null;
    usageStore.loading = false;
  });

  listen<string>('api-error', (event) => {
    usageStore.error = event.payload;
    usageStore.loading = false;
  });
}
