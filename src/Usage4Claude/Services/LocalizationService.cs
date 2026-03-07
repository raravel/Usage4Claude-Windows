using System.Globalization;
using Usage4Claude.Models;

namespace Usage4Claude.Services;

public class LocalizationService
{
    private readonly SettingsService _settingsService;

    public event EventHandler? LanguageChanged;

    public LocalizationService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        ApplyLanguage(_settingsService.Settings.Language);
    }

    public void ChangeLanguage(AppLanguage language)
    {
        _settingsService.Settings.Language = language;
        _settingsService.Save();
        ApplyLanguage(language);
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    public static string GetString(string key)
    {
        return Resources.Strings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
               ?? key;
    }

    public static string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    private static void ApplyLanguage(AppLanguage language)
    {
        var cultureName = language switch
        {
            AppLanguage.English => "en",
            AppLanguage.Japanese => "ja",
            AppLanguage.Chinese => "zh-Hans",
            AppLanguage.ChineseTraditional => "zh-Hant",
            AppLanguage.Korean => "ko",
            _ => "en"
        };

        var culture = new CultureInfo(cultureName);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
}
