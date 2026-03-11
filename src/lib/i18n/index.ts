import { register, init, getLocaleFromNavigator } from 'svelte-i18n';

register('en', () => import('./locales/en.json'));
register('ko', () => import('./locales/ko.json'));
register('ja', () => import('./locales/ja.json'));
register('zh-Hans', () => import('./locales/zh-Hans.json'));
register('zh-Hant', () => import('./locales/zh-Hant.json'));

export function initI18n(savedLanguage?: string) {
  const locale =
    savedLanguage && savedLanguage !== 'system'
      ? savedLanguage
      : mapNavigatorLocale(getLocaleFromNavigator() || 'en');

  init({
    fallbackLocale: 'en',
    initialLocale: locale,
  });
}

function mapNavigatorLocale(nav: string): string {
  if (nav.startsWith('ko')) return 'ko';
  if (nav.startsWith('ja')) return 'ja';
  if (nav === 'zh-TW' || nav === 'zh-Hant') return 'zh-Hant';
  if (nav.startsWith('zh')) return 'zh-Hans';
  return 'en';
}
