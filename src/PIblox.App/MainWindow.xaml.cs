using System.IO;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using PIblox.App.Models;
using PIblox.App.Services;

namespace PIblox.App;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly RobloxLocator _robloxLocator = new();
    private readonly SystemMetricsService _metricsService = new();
    private readonly UpdateService _updateService = new();
    private readonly RobloxOAuthService _robloxOAuthService = new();
    private readonly RobloxStudioMcpClient _studioMcpClient = new();
    private readonly DispatcherTimer _metricsTimer;
    private AppSettings _settings;
    private CallWindow? _callWindow;

    public MainWindow()
    {
        InitializeComponent();

        _settings = _settingsService.Load();
        LoadSettingsIntoUi();
        RefreshRobloxAccountStatus();
        RefreshRobloxDetection();
        RefreshGameLists();
        GenerateReport();

        _metricsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _metricsTimer.Tick += (_, _) => RefreshMetrics();
        _metricsTimer.Start();

        Loaded += async (_, _) =>
        {
            RefreshMetrics();
            await RestoreRobloxConnectionAsync();
            if (_settings.CheckUpdatesOnStartup)
                await CheckUpdatesAsync(showDialog: false);
        };
        Closed += async (_, _) =>
        {
            _metricsTimer.Stop();
            await _studioMcpClient.DisposeAsync();
        };
    }

    private void LoadSettingsIntoUi()
    {
        try
        {
            StartupCheckBox.IsChecked = StartupService.IsEnabled();
        }
        catch
        {
            StartupCheckBox.IsChecked = _settings.StartWithWindows;
        }

        AutoUpdateCheckBox.IsChecked = _settings.CheckUpdatesOnStartup;
        RobloxOAuthClientIdBox.Text = _settings.RobloxOAuthClientId;
    }

    private void RefreshRobloxDetection()
    {
        var player = _robloxLocator.FindPlayer();
        var studio = _robloxLocator.FindStudio();

        PlayerStatus.Text = player is null ? "Не найдено" : "Установлено";
        StudioStatus.Text = studio is null ? "Не найдено" : "Установлено";
        PlayerButton.IsEnabled = player is not null;
        StudioButton.IsEnabled = studio is not null;
    }

    private void RefreshGameLists()
    {
        FavoritesList.ItemsSource = null;
        FavoritesList.ItemsSource = _settings.Favorites;
        HistoryList.ItemsSource = null;
        HistoryList.ItemsSource = _settings.History;
    }

    private void ShowPage(Grid page, Button activeButton, string title, string description)
    {
        HomePage.Visibility = Visibility.Collapsed;
        DiscoverPage.Visibility = Visibility.Collapsed;
        PerformancePage.Visibility = Visibility.Collapsed;
        StudioMcpPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Collapsed;
        DiagnosticsPage.Visibility = Visibility.Collapsed;

        page.Visibility = Visibility.Visible;
        PageTitle.Text = title;
        PageDescription.Text = description;

        foreach (var button in new[] { HomeNavButton, DiscoverNavButton, PerformanceNavButton, StudioMcpNavButton, CallNavButton, SettingsNavButton, DiagnosticsNavButton })
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
        }

        activeButton.Background = new SolidColorBrush(Color.FromRgb(48, 44, 92));
        activeButton.BorderBrush = new SolidColorBrush(Color.FromRgb(81, 74, 145));
    }

    private void HomeNav_Click(object sender, RoutedEventArgs e) =>
        ShowPage(HomePage, HomeNavButton, "Главная", "Быстрый запуск Roblox Player и Studio");

    private void DiscoverNav_Click(object sender, RoutedEventArgs e) =>
        ShowPage(DiscoverPage, DiscoverNavButton, "Поиск игр", "Открывай официальный поиск Roblox по названию, ID или ссылке");

    private void PerformanceNav_Click(object sender, RoutedEventArgs e) =>
        ShowPage(PerformancePage, PerformanceNavButton, "Производительность", "Локальные показатели Windows без рискованных системных твиков");

    private void StudioMcpNav_Click(object sender, RoutedEventArgs e) =>
        ShowPage(StudioMcpPage, StudioMcpNavButton, "Roblox Studio Link", "Локальное read-only подключение к официальному MCP-серверу Roblox Studio");

    private void CallNav_Click(object sender, RoutedEventArgs e)
    {
        foreach (var button in new[] { HomeNavButton, DiscoverNavButton, PerformanceNavButton, StudioMcpNavButton, CallNavButton, SettingsNavButton, DiagnosticsNavButton })
        {
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
        }

        CallNavButton.Background = new SolidColorBrush(Color.FromRgb(48, 44, 92));
        CallNavButton.BorderBrush = new SolidColorBrush(Color.FromRgb(81, 74, 145));

        if (_callWindow is null || !_callWindow.IsLoaded)
        {
            _callWindow = new CallWindow { Owner = this };
            _callWindow.Closed += (_, _) => _callWindow = null;
        }

        _callWindow.Show();
        _callWindow.Activate();
    }

    private void SettingsNav_Click(object sender, RoutedEventArgs e) =>
        ShowPage(SettingsPage, SettingsNavButton, "Настройки", "Локальные предпочтения и проверка обновлений");

    private void DiagnosticsNav_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(DiagnosticsPage, DiagnosticsNavButton, "Диагностика", "Создание локального отчёта о состоянии PIblox и обнаружении Roblox");
        GenerateReport();
    }

    private void LaunchPlayer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LauncherService.LaunchPlayer(_robloxLocator.FindPlayer());
            StatusText.Text = "Запуск Roblox Player";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PIblox", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void LaunchStudio_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LauncherService.LaunchStudio(_robloxLocator.FindStudio());
            StatusText.Text = "Запуск Roblox Studio";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PIblox", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void ConnectStudioMcp_Click(object sender, RoutedEventArgs e)
    {
        if (!_studioMcpClient.IsConnected)
        {
            var consent = MessageBox.Show(
                "PIblox запустит встроенный локальный MCP-сервер Roblox Studio.\n\n" +
                "В этой версии PIblox использует только просмотр открытых окон Studio и чтение их состояния. " +
                "Он не редактирует скрипты, не запускает произвольный код, не загружает ассеты и не публикует игру.\n\n" +
                "Продолжить?",
                "Подключение Roblox Studio",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (consent != MessageBoxResult.Yes)
                return;
        }

        ConnectStudioMcpButton.IsEnabled = false;
        StudioMcpStatusText.Text = "Запускаем официальный connector…";
        StatusText.Text = "Подключение Roblox Studio…";

        try
        {
            var output = await _studioMcpClient.ConnectAndListStudiosAsync();
            StudioMcpOutputBox.Text = output;
            StudioMcpStatusText.Text = "MCP подключён · используется read-only режим";
            RefreshStudioListButton.IsEnabled = true;
            GetStudioStateButton.IsEnabled = true;
            DisconnectStudioMcpButton.IsEnabled = true;
            StatusText.Text = "Roblox Studio подключена";
        }
        catch (Exception ex)
        {
            StudioMcpStatusText.Text = "Не удалось подключиться к Roblox Studio";
            StudioMcpOutputBox.Text = ex.Message;
            StatusText.Text = "Ошибка Studio MCP";
        }
        finally
        {
            ConnectStudioMcpButton.IsEnabled = true;
        }
    }

    private async void RefreshStudioList_Click(object sender, RoutedEventArgs e)
    {
        RefreshStudioListButton.IsEnabled = false;
        try
        {
            StudioMcpOutputBox.Text = await _studioMcpClient.ListStudiosAsync();
            StudioMcpStatusText.Text = "Список открытых окон Studio обновлён";
        }
        catch (Exception ex)
        {
            StudioMcpStatusText.Text = "Не удалось обновить список Studio";
            StudioMcpOutputBox.Text = ex.Message;
        }
        finally
        {
            RefreshStudioListButton.IsEnabled = _studioMcpClient.IsConnected;
        }
    }

    private async void GetStudioState_Click(object sender, RoutedEventArgs e)
    {
        GetStudioStateButton.IsEnabled = false;
        try
        {
            StudioMcpOutputBox.Text = await _studioMcpClient.GetStudioStateAsync(StudioIdBox.Text);
            StudioMcpStatusText.Text = "Состояние выбранного окна Studio получено";
        }
        catch (Exception ex)
        {
            StudioMcpStatusText.Text = "Не удалось получить состояние Studio";
            StudioMcpOutputBox.Text = ex.Message;
        }
        finally
        {
            GetStudioStateButton.IsEnabled = _studioMcpClient.IsConnected;
        }
    }

    private async void DisconnectStudioMcp_Click(object sender, RoutedEventArgs e)
    {
        await _studioMcpClient.DisconnectAsync();
        RefreshStudioListButton.IsEnabled = false;
        GetStudioStateButton.IsEnabled = false;
        DisconnectStudioMcpButton.IsEnabled = false;
        StudioMcpStatusText.Text = "Roblox Studio MCP отключён";
        StudioMcpOutputBox.Text = "Соединение закрыто. Открытые проекты Studio не изменены.";
        StatusText.Text = "Studio отключена";
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (ReferenceEquals(sender, HomeSearchBox))
                SearchAndOpen(HomeSearchBox.Text);
            e.Handled = true;
        }
    }

    private void HomeSearch_Click(object sender, RoutedEventArgs e) => SearchAndOpen(HomeSearchBox.Text);

    private void DiscoverSearch_Click(object sender, RoutedEventArgs e) => SearchAndOpen(DiscoverSearchBox.Text);

    private void OpenGameId_Click(object sender, RoutedEventArgs e) => SearchAndOpen(GameIdBox.Text);

    private void SearchAndOpen(string input)
    {
        try
        {
            var uri = RobloxLinks.ResolveExperienceInput(input);
            LauncherService.OpenWebPage(uri);
            RecordHistory(input.Trim(), uri);
            StatusText.Text = "Открыта страница Roblox";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(ex.Message, "Поиск Roblox", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void AddFavorite_Click(object sender, RoutedEventArgs e) => AddFavorite(HomeSearchBox.Text);

    private void AddDiscoverFavorite_Click(object sender, RoutedEventArgs e)
    {
        var value = string.IsNullOrWhiteSpace(GameIdBox.Text) ? DiscoverSearchBox.Text : GameIdBox.Text;
        AddFavorite(value);
    }

    private void AddFavorite(string input)
    {
        try
        {
            var uri = RobloxLinks.ResolveExperienceInput(input);
            var name = input.Trim();
            if (_settings.Favorites.Any(item => string.Equals(item.Url, uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase)))
            {
                StatusText.Text = "Уже в избранном";
                return;
            }

            _settings.Favorites.Insert(0, new GameShortcut { Name = name, Url = uri.AbsoluteUri });
            if (_settings.Favorites.Count > 50)
                _settings.Favorites.RemoveAt(_settings.Favorites.Count - 1);

            SaveCurrentSettings();
            RefreshGameLists();
            StatusText.Text = "Добавлено в избранное";
        }
        catch (Exception ex) when (ex is ArgumentException)
        {
            MessageBox.Show(ex.Message, "Избранное", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void RecordHistory(string name, Uri uri)
    {
        _settings.History.RemoveAll(item => string.Equals(item.Url, uri.AbsoluteUri, StringComparison.OrdinalIgnoreCase));
        _settings.History.Insert(0, new GameShortcut { Name = string.IsNullOrWhiteSpace(name) ? uri.Host : name, Url = uri.AbsoluteUri });
        if (_settings.History.Count > 20)
            _settings.History.RemoveRange(20, _settings.History.Count - 20);

        SaveCurrentSettings();
        RefreshGameLists();
    }

    private void FavoritesList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelectedShortcut(FavoritesList.SelectedItem as GameShortcut);
    private void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelectedShortcut(HistoryList.SelectedItem as GameShortcut);

    private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (FavoritesList.SelectedItem is not GameShortcut selected)
        {
            StatusText.Text = "Сначала выбери избранное";
            return;
        }

        _settings.Favorites.Remove(selected);
        SaveCurrentSettings();
        RefreshGameLists();
        StatusText.Text = "Удалено из избранного";
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        _settings.History.Clear();
        SaveCurrentSettings();
        RefreshGameLists();
        StatusText.Text = "История очищена";
    }

    private void OpenSelectedShortcut(GameShortcut? shortcut)
    {
        if (shortcut is null)
            return;

        try
        {
            var uri = RobloxLinks.ResolveExperienceInput(shortcut.Url);
            LauncherService.OpenWebPage(uri);
            RecordHistory(shortcut.Name, uri);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PIblox", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void ConnectRoblox_Click(object sender, RoutedEventArgs e)
    {
        var clientId = RobloxOAuthClientIdBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            ShowPage(SettingsPage, SettingsNavButton, "Настройки", "Добавь Roblox OAuth Client ID для подключения аккаунта");
            RobloxOAuthClientIdBox.Focus();
            MessageBox.Show(
                "Сначала добавь Client ID зарегистрированного OAuth-приложения Roblox в Настройках PIblox.\n\n" +
                "Регистрацию приложения выполняет его владелец в Roblox Creator Dashboard. Пароль Roblox и cookies PIblox не нужны.",
                "Подключение Roblox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _settings.RobloxOAuthClientId = clientId;
        SaveCurrentSettings();
        ConnectRobloxButton.IsEnabled = false;
        AccountConnectionStatus.Text = "Ожидаем подтверждение входа в браузере…";
        StatusText.Text = "Подключение к Roblox…";

        try
        {
            var profile = await _robloxOAuthService.ConnectAsync(clientId);
            ApplyRobloxProfile(profile);
            StatusText.Text = "Аккаунт Roblox подключён";
            MessageBox.Show(
                $"Готово! PIblox подключён к аккаунту @{profile.Username}.\n\n" +
                "Токены подключения сохранены в зашифрованном виде средствами Windows.",
                "Roblox подключён", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            AccountConnectionStatus.Text = "Подключение отменено или время ожидания истекло";
            StatusText.Text = "Подключение отменено";
        }
        catch (Exception ex)
        {
            AccountConnectionStatus.Text = "Не удалось подключить аккаунт Roblox";
            StatusText.Text = "Ошибка OAuth-подключения";
            MessageBox.Show(ex.Message, "Подключение Roblox", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            ConnectRobloxButton.IsEnabled = true;
        }
    }

    private void DisconnectRoblox_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            "Удалить сохранённое подключение Roblox с этого компьютера?\n\n" +
            "Локальные токены PIblox будут удалены. При необходимости доступ приложения можно отдельно отозвать в настройках Roblox.",
            "Отключить Roblox", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        _robloxOAuthService.ClearSavedConnection();
        _settings.RobloxUserId = string.Empty;
        _settings.RobloxUsername = string.Empty;
        _settings.RobloxDisplayName = string.Empty;
        _settings.RobloxProfileUrl = string.Empty;
        SaveCurrentSettings();
        RefreshRobloxAccountStatus();
        StatusText.Text = "Аккаунт Roblox отключён на этом компьютере";
    }

    private void RefreshRobloxAccountStatus()
    {
        var connected = !string.IsNullOrWhiteSpace(_settings.RobloxUserId)
            && !string.IsNullOrWhiteSpace(_settings.RobloxUsername);

        if (connected)
        {
            AccountConnectionStatus.Text = $"Подключён: {_settings.RobloxDisplayName} (@{_settings.RobloxUsername})";
            ConnectRobloxButton.Content = "Переподключить ↗";
            DisconnectRobloxButton.Visibility = Visibility.Visible;
        }
        else
        {
            AccountConnectionStatus.Text = "Аккаунт Roblox ещё не подключён через OAuth";
            ConnectRobloxButton.Content = "Подключить Roblox ↗";
            DisconnectRobloxButton.Visibility = Visibility.Collapsed;
        }
    }

    private async Task RestoreRobloxConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.RobloxUserId))
        {
            RefreshRobloxAccountStatus();
            return;
        }

        RefreshRobloxAccountStatus();
        if (string.IsNullOrWhiteSpace(_settings.RobloxOAuthClientId))
            return;

        try
        {
            var profile = await _robloxOAuthService.GetSavedProfileAsync(_settings.RobloxOAuthClientId);
            if (profile is not null)
                ApplyRobloxProfile(profile);
        }
        catch
        {
            StatusText.Text = "Профиль Roblox сохранён; для обновления войди снова";
        }
    }

    private void ApplyRobloxProfile(RobloxProfile profile)
    {
        _settings.RobloxUserId = profile.UserId;
        _settings.RobloxUsername = profile.Username;
        _settings.RobloxDisplayName = profile.DisplayName;
        _settings.RobloxProfileUrl = profile.ProfileUrl;
        SaveCurrentSettings();
        RefreshRobloxAccountStatus();
    }

    private void OpenRobloxWebsite_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LauncherService.OpenWebPage(new Uri("https://www.roblox.com/"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PIblox", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RefreshMetrics()
    {
        try
        {
            var metrics = _metricsService.Read();
            if (metrics.CpuPercent.HasValue)
            {
                CpuValueText.Text = $"{metrics.CpuPercent.Value:0}%";
                CpuProgress.Value = metrics.CpuPercent.Value;
            }

            if (metrics.MemoryPercent.HasValue)
            {
                MemoryValueText.Text = $"{metrics.MemoryPercent.Value:0}%";
                MemoryProgress.Value = metrics.MemoryPercent.Value;
            }

            if (metrics.TotalMemoryBytes.HasValue && metrics.AvailableMemoryBytes.HasValue)
            {
                var totalGb = metrics.TotalMemoryBytes.Value / 1024d / 1024d / 1024d;
                var usedGb = (metrics.TotalMemoryBytes.Value - metrics.AvailableMemoryBytes.Value) / 1024d / 1024d / 1024d;
                MemoryDetailText.Text = $"{usedGb:0.0} ГБ из {totalGb:0.0} ГБ";
            }
        }
        catch
        {
            CpuValueText.Text = "Недоступно";
            MemoryValueText.Text = "Недоступно";
        }
    }

    private async void Ping_Click(object sender, RoutedEventArgs e)
    {
        PingValueText.Text = "Проверка…";
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync("www.roblox.com", 3000);
            PingValueText.Text = reply.Status == IPStatus.Success
                ? $"Ответ получен: {reply.RoundtripTime} мс"
                : $"Нет ответа: {reply.Status}";
        }
        catch (Exception ex) when (ex is PingException or InvalidOperationException or TaskCanceledException)
        {
            PingValueText.Text = "Проверка не удалась: нет ответа от сети.";
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var requestedStartup = StartupCheckBox.IsChecked == true;
        try
        {
            StartupService.SetEnabled(requestedStartup);
            _settings.StartWithWindows = requestedStartup;
            _settings.CheckUpdatesOnStartup = AutoUpdateCheckBox.IsChecked == true;
            _settings.RobloxOAuthClientId = RobloxOAuthClientIdBox.Text.Trim();
            SaveCurrentSettings();
            StatusText.Text = "Настройки сохранены";
            MessageBox.Show("Настройки сохранены.", "PIblox", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось применить настройки запуска Windows.\n{ex.Message}", "PIblox", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveCurrentSettings()
    {
        _settingsService.Save(_settings);
    }

    private void ExportSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Экспорт настроек PIblox",
            Filter = "Файл настроек PIblox (*.json)|*.json",
            FileName = "PIblox_Settings.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _settingsService.ExportTo(dialog.FileName, _settings);
            StatusText.Text = "Настройки экспортированы";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Экспорт настроек", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ImportSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Импорт настроек PIblox",
            Filter = "Файл настроек PIblox (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _settings = _settingsService.ImportFrom(dialog.FileName);
            StartupCheckBox.IsChecked = _settings.StartWithWindows;
            RobloxOAuthClientIdBox.Text = _settings.RobloxOAuthClientId;
            RefreshRobloxAccountStatus();
            AutoUpdateCheckBox.IsChecked = _settings.CheckUpdatesOnStartup;
            SaveCurrentSettings();
            RefreshGameLists();
            StatusText.Text = "Настройки импортированы";
            MessageBox.Show(
                "Настройки импортированы. Чтобы применить запуск PIblox вместе с Windows, проверь этот параметр и нажми «Сохранить настройки».",
                "PIblox",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Импорт настроек", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_settingsService.SettingsDirectory);
            Process.Start(new ProcessStartInfo("explorer.exe", _settingsService.SettingsDirectory) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PIblox", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void CheckUpdates_Click(object sender, RoutedEventArgs e) => await CheckUpdatesAsync(showDialog: true);

    private async Task CheckUpdatesAsync(bool showDialog)
    {
        StatusText.Text = "Проверка обновлений…";
        var result = await _updateService.CheckAsync();
        StatusText.Text = result.UpdateAvailable ? "Есть обновление" : "Проверка завершена";

        if (!showDialog)
        {
            if (result.UpdateAvailable)
                StatusText.Text = result.Message;
            return;
        }

        var answer = MessageBox.Show(
            result.Message + (result.UpdateAvailable ? "\n\nОткрыть страницу релизов GitHub?" : ""),
            "Обновления PIblox",
            result.UpdateAvailable ? MessageBoxButton.YesNo : MessageBoxButton.OK,
            result.UpdateAvailable ? MessageBoxImage.Information : MessageBoxImage.Information);

        if (result.UpdateAvailable && answer == MessageBoxResult.Yes)
            LauncherService.OpenWebPage(new Uri(result.ReleasesUrl));
    }

    private void GenerateReport()
    {
        DiagnosticsBox.Text = DiagnosticsService.BuildReport(_robloxLocator);
    }

    private void GenerateReport_Click(object sender, RoutedEventArgs e)
    {
        GenerateReport();
        StatusText.Text = "Отчёт создан локально";
    }

    private void CopyReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(DiagnosticsBox.Text);
            StatusText.Text = "Отчёт скопирован";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PIblox", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SaveReport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить отчёт PIblox",
            Filter = "Текстовый файл (*.txt)|*.txt",
            FileName = "PIblox_Diagnostics.txt"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, DiagnosticsBox.Text);
            StatusText.Text = "Отчёт сохранён";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "PIblox", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
