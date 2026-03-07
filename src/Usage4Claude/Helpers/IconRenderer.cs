using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace Usage4Claude.Helpers;

/// <summary>
/// Renders dynamic system tray icons using SkiaSharp.
/// Creates circular progress indicators with percentage text overlay.
/// </summary>
public static class IconRenderer
{
    private const int IconSize = 64; // Render at higher res, Windows downscales
    private const float StrokeWidth = 5f;
    private const float StartAngle = -90f; // 12 o'clock position

    // Color scheme based on usage percentage
    private static readonly SKColor LowUsageColor = new(76, 175, 80);      // Green (#4CAF50)
    private static readonly SKColor MediumUsageColor = new(255, 193, 7);    // Amber (#FFC107)
    private static readonly SKColor HighUsageColor = new(255, 152, 0);      // Orange (#FF9800)
    private static readonly SKColor CriticalUsageColor = new(244, 67, 54);  // Red (#F44336)
    private static readonly SKColor BackgroundArcColor = new(200, 200, 200, 80); // Light gray
    private static readonly SKColor IconBackgroundColor = new(107, 92, 231); // Purple (#6B5CE7) - app brand

    /// <summary>
    /// Render a circular progress icon with percentage text.
    /// </summary>
    public static BitmapSource RenderIcon(double percentage, bool showText = true)
    {
        percentage = Math.Clamp(percentage, 0, 100);

        using var surface = SKSurface.Create(new SKImageInfo(IconSize, IconSize, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var center = IconSize / 2f;
        var radius = (IconSize - StrokeWidth) / 2f - 2f; // Padding for anti-aliasing

        // Draw background circle (filled)
        using (var bgPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = IconBackgroundColor,
            IsAntialias = true
        })
        {
            canvas.DrawCircle(center, center, radius, bgPaint);
        }

        // Draw background arc track
        var arcRect = new SKRect(
            center - radius + StrokeWidth / 2,
            center - radius + StrokeWidth / 2,
            center + radius - StrokeWidth / 2,
            center + radius - StrokeWidth / 2);

        using (var trackPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
            Color = BackgroundArcColor,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        })
        {
            canvas.DrawArc(arcRect, 0, 360, false, trackPaint);
        }

        // Draw progress arc
        if (percentage > 0)
        {
            var sweepAngle = (float)(percentage / 100.0 * 360.0);
            var progressColor = GetColorForPercentage(percentage);

            using var progressPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = StrokeWidth,
                Color = progressColor,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round
            };

            canvas.DrawArc(arcRect, StartAngle, sweepAngle, false, progressPaint);
        }

        // Draw percentage text
        if (showText)
        {
            DrawCenteredText(canvas, ((int)percentage).ToString(), center, IconSize * 0.38f);
        }

        // Convert SkiaSharp surface to WPF BitmapSource
        return ConvertToBitmapSource(surface);
    }

    /// <summary>
    /// Render a simple text icon (e.g., "C" for default/no-data state).
    /// </summary>
    public static BitmapSource RenderDefaultIcon()
    {
        using var surface = SKSurface.Create(new SKImageInfo(IconSize, IconSize, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var center = IconSize / 2f;
        var radius = (IconSize - StrokeWidth) / 2f - 2f;

        // Purple background circle
        using (var bgPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = IconBackgroundColor,
            IsAntialias = true
        })
        {
            canvas.DrawCircle(center, center, radius, bgPaint);
        }

        // "C" text
        DrawCenteredText(canvas, "C", center, IconSize * 0.5f);

        return ConvertToBitmapSource(surface);
    }

    /// <summary>
    /// Get usage color based on percentage thresholds.
    /// </summary>
    public static SKColor GetColorForPercentage(double percentage) => percentage switch
    {
        < 50 => LowUsageColor,
        < 75 => MediumUsageColor,
        < 90 => HighUsageColor,
        _ => CriticalUsageColor
    };

    /// <summary>
    /// Draw text centered horizontally and vertically at the given position.
    /// Uses SKFont for SkiaSharp 3.x text rendering API.
    /// </summary>
    private static void DrawCenteredText(SKCanvas canvas, string text, float center, float fontSize)
    {
        using var typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
        using var font = new SKFont(typeface, fontSize);
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        // Measure text bounds for vertical centering
        var textBounds = new SKRect();
        font.MeasureText(text, out textBounds, textPaint);

        // Calculate position: horizontally centered, vertically centered
        var textX = center - textBounds.MidX;
        var textY = center - textBounds.MidY;

        canvas.DrawText(text, textX, textY, font, textPaint);
    }

    private static BitmapSource ConvertToBitmapSource(SKSurface surface)
    {
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        var bitmap = new BitmapImage();
        using (var stream = new MemoryStream(data.ToArray()))
        {
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
        }
        bitmap.Freeze(); // Make cross-thread accessible
        return bitmap;
    }
}
