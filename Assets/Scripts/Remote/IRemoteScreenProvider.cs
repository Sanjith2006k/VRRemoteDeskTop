using System;
using UnityEngine;

namespace VRRemoteDesktop.Remote
{
    /// <summary>
    /// Contract for remote video/screen stream providers (Section 16 of Master Requirements).
    /// Decouples UI rendering from underlying transport (Mock, WebRTC, TCP/UDP stream).
    /// </summary>
    public interface IRemoteScreenProvider
    {
        bool IsStreaming { get; }
        RemoteConnectionState State { get; }
        Vector2Int Resolution { get; }
        float CurrentFps { get; }
        float CurrentLatencyMs { get; }
        Texture ScreenTexture { get; }

        event Action<RemoteConnectionState> OnStateChanged;
        event Action<Texture> OnFrameReceived;
        event Action<Vector2Int> OnResolutionChanged;
        event Action<string> OnError;

        void Connect(string host, int port, string authToken = "");
        void Disconnect();
        void SetQualityPreset(string preset);
    }
}
