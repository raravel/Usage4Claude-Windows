import { invoke } from '@tauri-apps/api/core';
import type { UsageData, UserSettings, Account, Organization } from './types';

export async function getSettings(): Promise<UserSettings> {
  return invoke<UserSettings>('get_settings');
}

export async function updateSettings(settings: UserSettings): Promise<void> {
  return invoke('update_settings', { settings });
}

export async function fetchUsage(orgId: string, sessionKey: string): Promise<UsageData> {
  return invoke<UsageData>('fetch_usage', { orgId, sessionKey });
}

export async function manualRefresh(): Promise<void> {
  return invoke('manual_refresh');
}

export async function getAccounts(): Promise<Account[]> {
  return invoke<Account[]>('get_accounts');
}

export async function validateSession(accountId: string): Promise<boolean> {
  return invoke<boolean>('validate_session', { accountId });
}

export async function fetchOrganizations(sessionKey: string): Promise<Organization[]> {
  return invoke<Organization[]>('fetch_organizations', { sessionKey });
}
