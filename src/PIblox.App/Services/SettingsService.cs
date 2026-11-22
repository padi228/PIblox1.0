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
}

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; }
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public List<GameShortcut> Favorites { get; set; } = new();
    public List<GameShortcut> History { get; set; } = new();
}
