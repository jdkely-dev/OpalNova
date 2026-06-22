using System.IO;
using Microsoft.Data.Sqlite;
using System.Threading;
using JewelleryBusinessManager.Data;

namespace JewelleryBusinessManager.Services;

public static class BackupService
{
    public static string CreateBackup()
    {
        var backupDirectory = BusinessSettingsService.GetBackupFolder();
        Directory.CreateDirectory(backupDirectory);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var destination = Path.Combine(backupDirectory, $"jbm-backup-{timestamp}.db");

        DatabaseBootstrapper.Initialize();
        CreateSQLiteSnapshot(DatabaseBootstrapper.DatabasePath, destination);
        return destination;
    }

    public static void CreateSQLiteSnapshot(string sourceDatabasePath, string destinationDatabasePath)
    {
        if (string.IsNullOrWhiteSpace(sourceDatabasePath))
            throw new ArgumentException("Source database path was empty.", nameof(sourceDatabasePath));
        if (!File.Exists(sourceDatabasePath))
            throw new FileNotFoundException("The active database file could not be found for backup.", sourceDatabasePath);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationDatabasePath) ?? AppContext.BaseDirectory);
        TryDeleteFile(destinationDatabasePath);

        Exception? lastError = null;

        // Preferred method: SQLite online backup API. This is designed to copy a live SQLite database safely.
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                SqliteConnection.ClearAllPools();
                using var source = new SqliteConnection($"Data Source={sourceDatabasePath};Mode=ReadOnly;Cache=Shared;Default Timeout=30");
                using var destination = new SqliteConnection($"Data Source={destinationDatabasePath};Default Timeout=30");
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);
                return;
            }
            catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
            {
                lastError = ex;
                TryDeleteFile(destinationDatabasePath);
                Thread.Sleep(250 * attempt);
            }
        }

        // Fallback: copy with shared-read access. This is less ideal than SQLite's backup API,
        // but it avoids common Windows file-lock failures when another SQLite handle is still open.
        try
        {
            CopyFileSharedRead(sourceDatabasePath, destinationDatabasePath);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDeleteFile(destinationDatabasePath);
            throw new InvalidOperationException(
                "Backup could not create a database snapshot. Close all other running copies of OPALNOVA and Visual Studio debug sessions, then try again. " +
                $"Active database: {sourceDatabasePath}", lastError ?? ex);
        }
    }

    public static void CopyFileSharedRead(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? AppContext.BaseDirectory);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
                source.CopyTo(destination);
                return;
            }
            catch (IOException) when (attempt < 5)
            {
                Thread.Sleep(200 * attempt);
            }
            catch (UnauthorizedAccessException) when (attempt < 5)
            {
                Thread.Sleep(200 * attempt);
            }
        }

        using var finalSource = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var finalDestination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        finalSource.CopyTo(finalDestination);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

}
