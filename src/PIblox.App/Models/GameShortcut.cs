namespace PIblox.App.Models;

public sealed class GameShortcut
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset AddedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public override string ToString() => Name;
}
