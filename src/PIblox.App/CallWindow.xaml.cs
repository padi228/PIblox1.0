using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace PIblox.App;

public partial class CallWindow : Window
{
    private bool _miniMode;

    public CallWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitializeCallBrowserAsync();
        Closed += async (_, _) =>
        {
            try
            {
                if (CallBrowser.CoreWebView2 is not null)
                    await CallBrowser.CoreWebView2.ExecuteScriptAsync("window.PADICall?.shutdown()");
            }
            catch
            {
                // The WebView may already be disposed while the window is closing.
            }
        };
    }

    private async Task InitializeCallBrowserAsync()
    {
        try
        {
            var assetsDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Call");
            var pagePath = Path.Combine(assetsDirectory, "index.html");
            if (!File.Exists(pagePath))
                throw new FileNotFoundException("Не найден встроенный интерфейс PADI Call.", pagePath);

            var webViewDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PIblox",
                "WebView2");
            Directory.CreateDirectory(webViewDataDirectory);

            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: webViewDataDirectory);

            await CallBrowser.EnsureCoreWebView2Async(environment);

            CallBrowser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "piblox.local",
                assetsDirectory,
                CoreWebView2HostResourceAccessKind.DenyCors);

            CallBrowser.CoreWebView2.PermissionRequested += (_, args) =>
            {
                args.State = args.PermissionKind == CoreWebView2PermissionKind.Microphone
                    ? CoreWebView2PermissionState.Allow
                    : CoreWebView2PermissionState.Deny;
            };

            CallBrowser.CoreWebView2.WebMessageReceived += CallBrowser_WebMessageReceived;
            CallBrowser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            CallBrowser.CoreWebView2.Settings.IsZoomControlEnabled = true;
            CallBrowser.DefaultBackgroundColor = System.Drawing.Color.FromArgb(255, 11, 14, 27);
            CallBrowser.Source = new Uri("https://piblox.local/index.html");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Не удалось открыть PADI Call. Проверь, установлен ли Microsoft Edge WebView2 Runtime.\n\n" + ex.Message,
                "PADI Call",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Close();
        }
    }

    private void CallBrowser_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.TryGetWebMessageAsString() != "toggle-mini")
            return;

        _miniMode = !_miniMode;
        if (_miniMode)
        {
            Width = 470;
            Height = 390;
            MinWidth = 420;
            MinHeight = 340;
        }
        else
        {
            Width = 980;
            Height = 730;
            MinWidth = 760;
            MinHeight = 620;
        }

        CallBrowser.CoreWebView2.PostWebMessageAsString(_miniMode ? "mini:on" : "mini:off");
    }
}