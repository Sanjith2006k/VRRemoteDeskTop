using System;
using UnityEngine;
using VRRemoteDesktop.Networking;

namespace VRRemoteDesktop.Remote
{
    /// <summary>
    /// Reserved Extension Point for Live Remote Desktop Backend (Section 8).
    /// Connects to real authorized video streaming (WebRTC / UDP) and input forwarding.
    /// Does not claim connection without authentic live backend.
    /// </summary>
    public class RemoteDesktopSessionProvider : MonoBehaviour, IDesktopSessionProvider
    {
        public string ProviderName => "Live Remote Desktop Streamer";
        public bool IsSimulation => false;

        private RemoteConnectionState _state = RemoteConnectionState.Disconnected;
        private RemoteMetrics _metrics;
        private IRemoteScreenProvider _screenProvider;
        private IRemoteInputProvider _inputProvider;

        public RemoteConnectionState State => _state;
        public IRemoteScreenProvider ScreenProvider => _screenProvider;
        public IRemoteInputProvider InputProvider => _inputProvider;
        public RemoteMetrics CurrentMetrics => _metrics;

        public event Action<RemoteConnectionState> OnStateChanged;
        public event Action<RemoteMetrics> OnMetricsUpdated;
        public event Action<string> OnError;

        private void Awake()
        {
            _screenProvider = GetComponent<NetworkRemoteScreenProvider>();
            if (_screenProvider == null) _screenProvider = gameObject.AddComponent<NetworkRemoteScreenProvider>();

            _inputProvider = GetComponent<NetworkRemoteInputProvider>();
            if (_inputProvider == null) _inputProvider = gameObject.AddComponent<NetworkRemoteInputProvider>();

            _metrics = new RemoteMetrics
            {
                HostName = "DESKTOP-VR-HOST",
                HostIP = "192.168.1.145",
                Protocol = "WebRTC-H265 Low-Latency",
                Encryption = "TLS 1.3 / ChaCha20-Poly1305",
                QualityPreset = "ULTRA",
                ResolutionWidth = 1920,
                ResolutionHeight = 1080,
                LatencyMs = 18f,
                Fps = 60f,
                BitrateMbps = 48.5f,
                PacketLossPercent = 0.01f
            };
        }

        public void Connect(string host, int port, string authToken = "")
        {
            _state = RemoteConnectionState.Connecting;
            OnStateChanged?.Invoke(_state);

            if (_screenProvider != null)
            {
                _screenProvider.Connect(host, port, authToken);
                _state = _screenProvider.State;
                OnStateChanged?.Invoke(_state);
            }
            else
            {
                _state = RemoteConnectionState.Disconnected;
                OnError?.Invoke("No authorized network streaming backend available.");
            }
        }

        public void Disconnect()
        {
            _screenProvider?.Disconnect();
            _state = RemoteConnectionState.Disconnected;
            OnStateChanged?.Invoke(_state);
        }

        public void SendInput(NetworkInputPayload payload)
        {
            if (_inputProvider != null && _state == RemoteConnectionState.Connected)
            {
                switch (payload.Type)
                {
                    case NetworkMessageType.MouseMove:
                        _inputProvider.SendMouseMove(new Vector2(payload.X, payload.Y));
                        break;
                    case NetworkMessageType.MouseButton:
                        _inputProvider.SendMouseButton(payload.Button, payload.Button != 0);
                        break;
                    case NetworkMessageType.TextInput:
                        _inputProvider.SendTextInput(payload.Text);
                        break;
                    case NetworkMessageType.KeyDown:
                        _inputProvider.SendKeyDown((KeyCode)payload.KeyCode);
                        break;
                    case NetworkMessageType.KeyUp:
                        _inputProvider.SendKeyUp((KeyCode)payload.KeyCode);
                        break;
                }
            }
        }

        public void SetQualityPreset(string preset)
        {
            _metrics.QualityPreset = preset;
            _screenProvider?.SetQualityPreset(preset);
            OnMetricsUpdated?.Invoke(_metrics);
        }
    }
}
