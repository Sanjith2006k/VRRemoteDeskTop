using System;
using UnityEngine;
using VRRemoteDesktop.Networking;

namespace VRRemoteDesktop.Remote
{
    /// <summary>
    /// Master architectural boundary for Desktop Session Providers (Section 8).
    /// Decouples the local desktop simulation from future authorized remote desktop backends.
    /// </summary>
    public interface IDesktopSessionProvider
    {
        string ProviderName { get; }
        bool IsSimulation { get; }
        RemoteConnectionState State { get; }
        IRemoteScreenProvider ScreenProvider { get; }
        IRemoteInputProvider InputProvider { get; }
        RemoteMetrics CurrentMetrics { get; }

        event Action<RemoteConnectionState> OnStateChanged;
        event Action<RemoteMetrics> OnMetricsUpdated;
        event Action<string> OnError;

        void Connect(string host, int port, string authToken = "");
        void Disconnect();
        void SendInput(NetworkInputPayload payload);
        void SetQualityPreset(string preset);
    }
}
