using System;
using UnityEngine;
using VRRemoteDesktop.Networking;

namespace VRRemoteDesktop.Remote
{
    /// <summary>
    /// Default local Desktop Session Provider.
    /// Powers the in-VR desktop simulation without requiring a second PC, network connection,
    /// or live remote desktop server. Explicitly indicates simulation mode.
    /// </summary>
    public class SimulatedDesktopSessionProvider : MonoBehaviour, IDesktopSessionProvider
    {
        public string ProviderName => "Local Desktop Simulator";
        public bool IsSimulation => true;

        [Header("Simulated Host Config")]
        [SerializeField] private string _simulatedHostName = "DESKTOP-VR-LOCAL [SIMULATED]";
        [SerializeField] private string _simulatedHostIP = "127.0.0.1 (Loopback)";
        [SerializeField] private string _simulatedProtocol = "Internal Render Pipeline";
        [SerializeField] private string _simulatedEncryption = "Local Memory Buffer";
        [SerializeField] private string _qualityPreset = "SIMULATION ULTRA";

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
#pragma warning disable CS0067
        public event Action<string> OnError;
#pragma warning restore CS0067

        private void Awake()
        {
            _screenProvider = GetComponent<MockRemoteScreenProvider>();
            if (_screenProvider == null) _screenProvider = gameObject.AddComponent<MockRemoteScreenProvider>();

            _inputProvider = GetComponent<MockRemoteInputProvider>();
            if (_inputProvider == null) _inputProvider = gameObject.AddComponent<MockRemoteInputProvider>();

            InitSimulationMetrics();
        }

        private void InitSimulationMetrics()
        {
            _metrics = new RemoteMetrics
            {
                HostName = _simulatedHostName,
                HostIP = _simulatedHostIP,
                Protocol = _simulatedProtocol,
                Encryption = _simulatedEncryption,
                QualityPreset = _qualityPreset,
                ResolutionWidth = 1920,
                ResolutionHeight = 1080,
                LatencyMs = 0.8f, // Ultra-fast internal loopback simulation
                Fps = 90.0f,
                BitrateMbps = 0.0f,
                PacketLossPercent = 0.00f
            };
        }

        public void Connect(string host, int port, string authToken = "")
        {
            _state = RemoteConnectionState.Connecting;
            OnStateChanged?.Invoke(_state);

            // Simulation connects quickly
            _state = RemoteConnectionState.Connected;
            OnStateChanged?.Invoke(_state);
            OnMetricsUpdated?.Invoke(_metrics);
            Debug.Log("[SimulatedDesktopSessionProvider] Desktop Simulation session is READY and active.");
        }

        public void Disconnect()
        {
            _state = RemoteConnectionState.Disconnected;
            OnStateChanged?.Invoke(_state);
            Debug.Log("[SimulatedDesktopSessionProvider] Desktop Simulation session disconnected.");
        }

        public void SendInput(NetworkInputPayload payload)
        {
            if (_inputProvider != null)
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
            _qualityPreset = preset;
            _metrics.QualityPreset = preset;
            _screenProvider?.SetQualityPreset(preset);
            OnMetricsUpdated?.Invoke(_metrics);
        }
    }
}
