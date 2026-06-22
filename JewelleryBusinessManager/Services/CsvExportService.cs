using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using JewelleryBusinessManager.Data;

namespace JewelleryBusinessManager.Services;

public static class CsvExportService
{
    public static string ExportObjects(IEnumerable<object> records, Type recordType, string filePrefix)
    {
        var exportDir = Path.Combine(DatabaseBootstrapper.AppDataDirectory, "Exports");
        Directory.CreateDirectory(exportDir);
        var path = Path.Combine(exportDir, $"{filePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        var props = recordType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", props.Select(p => Escape(p.Name))));
        foreach (var record in records)
        {
            sb.AppendLine(string.Join(",", props.Select(p => Escape(Convert.ToString(p.GetValue(record), CultureInfo.InvariantCulture) ?? string.Empty))));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    public static string Export<T>(IEnumerable<T> records, string filePrefix)
    {
        var exportDir = Path.Combine(DatabaseBootstrapper.AppDataDirectory, "Exports");
        Directory.CreateDirectory(exportDir);
        var path = Path.Combine(exportDir, $"{filePrefix}-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", props.Select(p => Escape(p.Name))));
        foreach (var record in records)
        {
            sb.AppendLine(string.Join(",", props.Select(p => Escape(Convert.ToString(p.GetValue(record), CultureInfo.InvariantCulture) ?? string.Empty))));
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private static string Escape(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
