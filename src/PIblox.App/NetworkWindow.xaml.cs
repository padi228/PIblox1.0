using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Windows;
using PIblox.App.Services;

namespace PIblox.App;

public partial class NetworkWindow : Window
{
    private const string WireGuardInstallUrl = "https://www.wireguard.com/install/";
    private readonly NetworkDiagnosticsService _diagnosticsService = new();

    public NetworkWindow()
    {
        InitializeComponent();
        RefreshDiagnostics();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshDiagnostics();

    private void RefreshDiagnostics()
    {
        try
        {
            var snapshot = _diagnosticsService.Check();

            EngineStatusText.Text = snapshot.WireGuardInstalled ? "Установлен" : "Не найден";
            AdapterStatusText.Text = snapshot.WireGuardAdapterCount == 0
                ? "Не обнаружен"
                : $"{snapshot.ActiveWireGuardAdapterCount} активно / {snapshot.WireGuardAdapterCount}";

            TunnelStatusText.Text = snapshot.ActiveWireGuardAdapterCount > 0
                ? "Есть активный адаптер"
                : "Не подключён";

            NetworkStatusBadge.Text = snapshot.WireGuardInstalled ? "ДИАГНОСТИКА ГОТОВА" : "НУЖЕН WIREGUARD";

            var lines = new List<string>
            {
                "PIblox Network — локальная диагностика",
                "=====================================",
                $"Проверено: {snapshot.CheckedAt:yyyy-MM-dd HH:mm:ss zzz}",
                $"WireGuard: {(snapshot.WireGuardInstalled ? "найден" : "не найден")}",
                $"Путь: {snapshot.WireGuardPath ?? "не обнаружен"}",
                $"Адаптеры WireGuard: {snapshot.WireGuardAdapterCount}",
                $"Активные адаптеры WireGuard: {snapshot.ActiveWireGuardAdapterCount}",
                "",
                "Обнаруженные адаптеры:"
            };

            if (snapshot.AdapterDetails.Count == 0)
                lines.Add("— нет");
            else
                lines.AddRange(snapshot.AdapterDetails.Select(item => $"— {item}"));

            lines.Add("");
            lines.Add("Серверный профиль: не импортирован в PIblox.");
            lines.Add("Сетевые маршруты и DNS не изменялись.");
            lines.Add("PIblox не собирает и не отправляет сетевые данные.");

            DiagnosticsTextBox.Text = string.Join(Environment.NewLine, lines);
        }
        catch (NetworkInformationException ex)
        {
            NetworkStatusBadge.Text = "ОШИБКА ДИАГНОСТИКИ";
            DiagnosticsTextBox.Text = $"Не удалось прочитать сетевые адаптеры Windows.\n{ex.Message}";
        }
        catch (Exception ex)
        {
            NetworkStatusBadge.Text = "ОШИБКА ДИАГНОСТИКИ";
            DiagnosticsTextBox.Text = $"Диагностика завершилась ошибкой.\n{ex.Message}";
        }
    }

    private void OpenWireGuard_Click(object sender, RoutedEventArgs e)
    {
        var snapshot = _diagnosticsService.Check();
        var target = snapshot.WireGuardPath ?? WireGuardInstallUrl;

        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть WireGuard.\n{ex.Message}",
                "PIblox Network", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OpenInstallPage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(WireGuardInstallUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось открыть официальный сайт WireGuard.\n{ex.Message}",
                "PIblox Network", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
