using System.Text.Json;
using PIblox.App.Models;

namespace PIblox.App.Services;

public sealed class SettingsService
{
    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PIblox");
    private readonly string _path;

    public SettingsService()
    {
        _path = Path.Combine(_directory, "settings.json");
    }

    public string SettingsDirectory => _directory;

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path));
                if (loaded is not null)
                {
                    loaded.Favorites ??= new List<GameShortcut>();
                    loaded.History ??= new List<GameShortcut>();
                    return loaded;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A damaged settings file should not prevent the launcher from opening.
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(_directory);
        var temporaryPath = _path + ".tmp";
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(temporaryPath, json);

        if (File.Exists(_path))
            File.Replace(temporaryPath, _path, null);
        else
            File.Move(temporaryPath, _path);
    }

    public void ExportTo(string destinationPath, AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(destinationPath, json);
    }

    public AppSettings ImportFrom(string sourcePath)
    {
        var info = new FileInfo(sourcePath);
        if (!info.Exists)
            throw new FileNotFoundException("Файл настроек не найден.", sourcePath);
        if (info.Length > 1024 * 1024)
            throw new InvalidDataException("Файл настроек слишком большой (максимум 1 МБ).");

        var imported = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(sourcePath))
            ?? throw new InvalidDataException("Файл не содержит настроек PIblox.");

        imported.Favorites = SanitizeShortcuts(imported.Favorites, 50);
        imported.History = SanitizeShortcuts(imported.History, 20);
        return imported;
    }

    private static List<GameShortcut> SanitizeShortcuts(IEnumerable<GameShortcut>? items, int limit)
    {
        var clean = new List<GameShortcut>();
        if (items is null)
            return clean;

        foreach (var item in items)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Url))
                continue;

            try
            {
                var safeUri = RobloxLinks.ResolveExperienceInput(item.Url);
                clean.Add(new GameShortcut
                {
                    Name = string.IsNullOrWhiteSpace(item.Name) ? safeUri.Host : item.Name[..Math.Min(item.Name.Length, 100)],
                    Url = safeUri.AbsoluteUri,
                    AddedAtUtc = item.AddedAtUtc
                });
            }
            catch (ArgumentException)
            {
                // Ignore links outside the official Roblox HTTPS domain.
            }

            if (clean.Count >= limit)
                break;
        }

        return clean;
    }
}

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; }
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public List<GameShortcut> Favorites { get; set; } = new();
    public List<GameShortcut> History { get; set; } = new();
}
