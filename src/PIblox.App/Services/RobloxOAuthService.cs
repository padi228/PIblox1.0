using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PIblox.App.Services;

public sealed record RobloxProfile(string UserId, string Username, string DisplayName, string ProfileUrl, string? AvatarUrl);

public sealed class RobloxOAuthService
{
    private const int CallbackPort = 37127;
    private const string RedirectUri = "http://localhost:37127/oauth/callback/";
    private const string TokenEndpoint = "https://apis.roblox.com/oauth/v1/token";
    private const string UserInfoEndpoint = "https://apis.roblox.com/oauth/v1/userinfo";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<RobloxProfile> ConnectAsync(string clientId)
    {
        ValidateClientId(clientId);
        using var ipv4Listener = new TcpListener(IPAddress.Loopback, CallbackPort);
        using var ipv6Listener = new TcpListener(IPAddress.IPv6Loopback, CallbackPort);
        var ipv6Listening = false;
        try
        {
            ipv4Listener.Start(1);
            try
            {
                ipv6Listener.Server.DualMode = false;
                ipv6Listener.Start(1);
                ipv6Listening = true;
            }
            catch (SocketException)
            {
                // Some Windows installations have IPv6 disabled; the IPv4 loopback listener remains available.
            }
        }
        catch (SocketException ex)
        {
            throw new InvalidOperationException($"Не удалось открыть локальный OAuth-порт {CallbackPort}. Закрой другое приложение, использующее этот порт, и повтори попытку.", ex);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = RedirectUri,
            ["scope"] = "openid profile",
            ["response_type"] = "code",
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state
        };
        var authorizationUrl = "https://apis.roblox.com/oauth/v1/authorize?" +
            string.Join("&", query.Select(item => Uri.EscapeDataString(item.Key) + "=" + Uri.EscapeDataString(item.Value)));

        try { Process.Start(new ProcessStartInfo(authorizationUrl) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException("Не удалось открыть браузер для входа на официальный сайт Roblox.", ex);
        }

        var code = await ReceiveAuthorizationCodeAsync(ipv4Listener, ipv6Listener, ipv6Listening, state, timeout.Token);
        var tokens = await ExchangeCodeAsync(clientId, code, verifier, timeout.Token);
        RobloxTokenStore.Save(tokens);
        return await GetUserInfoAsync(tokens.AccessToken, timeout.Token);
    }

    public async Task<RobloxProfile?> GetSavedProfileAsync(string clientId)
    {
        ValidateClientId(clientId);
        var tokens = RobloxTokenStore.Load();
        if (tokens is null) return null;
        if (tokens.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddMinutes(1))
        {
            if (string.IsNullOrWhiteSpace(tokens.RefreshToken)) return null;
            tokens = await RefreshTokensAsync(clientId, tokens);
            RobloxTokenStore.Save(tokens);
        }
        return await GetUserInfoAsync(tokens.AccessToken, CancellationToken.None);
    }

    public void ClearSavedConnection() => RobloxTokenStore.Delete();

    private static async Task<TcpClient> AcceptLoopbackClientAsync(
        TcpListener ipv4Listener, TcpListener ipv6Listener, bool ipv6Listening, CancellationToken ct)
    {
        using var acceptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var ipv4Task = ipv4Listener.AcceptTcpClientAsync(acceptCts.Token).AsTask();
        if (!ipv6Listening)
            return await ipv4Task;

        var ipv6Task = ipv6Listener.AcceptTcpClientAsync(acceptCts.Token).AsTask();
        var winner = await Task.WhenAny(ipv4Task, ipv6Task);
        var accepted = await winner;
        acceptCts.Cancel();
        try { await Task.WhenAll(ipv4Task, ipv6Task); }
        catch (OperationCanceledException) { }
        return accepted;
    }

    private static async Task<string> ReceiveAuthorizationCodeAsync(
        TcpListener ipv4Listener, TcpListener ipv6Listener, bool ipv6Listening, string expectedState, CancellationToken ct)
    {
        using var client = await AcceptLoopbackClientAsync(ipv4Listener, ipv6Listener, ipv6Listening, ct);
        if (client.Client.RemoteEndPoint is not IPEndPoint remote || !IPAddress.IsLoopback(remote.Address))
            throw new InvalidOperationException("OAuth callback пришёл не с локального компьютера.");

        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync(ct);
        string? header;
        do { header = await reader.ReadLineAsync(ct); } while (!string.IsNullOrEmpty(header));
        if (string.IsNullOrWhiteSpace(requestLine))
            throw new InvalidOperationException("Roblox вернул пустой OAuth callback.");

        var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !parts[0].Equals("GET", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Получен некорректный OAuth callback.");

        var callbackUri = new Uri("http://localhost:" + CallbackPort + parts[1]);
        if (!callbackUri.AbsolutePath.Equals("/oauth/callback/", StringComparison.Ordinal))
            throw new InvalidOperationException("Путь OAuth callback не совпадает с ожидаемым.");

        var values = ParseQuery(callbackUri.Query);
        if (!values.TryGetValue("state", out var returnedState) ||
            returnedState.Length != expectedState.Length ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(returnedState), Encoding.UTF8.GetBytes(expectedState)))
        {
            await SendCallbackPageAsync(stream, false, ct);
            throw new InvalidOperationException("Проверка OAuth state не прошла. Повтори подключение.");
        }

        if (values.TryGetValue("error", out var error))
        {
            await SendCallbackPageAsync(stream, false, ct);
            var details = values.TryGetValue("error_description", out var description) ? description : "Roblox отменил авторизацию.";
            throw new InvalidOperationException($"Roblox не подтвердил подключение ({error}): {details}");
        }
        if (!values.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
        {
            await SendCallbackPageAsync(stream, false, ct);
            throw new InvalidOperationException("Roblox не вернул код авторизации.");
        }
        await SendCallbackPageAsync(stream, true, ct);
        return code;
    }

    private static async Task SendCallbackPageAsync(NetworkStream stream, bool success, CancellationToken ct)
    {
        var message = success ? "Roblox подтвердил вход. Можно вернуться в PIblox." : "Не удалось завершить вход. Вернись в PIblox и попробуй снова.";
        var html = "<!doctype html><html lang=\"ru\"><meta charset=\"utf-8\"><title>PIblox</title><body style=\"font-family:Segoe UI,Arial;background:#101126;color:#f4f5ff;padding:48px\"><h2>" +
            (success ? "Аккаунт подтверждён" : "Подключение не завершено") + "</h2><p>" + WebUtility.HtmlEncode(message) + "</p></body></html>";
        var body = Encoding.UTF8.GetBytes(html);
        var headers = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headers, ct);
        await stream.WriteAsync(body, ct);
    }

    private static async Task<RobloxOAuthTokens> ExchangeCodeAsync(string clientId, string code, string verifier, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code", ["client_id"] = clientId, ["redirect_uri"] = RedirectUri,
            ["code"] = code, ["code_verifier"] = verifier
        });
        using var response = await Http.PostAsync(TokenEndpoint, content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response.StatusCode, json, "обмен кода авторизации");
        return ParseTokens(json, null);
    }

    private static async Task<RobloxOAuthTokens> RefreshTokensAsync(string clientId, RobloxOAuthTokens current)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token", ["client_id"] = clientId, ["refresh_token"] = current.RefreshToken!
        });
        using var response = await Http.PostAsync(TokenEndpoint, content);
        var json = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response.StatusCode, json, "обновление сессии Roblox");
        return ParseTokens(json, current.RefreshToken);
    }

    private static RobloxOAuthTokens ParseTokens(string json, string? previousRefreshToken)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var accessToken = ReadString(root, "access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new InvalidOperationException("Roblox не вернул access token.");
        var refreshToken = ReadString(root, "refresh_token") ?? previousRefreshToken;
        var expiresIn = root.TryGetProperty("expires_in", out var expiry) && expiry.TryGetInt32(out var seconds) ? Math.Clamp(seconds, 60, 86400) : 900;
        return new RobloxOAuthTokens(accessToken, refreshToken, DateTimeOffset.UtcNow.AddSeconds(expiresIn));
    }

    private static async Task<RobloxProfile> GetUserInfoAsync(string accessToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await Http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response.StatusCode, json, "получение профиля Roblox");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var userId = ReadString(root, "sub");
        if (string.IsNullOrWhiteSpace(userId) || !userId.All(char.IsDigit))
            throw new InvalidOperationException("Roblox вернул профиль без корректного ID пользователя.");

        var username = ReadString(root, "preferred_username") ?? ReadString(root, "name") ?? userId;
        var displayName = ReadString(root, "nickname") ?? ReadString(root, "name") ?? username;
        var profileUrl = ReadString(root, "profile");
        if (!Uri.TryCreate(profileUrl, UriKind.Absolute, out var profileUri) ||
            profileUri.Scheme != Uri.UriSchemeHttps ||
            !profileUri.Host.Equals("www.roblox.com", StringComparison.OrdinalIgnoreCase))
            profileUrl = $"https://www.roblox.com/users/{userId}/profile";

        var avatarUrl = ReadString(root, "picture");
        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var avatarUri) || avatarUri.Scheme != Uri.UriSchemeHttps)
            avatarUrl = null;

        return new RobloxProfile(userId, username, displayName, profileUrl, avatarUrl);
    }

    private static void EnsureSuccess(HttpStatusCode statusCode, string json, string operation)
    {
        if ((int)statusCode is >= 200 and <= 299) return;
        var reason = "Roblox API вернул ошибку.";
        try
        {
            using var document = JsonDocument.Parse(json);
            reason = ReadString(document.RootElement, "error_description") ?? ReadString(document.RootElement, "message") ?? ReadString(document.RootElement, "error") ?? reason;
        }
        catch (JsonException) { }
        throw new InvalidOperationException($"Не удалось выполнить {operation}: {reason} (HTTP {(int)statusCode}).");
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = item.Split('=', 2);
            var key = Uri.UnescapeDataString(pair[0].Replace("+", " ", StringComparison.Ordinal));
            var value = pair.Length > 1 ? Uri.UnescapeDataString(pair[1].Replace("+", " ", StringComparison.Ordinal)) : string.Empty;
            values[key] = value;
        }
        return values;
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static void ValidateClientId(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length is < 8 or > 40 || !clientId.All(char.IsDigit))
            throw new ArgumentException("Roblox OAuth Client ID должен состоять из цифр. Проверь его в настройках PIblox.", nameof(clientId));
    }
}