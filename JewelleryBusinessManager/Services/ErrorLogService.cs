using System.IO;
using JewelleryBusinessManager.Data;

namespace JewelleryBusinessManager.Services;

public static class ErrorLogService
{
    public static string LogDirectory => Path.Combine(DatabaseBootstrapper.AppDataDirectory, "Logs");
    public static string LogPath => Path.Combine(LogDirectory, "jbm-error-log.txt");

    public static void Log(Exception ex, string context)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\n{ex}\n\n";
            File.AppendAllText(LogPath, entry);
        }
        catch
        {
            // Never let logging errors crash the app.
        }
    }

    public static string ReadLog()
    {
        Directory.CreateDirectory(LogDirectory);
        if (!File.Exists(LogPath))
            File.WriteAllText(LogPath, "No errors have been logged yet.\n");
        return File.ReadAllText(LogPath);
    }

    public static void ClearLog()
    {
        Directory.CreateDirectory(LogDirectory);
        File.WriteAllText(LogPath, "Error log cleared at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine);
    }
}
