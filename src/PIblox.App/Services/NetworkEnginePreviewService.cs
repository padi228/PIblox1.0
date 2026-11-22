using System;

namespace PIblox.App.Services;

/// <summary>
/// UI-preview state machine for PIblox Network.
/// This deliberately does not create a tunnel, alter routes/DNS, or transmit traffic.
/// </summary>
public enum NetworkPreviewState
{
    Disconnected,
    Preparing,
    ConnectedPreview,
    Disconnecting,
    Error
}

public sealed record NetworkPreviewSnapshot(
    NetworkPreviewState State,
    DateTimeOffset UpdatedAt,
    string Message);

public sealed class NetworkEnginePreviewService
{
    private NetworkPreviewState _state = NetworkPreviewState.Disconnected;

    public NetworkPreviewSnapshot Snapshot() => new(
        _state,
        DateTimeOffset.Now,
        Describe(_state));

    public NetworkPreviewSnapshot StartPreview()
    {
        if (_state is NetworkPreviewState.ConnectedPreview or NetworkPreviewState.Preparing)
            return Snapshot();

        _state = NetworkPreviewState.Preparing;
        // Preview-only state transition. No tunnel or OS network settings are touched.
        _state = NetworkPreviewState.ConnectedPreview;
        return Snapshot();
    }

    public NetworkPreviewSnapshot StopPreview()
    {
        if (_state == NetworkPreviewState.Disconnected)
            return Snapshot();

        _state = NetworkPreviewState.Disconnecting;
        _state = NetworkPreviewState.Disconnected;
        return Snapshot();
    }

    private static string Describe(NetworkPreviewState state) => state switch
    {
        NetworkPreviewState.Disconnected => "Preview is stopped. No tunnel is active.",
        NetworkPreviewState.Preparing => "Preparing preview state…",
        NetworkPreviewState.ConnectedPreview => "Preview connected (simulation only; traffic is not routed).",
        NetworkPreviewState.Disconnecting => "Stopping preview…",
        NetworkPreviewState.Error => "Preview error.",
        _ => "Unknown state."
    };
}
