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
  // REVIEW: [PASS] listen()의 반환값(Promise<UnlistenFn>)을 무시하고 있음.
  // 현재 구조(앱 전체 수명과 같은 레이아웃)에서는 실용적으로 문제없으나,
  // 향후 cleanup이 필요할 경우 unlisten 함수를 저장해 onDestroy에서 호출하는 것을 권장.
  // Initial load
  accountsStore.load();
}
