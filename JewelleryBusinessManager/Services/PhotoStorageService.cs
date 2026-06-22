using System.IO;

namespace JewelleryBusinessManager.Services;

public static class PhotoStorageService
{
    private static readonly string PhotosFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JewelleryBusinessManager",
        "Photos");

    public static string CopyPhotoToAppFolder(string sourcePath, string entityType, int entityId)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new FileNotFoundException("The selected photo could not be found.", sourcePath);

        Directory.CreateDirectory(PhotosFolder);

        var extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        var safeEntityType = MakeSafeFilePart(entityType);
        var fileName = $"{safeEntityType}_{entityId}_{DateTime.Now:yyyyMMdd_HHmmssfff}{extension}";
        var destinationPath = Path.Combine(PhotosFolder, fileName);
        File.Copy(sourcePath, destinationPath, overwrite: false);
        return destinationPath;
    }

    public static bool LooksLikeImage(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp";
    }

    private static string MakeSafeFilePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }
}
