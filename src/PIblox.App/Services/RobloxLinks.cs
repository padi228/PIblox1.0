namespace PIblox.App.Services;

public static class RobloxLinks
{
    public static Uri ResolveExperienceInput(string input)
    {
        var value = input.Trim();

        if (Uri.TryCreate(value, UriKind.Absolute, out var suppliedUri))
        {
            var host = suppliedUri.Host.ToLowerInvariant();
            if (suppliedUri.Scheme == Uri.UriSchemeHttps &&
                (host == "roblox.com" || host.EndsWith(".roblox.com", StringComparison.Ordinal)))
                return suppliedUri;

            throw new ArgumentException("For safety, PIblox opens only HTTPS links on roblox.com.");
        }

        if (long.TryParse(value, out var placeId) && placeId > 0)
            return new Uri($"https://www.roblox.com/games/{placeId}");

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Enter a game name, game ID, or Roblox link.");

        return new Uri("https://www.roblox.com/search/experiences?keyword=" + Uri.EscapeDataString(value));
    }

    public static Uri BuildSearchUri(string query) =>
        new("https://www.roblox.com/search/experiences?keyword=" + Uri.EscapeDataString(query.Trim()));
}
