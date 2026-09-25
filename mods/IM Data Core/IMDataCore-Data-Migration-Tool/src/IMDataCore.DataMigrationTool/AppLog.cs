using System.Diagnostics;
using System.Text;

namespace IMDataCore.DataMigrationTool;

internal static class AppLog
{
    private static readonly object Sync = new();
    private static string? _sessionLogPath;

    private static string PreferredLogDirectory
    {
        get
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(local)) local = Path.GetTempPath();
            return Path.Combine(local, "Cosmo", "IMDataCore Data Migration Tool", "Logs");
        }
    }

    public static string LogDirectory
    {
        get
        {
            EnsureInitialized();
            string? directory = Path.GetDirectoryName(_sessionLogPath);
            return string.IsNullOrWhiteSpace(directory) ? PreferredLogDirectory : directory;
        }
    }

    public static string SessionLogPath
    {
        get
        {
            EnsureInitialized();
            return _sessionLogPath ?? "(diagnostic logging unavailable)";
        }
    }

    public static void EnsureInitialized()
    {
        lock (Sync)
        {
            if (!string.IsNullOrWhiteSpace(_sessionLogPath)) return;

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string fileName = $"data-migration-tool-{stamp}-{Environment.ProcessId}.log";
            string[] candidates =
            {
                PreferredLogDirectory,
                Path.Combine(Path.GetTempPath(), "Cosmo", "IMDataCore Data Migration Tool", "Logs"),
                Path.Combine(AppContext.BaseDirectory, "Logs")
            };

            foreach (string directory in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    string candidate = Path.Combine(directory, fileName);
                    File.AppendAllText(candidate, string.Empty, Encoding.UTF8);
                    _sessionLogPath = candidate;
                    WriteUnlocked("INFO", $"{ToolInfo.ProductName} {ToolInfo.Version} log started.", null);
                    WriteUnlocked("INFO", $"Executable: {Environment.ProcessPath ?? AppContext.BaseDirectory}", null);
                    WriteUnlocked("INFO", $"OS: {Environment.OSVersion}; .NET: {Environment.Version}; 64-bit process: {Environment.Is64BitProcess}", null);
                    return;
                }
                catch
                {
                    // Try the next writable location. Logging must never prevent startup.
                }
            }

            _sessionLogPath = "(diagnostic logging unavailable)";
        }
    }

    public static void Info(string message) => Write("INFO", message, null);
    public static void Warning(string message) => Write("WARN", message, null);
    public static void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            EnsureInitialized();
            lock (Sync) WriteUnlocked(level, message, exception);
        }
        catch
        {
            // Logging must never make the application fail.
        }
    }

    private static void WriteUnlocked(string level, string message, Exception? exception)
    {
        if (string.IsNullOrWhiteSpace(_sessionLogPath) || _sessionLogPath.StartsWith("(", StringComparison.Ordinal)) return;
        var sb = new StringBuilder();
        sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        sb.Append(" [").Append(level).Append("] ").AppendLine(message);
        if (exception is not null) sb.AppendLine(exception.ToString());
        File.AppendAllText(_sessionLogPath, sb.ToString(), Encoding.UTF8);
    }

    public static void OpenLogDirectory()
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(_sessionLogPath) || _sessionLogPath.StartsWith("(", StringComparison.Ordinal))
            throw new IOException("No writable diagnostic log directory was available for this session.");

        string? directory = Path.GetDirectoryName(_sessionLogPath);
        if (string.IsNullOrWhiteSpace(directory)) throw new IOException("Could not resolve the diagnostic log directory.");
        Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo("explorer.exe", directory) { UseShellExecute = true });
    }
}
