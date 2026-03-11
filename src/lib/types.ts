// REVIEW: PASS
// TypeScript types — 1:1 mapping with Rust models (camelCase)

export type LimitType = 'fiveHour' | 'sevenDay' | 'opus' | 'sonnet' | 'extra';

export interface UsageLimitInfo {
  limitType: LimitType;
  percentage: number;
  resetsAt: string;
}

export interface UsageData {
  limits: UsageLimitInfo[];
  extra: UsageLimitInfo | null;
  fetchedAt: string;
}

export type DisplayMode = 'percentOnly' | 'iconOnly' | 'iconAndPercent';
export type IconTheme = 'colorTranslucent' | 'colorWithBackground' | 'monochrome';
export type DisplayContent = 'smart' | 'custom';
export type RefreshMode = 'smart' | 'fixed';
export type AppTheme = 'system' | 'light' | 'dark';
export type TimeFormat = 'system' | 'twelveHour' | 'twentyFourHour';

export interface UserSettings {
  displayMode: DisplayMode;
  iconTheme: IconTheme;
  displayContent: DisplayContent;
  refreshMode: RefreshMode;
  refreshIntervalMinutes: number;
  theme: AppTheme;
  timeFormat: TimeFormat;
  language: string;
  launchAtLogin: boolean;
  notificationsEnabled: boolean;
  resetNotifications: boolean;
}

export interface Account {
  id: string;
  displayName: string;
  orgId: string;
  orgName: string;
  isActive: boolean;
}

export interface Organization {
  uuid: string;
  name: string;
}
