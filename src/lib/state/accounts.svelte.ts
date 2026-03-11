import { getAccounts } from '$lib/api';
import { listen } from '@tauri-apps/api/event';
import type { Account } from '$lib/types';

class AccountsStore {
  accounts = $state<Account[]>([]);
  loading = $state(false);

  get activeAccount(): Account | undefined {
    return this.accounts.find((a) => a.isActive);
  }

  async load() {
    this.loading = true;
    try {
      this.accounts = await getAccounts();
    } catch (e) {
      console.error('Failed to load accounts:', e);
    } finally {
      this.loading = false;
    }
  }
}

export const accountsStore = new AccountsStore();

export function initAccountsListener() {
  // Listen for account changes from Rust
  listen('account-changed', () => {
    accountsStore.load();
  });
  // Initial load
  accountsStore.load();
}
