using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PIblox.App.Services;

public sealed class UpdateService
{
    private static readonly HttpClient Client = CreateClient();
    private const string LatestReleaseUrl = "https://api.github.com/repos/padi228/PIblox1.0/releases/latest";
    private const string ReleasesPageUrl = "https://github.com/padi228/PIblox1.0/releases";

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await Client.GetAsync(LatestReleaseUrl, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new UpdateCheckResult(false, "Публичных релизов PIblox пока нет.", ReleasesPageUrl);

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            var tag = root.TryGetProperty("tag_name", out var tagElement) ? tagElement.GetString() : null;
            var htmlUrl = root.TryGetProperty("html_url", out var urlElement) ? urlElement.GetString() : ReleasesPageUrl;

            if (string.IsNullOrWhiteSpace(tag))
                return new UpdateCheckResult(false, "GitHub не вернул номер версии релиза.", ReleasesPageUrl);

            var latestText = tag.TrimStart('v', 'V');
            var currentText = typeof(UpdateService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

            if (Version.TryParse(latestText, out var latest) &&
                Version.TryParse(currentText, out var current) &&
                latest > current)
                return new UpdateCheckResult(true, $"Доступна новая версия {tag}.", htmlUrl ?? ReleasesPageUrl);

            return new UpdateCheckResult(false, $"Установлена актуальная версия ({currentText}).", htmlUrl ?? ReleasesPageUrl);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or TaskCanceledException)
        {
            return new UpdateCheckResult(false, "Не удалось проверить обновления. Проверь подключение к интернету.", ReleasesPageUrl);
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PIblox", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}

public sealed record UpdateCheckResult(bool UpdateAvailable, string Message, string ReleasesUrl);
