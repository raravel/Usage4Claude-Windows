using System.IO;
using Serilog;
using Serilog.Events;

namespace Usage4Claude.Services;

/// <summary>
/// Configures and manages Serilog logging for the application.
/// Logs are written to %APPDATA%/Usage4Claude/logs/
/// </summary>
public static class LoggingService
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Usage4Claude", "logs");

    private static bool _initialized;

    /// <summary>
    /// Initialize Serilog with file sink. Call once at app startup.
    /// </summary>
    /// <param name="debugMode">If true, sets minimum level to Verbose. Otherwise Information.</param>
    public static void Initialize(bool debugMode = false)
    {
        if (_initialized) return;

        Directory.CreateDirectory(LogDirectory);

        var logPath = Path.Combine(LogDirectory, "usage4claude-.log");

        var config = new LoggerConfiguration()
            .MinimumLevel.Is(debugMode ? LogEventLevel.Verbose : LogEventLevel.Information)
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 10_000_000) // 10 MB per file
            .Enrich.WithProperty("AppVersion", GetAppVersion());

        if (debugMode)
        {
            config.WriteTo.Debug(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        Log.Logger = config.CreateLogger();
        _initialized = true;

        Log.Information("Usage4Claude started (Debug mode: {DebugMode})", debugMode);
    }

    /// <summary>
    /// Flush and close the logger. Call on app exit.
    /// </summary>
    public static void Shutdown()
    {
        Log.Information("Usage4Claude shutting down");
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Get the log directory path (for UI display).
    /// </summary>
    public static string GetLogDirectory() => LogDirectory;

    private static string GetAppVersion()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }
}
