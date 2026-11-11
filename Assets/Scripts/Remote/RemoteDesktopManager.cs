using System;
using System.Collections;
using UnityEngine;

namespace VRRemoteDesktop.Remote
{
    public enum RemoteConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Error
    }

    [System.Serializable]
    public struct RemoteMetrics
    {
        public float LatencyMs;
        public float Fps;
        public float BitrateMbps;
        public float PacketLossPercent;
        public int ResolutionWidth;
        public int ResolutionHeight;
        public string Protocol;
        public string QualityPreset;
        public string HostName;
        public string HostIP;
        public string Encryption;
    }

    public enum ProviderMode
    {
        Mock,
        NetworkLive
    }

    /// <summary>
    /// Master coordinator for Remote Desktop Networking & Connection Management (Phases 9 & 10).
    /// Manages remote host connection lifecycle, state machine, simulated network jitter/latency,
    /// dynamic quality presets, and modular screen/input provider switching (Mock vs Live Network).
    /// </summary>
    public class RemoteDesktopManager : MonoBehaviour
    {
        public static RemoteDesktopManager Instance { get; private set; }

        [Header("Provider Abstraction")]
        [SerializeField] private ProviderMode _providerMode = ProviderMode.Mock;

        [Header("Host Configuration")]
        [SerializeField] private string _hostName = "DESKTOP-VR-HOST";
        [SerializeField] private string _hostIP = "192.168.1.145";
        [SerializeField] private int _hostPort = 8080;
        [SerializeField] private string _protocol = "WebRTC-H265 Ultra";
        [SerializeField] private string _encryption = "ChaCha20-Poly1305";
        [SerializeField] private string _qualityPreset = "ULTRA";

        [Header("Connection Settings")]
        [SerializeField] private bool _autoConnectOnStart = true;
        [SerializeField] private float _connectDuration = 1.4f;
        [SerializeField] private float _reconnectDuration = 1.8f;

        [Header("Live Metrics State")]
        [SerializeField] private RemoteConnectionState _state = RemoteConnectionState.Disconnected;
        [SerializeField] private float _baseLatency = 18.0f;
        [SerializeField] private float _targetFps = 60.0f;
        [SerializeField] private float _bitrateMbps = 48.5f;

        private IRemoteScreenProvider _activeScreenProvider;
        private IRemoteInputProvider _activeInputProvider;
        private Coroutine _activeRoutine;
        private RemoteMetrics _currentMetrics;
        private float _jitterTimer = 0f;
        private bool _simulateStress = false;

        public ProviderMode ActiveMode => _providerMode;
        public IRemoteScreenProvider ActiveScreenProvider => _activeScreenProvider;
        public IRemoteInputProvider ActiveInputProvider => _activeInputProvider;
        public RemoteConnectionState State => _state;
        public RemoteMetrics Metrics => _currentMetrics;
        public string QualityPreset => _qualityPreset;
        public bool IsConnected => _state == RemoteConnectionState.Connected;

        public event Action<RemoteConnectionState> OnConnectionStateChanged;
        public event Action<RemoteMetrics> OnMetricsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            InitProviders();
            InitMetrics();
        }

        private void InitProviders()
        {
            if (_providerMode == ProviderMode.Mock)
            {
                var mockScreen = GetComponent<MockRemoteScreenProvider>();
                if (mockScreen == null) mockScreen = gameObject.AddComponent<MockRemoteScreenProvider>();
                _activeScreenProvider = mockScreen;

                var mockInput = GetComponent<MockRemoteInputProvider>();
                if (mockInput == null) mockInput = gameObject.AddComponent<MockRemoteInputProvider>();
                _activeInputProvider = mockInput;
            }
            else
            {
                var netScreen = GetComponent<VRRemoteDesktop.Networking.NetworkRemoteScreenProvider>();
                if (netScreen == null) netScreen = gameObject.AddComponent<VRRemoteDesktop.Networking.NetworkRemoteScreenProvider>();
                _activeScreenProvider = netScreen;

                var netInput = GetComponent<VRRemoteDesktop.Networking.NetworkRemoteInputProvider>();
                if (netInput == null) netInput = gameObject.AddComponent<VRRemoteDesktop.Networking.NetworkRemoteInputProvider>();
                _activeInputProvider = netInput;
            }
        }

        public void SwitchProviderMode(ProviderMode newMode)
        {
            if (_providerMode == newMode) return;
            Disconnect();
            _providerMode = newMode;
            InitProviders();
            Connect();
        }

        private void Start()
        {
            if (_autoConnectOnStart && _state == RemoteConnectionState.Disconnected)
            {
                Connect();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void InitMetrics()
        {
            _currentMetrics = new RemoteMetrics
            {
                HostName = _hostName,
                HostIP = _hostIP,
                Protocol = _protocol,
                Encryption = _encryption,
                QualityPreset = _qualityPreset,
                ResolutionWidth = 1920,
                ResolutionHeight = 1080,
                LatencyMs = _baseLatency,
                Fps = _targetFps,
                BitrateMbps = _bitrateMbps,
                PacketLossPercent = 0.01f
            };
        }

        private void Update()
        {
            if (_state != RemoteConnectionState.Connected) return;

            _jitterTimer += Time.deltaTime;
            if (_jitterTimer >= 0.45f)
            {
                _jitterTimer = 0f;
                UpdateLiveJitter();
            }
        }

        private void UpdateLiveJitter()
        {
            float jitterLat = _simulateStress ? UnityEngine.Random.Range(55f, 125f) : UnityEngine.Random.Range(-2.2f, 2.5f);
            float jitterFps = _simulateStress ? UnityEngine.Random.Range(-8f, 2f) : UnityEngine.Random.Range(-0.4f, 0.2f);
            float jitterBit = UnityEngine.Random.Range(-1.5f, 1.8f);

            _currentMetrics.LatencyMs = Mathf.Clamp(_baseLatency + jitterLat, 8f, 300f);
            _currentMetrics.Fps = Mathf.Clamp(_targetFps + jitterFps, 20f, 60.2f);
            _currentMetrics.BitrateMbps = Mathf.Clamp(_bitrateMbps + jitterBit, 5f, 80f);
            _currentMetrics.PacketLossPercent = _simulateStress ? UnityEngine.Random.Range(0.2f, 1.8f) : 0.01f;

            OnMetricsUpdated?.Invoke(_currentMetrics);
        }

        public void Connect()
        {
            if (_state == RemoteConnectionState.Connected || _state == RemoteConnectionState.Connecting) return;

            _activeScreenProvider?.Connect(_hostIP, _hostPort);
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(ConnectRoutine());
        }

        public void Disconnect()
        {
            _activeScreenProvider?.Disconnect();
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            SetState(RemoteConnectionState.Disconnected);

            _currentMetrics.LatencyMs = 0f;
            _currentMetrics.Fps = 0f;
            _currentMetrics.BitrateMbps = 0f;
            OnMetricsUpdated?.Invoke(_currentMetrics);
        }

        public void Reconnect()
        {
            _activeScreenProvider?.Disconnect();
            _activeScreenProvider?.Connect(_hostIP, _hostPort);
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(ReconnectRoutine());
        }

        public void SetQualityPreset(string preset)
        {
            if (string.IsNullOrEmpty(preset)) return;
            _qualityPreset = preset.ToUpper();
            _currentMetrics.QualityPreset = _qualityPreset;
            _activeScreenProvider?.SetQualityPreset(_qualityPreset);

            switch (_qualityPreset)
            {
                case "LOW":
                    _currentMetrics.ResolutionWidth = 1280;
                    _currentMetrics.ResolutionHeight = 720;
                    _targetFps = 30f;
                    _bitrateMbps = 12.0f;
                    _baseLatency = 24.0f;
                    break;

                case "MEDIUM":
                    _currentMetrics.ResolutionWidth = 1600;
                    _currentMetrics.ResolutionHeight = 900;
                    _targetFps = 60f;
                    _bitrateMbps = 25.0f;
                    _baseLatency = 20.0f;
                    break;

                case "HIGH":
                    _currentMetrics.ResolutionWidth = 1920;
                    _currentMetrics.ResolutionHeight = 1080;
                    _targetFps = 60f;
                    _bitrateMbps = 38.0f;
                    _baseLatency = 18.0f;
                    break;

                case "ULTRA":
                default:
                    _currentMetrics.ResolutionWidth = 1920;
                    _currentMetrics.ResolutionHeight = 1080;
                    _targetFps = 60f;
                    _bitrateMbps = 50.0f;
                    _baseLatency = 16.0f;
                    break;
            }

            if (_state == RemoteConnectionState.Connected)
            {
                UpdateLiveJitter();
            }
        }

        public void ToggleStressSimulation()
        {
            _simulateStress = !_simulateStress;
        }

        private IEnumerator ConnectRoutine()
        {
            SetState(RemoteConnectionState.Connecting);

            yield return new WaitForSeconds(_connectDuration * 0.4f);
            // Simulate handshake & certificate validation
            yield return new WaitForSeconds(_connectDuration * 0.6f);

            SetState(RemoteConnectionState.Connected);
            UpdateLiveJitter();
            _activeRoutine = null;
        }

        private IEnumerator ReconnectRoutine()
        {
            SetState(RemoteConnectionState.Reconnecting);

            yield return new WaitForSeconds(_reconnectDuration * 0.5f);
            // Re-negotiate WebRTC channel
            yield return new WaitForSeconds(_reconnectDuration * 0.5f);

            SetState(RemoteConnectionState.Connected);
            UpdateLiveJitter();
            _activeRoutine = null;
        }

        private void SetState(RemoteConnectionState newState)
        {
            _state = newState;
            OnConnectionStateChanged?.Invoke(_state);
        }
    }
}
