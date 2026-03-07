using System.IO;
using System.Text.Json;
using Usage4Claude.Models;

namespace Usage4Claude.Services;

public class SettingsService
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Usage4Claude");

    private static readonly string SettingsFilePath = Path.Combine(AppDataPath, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private UserSettings _settings = new();

    public UserSettings Settings => _settings;

    public SettingsService()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                _settings = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions) ?? new UserSettings();
            }
            else
            {
                _settings = new UserSettings();
                // 시스템 언어 감지
                _settings.Language = DetectSystemLanguage();
                Save(); // 기본값으로 첫 저장
            }
        }
        catch
        {
            _settings = new UserSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppDataPath);
            var json = JsonSerializer.Serialize(_settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // 저장 실패 시 무시 (추후 로깅 추가)
        }
    }

    private static AppLanguage DetectSystemLanguage()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.Name;
        return culture switch
        {
            var c when c.StartsWith("ko") => AppLanguage.Korean,
            var c when c.StartsWith("ja") => AppLanguage.Japanese,
            var c when c.StartsWith("zh-Hans") || c == "zh-CN" => AppLanguage.Chinese,
            var c when c.StartsWith("zh-Hant") || c == "zh-TW" || c == "zh-HK" => AppLanguage.ChineseTraditional,
            _ => AppLanguage.English
        };
    }
}
