using System.Text.Json;

namespace PIblox.App.Services;

public sealed class SettingsService
{
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PIblox", "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
        }
        catch { /* Fall back to safe defaults; settings are non-critical. */ }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public sealed class AppSettings
{
    public bool StartMinimized { get; set; }
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public string Theme { get; set; } = "Dark";
}
