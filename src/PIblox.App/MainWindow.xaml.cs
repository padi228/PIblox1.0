using System.Windows;
using PIblox.App.Services;

namespace PIblox.App;

public partial class MainWindow : Window
{
    private readonly RobloxLocator _locator = new();
    private readonly SettingsService _settings = new();

    public MainWindow()
    {
        InitializeComponent();
        RefreshDetection();
        _settings.Load();
    }

    private void RefreshDetection()
    {
        var player = _locator.FindPlayer();
        var studio = _locator.FindStudio();
        PlayerStatus.Text = player is null ? "Not found on this computer" : "Detected";
        StudioStatus.Text = studio is null ? "Not found on this computer" : "Detected";
        PlayerButton.IsEnabled = player is not null;
        StudioButton.IsEnabled = studio is not null;
    }

    private void Player_Click(object sender, RoutedEventArgs e)
    {
        var path = _locator.FindPlayer();
        if (path is not null) LauncherService.Start(path);
        else MessageBox.Show("Roblox Player was not found.", "PIblox", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Studio_Click(object sender, RoutedEventArgs e)
    {
        var path = _locator.FindStudio();
        if (path is not null) LauncherService.Start(path);
        else MessageBox.Show("Roblox Studio was not found.", "PIblox", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Home_Click(object sender, RoutedEventArgs e) => RefreshDetection();

    private void Diagnostics_Click(object sender, RoutedEventArgs e)
    {
        DiagnosticsText.Text = DiagnosticsService.BuildReport(_locator);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Settings are currently stored locally. UI for editing them will be added later.", "PIblox Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
