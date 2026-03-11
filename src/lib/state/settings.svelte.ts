import { getSettings } from '../api';
import type { UserSettings } from '../types';

class SettingsStore {
  data = $state<UserSettings | null>(null);

  async load() {
    this.data = await getSettings();
  }
}

export const settingsStore = new SettingsStore();
