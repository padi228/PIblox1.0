using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace PIblox.App.Services;

/// <summary>
/// Minimal MCP stdio client for Roblox Studio's built-in local MCP server.
/// PIblox exposes only read-only discovery and state inspection in this preview.
/// </summary>
public sealed class RobloxStudioMcpClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private Process? _process;
    private Task<string>? _stderrReader;
    private int _nextRequestId;

    public bool IsConnected => _process is { HasExited: false };

    public async Task<string> ConnectAndListStudiosAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            return await ListStudiosAsync(cancellationToken);

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var mcpBatchPath = Path.Combine(localAppData, "Roblox", "mcp.bat");
        if (!File.Exists(mcpBatchPath))
        {
            throw new FileNotFoundException(
                "Не найден официальный Roblox Studio MCP connector.\n\n" +
                "Открой Roblox Studio → Assistant → Manage MCP Servers → включи " +
                "«Enable Studio as MCP server». После этого повтори подключение в PIblox.",
                mcpBatchPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            Arguments = $"/d /s /c \"\"{mcpBatchPath}\"\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };

        var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Не удалось запустить официальный Roblox Studio MCP connector.");
        }

        _process = process;
        _stderrReader = process.StandardError.ReadToEndAsync();

        try
        {
            await SendRequestAsync(
                "initialize",
                new
                {
                    protocolVersion = "2025-06-18",
                    capabilities = new { },
                    clientInfo = new { name = "PIblox", version = "1.0.2" }
                },
                cancellationToken);

            await SendNotificationAsync("notifications/initialized", new { }, cancellationToken);

            // Ask the server for its available tools so a clear diagnostic is shown.
            var toolsJson = await SendRequestAsync(
                "tools/list",
                new { },
                cancellationToken);

            var availableTools = ReadToolNames(toolsJson);
            if (!availableTools.Contains("list_roblox_studios", StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Roblox Studio MCP подключился, но в списке инструментов нет list_roblox_studios. " +
                    "Обнови Roblox Studio до последней версии.");
            }

            var studios = await ListStudiosAsync(cancellationToken);
            return "Подключение установлено.\n\nДоступные инструменты:\n" +
                   string.Join("\n", availableTools.Select(name => "• " + name)) +
                   "\n\nОткрытые экземпляры Roblox Studio:\n" + studios;
        }
        catch (Exception ex)
        {
            var diagnostic = await ReadStderrAsync();
            await DisconnectAsync();
            if (!string.IsNullOrWhiteSpace(diagnostic))
                throw new InvalidOperationException(ex.Message + "\n\nВывод connector:\n" + diagnostic, ex);
            throw;
        }
    }

    public async Task<string> ListStudiosAsync(CancellationToken cancellationToken = default)
    {
        var resultJson = await CallToolAsync(
            "list_roblox_studios",
            new Dictionary<string, object?>(),
            cancellationToken);

        return FormatToolResult(resultJson);
    }

    public async Task<string> GetStudioStateAsync(
        string studioId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studioId))
            throw new ArgumentException("Укажи Studio ID из списка открытых окон.", nameof(studioId));

        var resultJson = await CallToolAsync(
            "get_studio_state",
            new Dictionary<string, object?> { ["studio_id"] = studioId.Trim() },
            cancellationToken);

        return FormatToolResult(resultJson);
    }

    private async Task<string> CallToolAsync(
        string name,
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        return await SendRequestAsync(
            "tools/call",
            new { name, arguments },
            cancellationToken);
    }

    private async Task<string> SendRequestAsync(
        string method,
        object parameters,
        CancellationToken cancellationToken)
    {
        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            var process = GetRunningProcess();
            var requestId = Interlocked.Increment(ref _nextRequestId);
            var request = new
            {
                jsonrpc = "2.0",
                id = requestId,
                method,
                @params = parameters
            };

            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request));
            await process.StandardInput.FlushAsync(cancellationToken);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));

            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync(timeout.Token);
                if (line is null)
                    throw new EndOfStreamException("Roblox Studio MCP connector завершил работу.");

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                JsonDocument message;
                try
                {
                    message = JsonDocument.Parse(line);
                }
                catch (JsonException)
                {
                    // Ignore non-protocol diagnostic lines. MCP protocol data itself is JSON-RPC.
                    continue;
                }

                using (message)
                {
                    var root = message.RootElement;
                    if (!root.TryGetProperty("id", out var idElement) ||
                        idElement.ValueKind != JsonValueKind.Number ||
                        idElement.GetInt32() != requestId)
                    {
                        continue;
                    }

                    if (root.TryGetProperty("error", out var error))
                    {
                        var errorText = error.TryGetProperty("message", out var errorMessage)
                            ? errorMessage.GetString()
                            : error.GetRawText();
                        throw new InvalidOperationException(
                            $"Roblox Studio MCP: {errorText ?? "неизвестная ошибка"}");
                    }

                    if (!root.TryGetProperty("result", out var result))
                        throw new InvalidDataException("Roblox Studio MCP вернул ответ без result.");

                    return result.GetRawText();
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Roblox Studio MCP не ответил за 20 секунд.");
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private async Task SendNotificationAsync(
        string method,
        object parameters,
        CancellationToken cancellationToken)
    {
        await _ioLock.WaitAsync(cancellationToken);
        try
        {
            var process = GetRunningProcess();
            var notification = new
            {
                jsonrpc = "2.0",
                method,
                @params = parameters
            };

            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(notification));
            await process.StandardInput.FlushAsync(cancellationToken);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private Process GetRunningProcess()
    {
        if (_process is not { HasExited: false } process)
            throw new InvalidOperationException("Roblox Studio MCP не подключён. Сначала нажми «Подключить».");

        return process;
    }

    private static List<string> ReadToolNames(string toolsJson)
    {
        using var document = JsonDocument.Parse(toolsJson);
        var names = new List<string>();
        if (document.RootElement.TryGetProperty("tools", out var tools) &&
            tools.ValueKind == JsonValueKind.Array)
        {
            foreach (var tool in tools.EnumerateArray())
            {
                if (tool.TryGetProperty("name", out var name) &&
                    name.ValueKind == JsonValueKind.String)
                {
                    var value = name.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        names.Add(value);
                }
            }
        }

        return names;
    }

    private static string FormatToolResult(string resultJson)
    {
        using var document = JsonDocument.Parse(resultJson);
        var root = document.RootElement;

        if (root.TryGetProperty("isError", out var isError) &&
            isError.ValueKind == JsonValueKind.True)
        {
            throw new InvalidOperationException(ExtractText(root));
        }

        var text = ExtractText(root);
        if (!string.IsNullOrWhiteSpace(text))
            return text;

        if (root.TryGetProperty("structuredContent", out var structured))
            return JsonSerializer.Serialize(structured, new JsonSerializerOptions { WriteIndented = true });

        return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string ExtractText(JsonElement root)
    {
        if (!root.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var item in content.EnumerateArray())
        {
            if (item.TryGetProperty("text", out var text) &&
                text.ValueKind == JsonValueKind.String)
            {
                parts.Add(text.GetString() ?? string.Empty);
            }
            else if (item.TryGetProperty("type", out var type) &&
                     type.ValueKind == JsonValueKind.String &&
                     string.Equals(type.GetString(), "image", StringComparison.Ordinal))
            {
                parts.Add("[Получен снимок экрана Studio. В этой версии PIblox показывает текстовые ответы MCP.]");
            }
        }

        return string.Join("\n\n", parts);
    }

    private async Task<string> ReadStderrAsync()
    {
        if (_stderrReader is null)
            return string.Empty;

        try
        {
            var output = await _stderrReader.WaitAsync(TimeSpan.FromSeconds(1));
            return output.Length <= 3000 ? output : output[^3000..];
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task DisconnectAsync()
    {
        var process = _process;
        _process = null;
        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
            {
                process.StandardInput.Close();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await process.WaitForExitAsync(timeout.Token);
            }
        }
        catch
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best-effort cleanup of the local MCP child process.
            }
        }
        finally
        {
            process.Dispose();
            _stderrReader = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _ioLock.Dispose();
    }
}
