using System.Windows.Media.Imaging;

namespace Usage4Claude.Helpers;

/// <summary>
/// Caches rendered tray icons to avoid redundant SkiaSharp rendering.
/// Key format: "{percentage}_{showText}_{showRing}_{monochrome}" -> BitmapSource
/// </summary>
public class IconCache
{
    private readonly Dictionary<string, BitmapSource> _cache = new();
    private const int MaxCacheSize = 50;

    /// <summary>
    /// Get a cached icon or create one using the factory.
    /// </summary>
    /// <param name="percentage">Usage percentage (0-100, truncated to int for cache key).</param>
    /// <param name="showText">Whether percentage text is shown.</param>
    /// <param name="showRing">Whether the progress ring is shown.</param>
    /// <param name="monochrome">Whether monochrome style is used.</param>
    /// <param name="factory">Factory that creates the BitmapSource given (percentage, showText, showRing, monochrome).</param>
    public BitmapSource GetOrCreate(double percentage, bool showText, bool showRing, bool monochrome,
        Func<double, bool, bool, bool, BitmapSource> factory)
    {
        var key = $"{(int)percentage}_{showText}_{showRing}_{monochrome}";

        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var icon = factory(percentage, showText, showRing, monochrome);

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
