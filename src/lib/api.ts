import { invoke } from '@tauri-apps/api/core';
import type { UsageData, UserSettings, Account, Organization, DiagnosisResult } from './types';

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

export async function addAccount(
  sessionKey: string,
  orgId: string,
  displayName: string,
  orgName: string
): Promise<Account> {
  return invoke<Account>('add_account', { sessionKey, orgId, displayName, orgName });
}

export async function removeAccount(accountId: string): Promise<void> {
  return invoke('remove_account', { accountId });
}

export async function switchAccount(accountId: string): Promise<void> {
  return invoke('switch_account', { accountId });
}

export async function diagnoseConnection(accountId: string): Promise<DiagnosisResult> {
  return invoke<DiagnosisResult>('diagnose_connection', { accountId });
}

export async function getAppVersion(): Promise<string> {
  return invoke<string>('get_app_version');
}

export async function openLoginWindow(): Promise<void> {
  return invoke('open_login_window');
}

export async function closeLoginWindow(): Promise<void> {
  return invoke('close_login_window');
}
