using System.Windows.Media.Imaging;

namespace Usage4Claude.Helpers;

/// <summary>
/// Caches rendered tray icons to avoid redundant SkiaSharp rendering.
/// Key format: "{percentage}_{showText}" -> BitmapSource
/// </summary>
public class IconCache
{
    private readonly Dictionary<string, BitmapSource> _cache = new();
    private const int MaxCacheSize = 50;

    public BitmapSource GetOrCreate(double percentage, bool showText, Func<double, bool, BitmapSource> factory)
    {
        var key = $"{(int)percentage}_{showText}";

        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var icon = factory(percentage, showText);

        // Evict oldest entries if cache is full
        if (_cache.Count >= MaxCacheSize)
        {
            var firstKey = _cache.Keys.First();
            _cache.Remove(firstKey);
        }

        _cache[key] = icon;
        return icon;
    }

    public void Clear() => _cache.Clear();
}
