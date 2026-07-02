using System.IO;
using System.Text.Json;
using JewelleryBusinessManager.Data;

namespace JewelleryBusinessManager.Services;

public sealed class SavedSearchView
{
    public string Name { get; set; } = string.Empty;
    public string Section { get; set; } = "All Sections";
    public string SearchText { get; set; } = string.Empty;
    public string FilterPreset { get; set; } = "All Records";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public override string ToString() => string.IsNullOrWhiteSpace(Name)
        ? $"{Section} - {FilterPreset}"
        : $"{Name} - {Section} - {FilterPreset}";
}

public static class SavedViewService
{
    public static string FilePath => Path.Combine(DatabaseBootstrapper.AppDataDirectory, "saved-search-views.json");

    public static List<SavedSearchView> LoadViews()
    {
        try
        {
            Directory.CreateDirectory(DatabaseBootstrapper.AppDataDirectory);
            if (!File.Exists(FilePath)) return new List<SavedSearchView>();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<SavedSearchView>>(json) ?? new List<SavedSearchView>();
        }
        catch
        {
            return new List<SavedSearchView>();
        }
    }

    public static void SaveView(SavedSearchView view)
    {
        if (string.IsNullOrWhiteSpace(view.Name))
            throw new InvalidOperationException("Saved view name is required.");

        var views = LoadViews();
        var existing = views.FirstOrDefault(v => v.Name.Equals(view.Name, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            views.Add(view);
        }
        else
        {
            existing.Section = view.Section;
            existing.SearchText = view.SearchText;
            existing.FilterPreset = view.FilterPreset;
            existing.UpdatedAt = DateTime.Now;
        }
        SaveAll(views);
    }

    public static void DeleteView(string name)
    {
        var views = LoadViews();
        views.RemoveAll(v => v.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        SaveAll(views);
    }

    private static void SaveAll(List<SavedSearchView> views)
    {
        Directory.CreateDirectory(DatabaseBootstrapper.AppDataDirectory);
        var json = JsonSerializer.Serialize(views.OrderBy(v => v.Name).ToList(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
