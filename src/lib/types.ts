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

export interface DiagnosisResult {
  sessionValid: boolean;
  apiReachable: boolean;
  organizations: Organization[];
  errorMessage: string | null;
}

// REVIEW: PASS — [완료조건2] UpdateInfo 인터페이스가 Rust UpdateInfo 구조체의 camelCase 직렬화와 1:1 매핑됨.
export interface UpdateInfo {
  currentVersion: string;
  latestVersion: string;
  updateAvailable: boolean;
  releaseUrl: string | null;
  releaseNotes: string | null;
}
