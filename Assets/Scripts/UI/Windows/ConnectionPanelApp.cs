using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.Remote;

namespace VRRemoteDesktop.UI.Windows
{
    /// <summary>
    /// Interactive Connection Panel application for Phase 9 Remote Desktop.
    /// Provides live connection state visualization, remote host telemetry, quality preset switching,
    /// and connect / disconnect / reconnect controls.
    /// </summary>
    public class ConnectionPanelApp : MonoBehaviour
    {
        [Header("Status Card")]
        [SerializeField] private Image _statusDot;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _hostInfoText;

        [Header("Telemetry Readouts")]
        [SerializeField] private TextMeshProUGUI _latencyText;
        [SerializeField] private TextMeshProUGUI _fpsText;
        [SerializeField] private TextMeshProUGUI _bitrateText;
        [SerializeField] private TextMeshProUGUI _packetLossText;

        [Header("Control Buttons")]
        [SerializeField] private Button _connectBtn;
        [SerializeField] private Button _disconnectBtn;
        [SerializeField] private Button _reconnectBtn;
        [SerializeField] private Button _stressTestBtn;

        [Header("Quality Buttons")]
        [SerializeField] private Button _lowBtn;
        [SerializeField] private Button _medBtn;
        [SerializeField] private Button _highBtn;
        [SerializeField] private Button _ultraBtn;

        private readonly Dictionary<string, Image> _presetButtonImgs = new Dictionary<string, Image>();

        private static readonly Color ColorConnected = new Color(0f, 1f, 0.55f, 1f);
        private static readonly Color ColorConnecting = new Color(1f, 0.75f, 0.1f, 1f);
        private static readonly Color ColorReconnecting = new Color(1f, 0.55f, 0.1f, 1f);
        private static readonly Color ColorDisconnected = new Color(0.6f, 0.65f, 0.75f, 1f);
        private static readonly Color ColorError = new Color(1f, 0.25f, 0.35f, 1f);

        private static readonly Color PresetActiveColor = new Color(0f, 0.65f, 0.85f, 0.95f);
        private static readonly Color PresetInactiveColor = new Color(0.06f, 0.10f, 0.16f, 0.85f);

        private void Start()
        {
            EnsureManager();
            HookEvents();
            RefreshState();
        }

        private void OnDestroy()
        {
            if (RemoteDesktopManager.Instance != null)
            {
                RemoteDesktopManager.Instance.OnConnectionStateChanged -= HandleStateChanged;
                RemoteDesktopManager.Instance.OnMetricsUpdated -= HandleMetricsUpdated;
            }
        }

        public void Setup(
            Image statusDot, TextMeshProUGUI statusText, TextMeshProUGUI hostInfoText,
            TextMeshProUGUI latencyText, TextMeshProUGUI fpsText, TextMeshProUGUI bitrateText, TextMeshProUGUI packetLossText,
            Button connectBtn, Button disconnectBtn, Button reconnectBtn, Button stressBtn,
            Button lowBtn, Button medBtn, Button highBtn, Button ultraBtn)
        {
            _statusDot = statusDot;
            _statusText = statusText;
            _hostInfoText = hostInfoText;
            _latencyText = latencyText;
            _fpsText = fpsText;
            _bitrateText = bitrateText;
            _packetLossText = packetLossText;

            _connectBtn = connectBtn;
            _disconnectBtn = disconnectBtn;
            _reconnectBtn = reconnectBtn;
            _stressTestBtn = stressBtn;

            _lowBtn = lowBtn;
            _medBtn = medBtn;
            _highBtn = highBtn;
            _ultraBtn = ultraBtn;

            _presetButtonImgs["LOW"] = _lowBtn?.GetComponent<Image>();
            _presetButtonImgs["MEDIUM"] = _medBtn?.GetComponent<Image>();
            _presetButtonImgs["HIGH"] = _highBtn?.GetComponent<Image>();
            _presetButtonImgs["ULTRA"] = _ultraBtn?.GetComponent<Image>();

            EnsureManager();
            HookEvents();
            RefreshState();
        }

        private void EnsureManager()
        {
            if (RemoteDesktopManager.Instance == null)
            {
                var mgrObj = new GameObject("RemoteDesktopManager", typeof(RemoteDesktopManager));
                DontDestroyOnLoad(mgrObj);
            }
        }

        private void HookEvents()
        {
            if (RemoteDesktopManager.Instance != null)
            {
                RemoteDesktopManager.Instance.OnConnectionStateChanged -= HandleStateChanged;
                RemoteDesktopManager.Instance.OnConnectionStateChanged += HandleStateChanged;

                RemoteDesktopManager.Instance.OnMetricsUpdated -= HandleMetricsUpdated;
                RemoteDesktopManager.Instance.OnMetricsUpdated += HandleMetricsUpdated;
            }

            if (_connectBtn != null)
            {
                _connectBtn.onClick.RemoveAllListeners();
                _connectBtn.onClick.AddListener(() =>
                {
                    RemoteDesktopManager.Instance?.Connect();
                    HostDesktopStreamManager.Instance?.SetHostStreamActive(true);
                });
            }

            if (_disconnectBtn != null)
            {
                _disconnectBtn.onClick.RemoveAllListeners();
                _disconnectBtn.onClick.AddListener(() =>
                {
                    RemoteDesktopManager.Instance?.Disconnect();
                    HostDesktopStreamManager.Instance?.SetHostStreamActive(false);
                });
            }

            if (_reconnectBtn != null)
            {
                _reconnectBtn.onClick.RemoveAllListeners();
                _reconnectBtn.onClick.AddListener(() => RemoteDesktopManager.Instance?.Reconnect());
            }

            if (_stressTestBtn != null)
            {
                _stressTestBtn.onClick.RemoveAllListeners();
                _stressTestBtn.onClick.AddListener(() => RemoteDesktopManager.Instance?.ToggleStressSimulation());
            }

            if (_lowBtn != null)
            {
                _lowBtn.onClick.RemoveAllListeners();
                _lowBtn.onClick.AddListener(() => SelectPreset("LOW"));
            }

            if (_medBtn != null)
            {
                _medBtn.onClick.RemoveAllListeners();
                _medBtn.onClick.AddListener(() => SelectPreset("MEDIUM"));
            }

            if (_highBtn != null)
            {
                _highBtn.onClick.RemoveAllListeners();
                _highBtn.onClick.AddListener(() => SelectPreset("HIGH"));
            }

            if (_ultraBtn != null)
            {
                _ultraBtn.onClick.RemoveAllListeners();
                _ultraBtn.onClick.AddListener(() => SelectPreset("ULTRA"));
            }
        }

        private void SelectPreset(string preset)
        {
            RemoteDesktopManager.Instance?.SetQualityPreset(preset);
            UpdatePresetHighlights(preset);
        }

        private void UpdatePresetHighlights(string activePreset)
        {
            foreach (var kvp in _presetButtonImgs)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.color = (kvp.Key == activePreset) ? PresetActiveColor : PresetInactiveColor;
                }
            }
        }

        private void HandleStateChanged(RemoteConnectionState state)
        {
            RefreshState();
        }

        private void HandleMetricsUpdated(RemoteMetrics metrics)
        {
            UpdateMetricsUI(metrics);
        }

        private void RefreshState()
        {
            if (RemoteDesktopManager.Instance == null) return;

            var state = RemoteDesktopManager.Instance.State;
            var metrics = RemoteDesktopManager.Instance.Metrics;

            Color statusColor;
            string stateTitle;

            bool isSimulation = RemoteDesktopManager.Instance.ActiveMode == ProviderMode.Mock;

            switch (state)
            {
                case RemoteConnectionState.Connected:
                    statusColor = ColorConnected;
                    stateTitle = isSimulation ? "SIMULATION READY" : "CONNECTED";
                    break;
                case RemoteConnectionState.Connecting:
                    statusColor = ColorConnecting;
                    stateTitle = isSimulation ? "INITIALIZING SIMULATION..." : "CONNECTING...";
                    break;
                case RemoteConnectionState.Reconnecting:
                    statusColor = ColorReconnecting;
                    stateTitle = "RECONNECTING...";
                    break;
                case RemoteConnectionState.Error:
                    statusColor = ColorError;
                    stateTitle = isSimulation ? "SIMULATION ERROR" : "CONNECTION ERROR";
                    break;
                case RemoteConnectionState.Disconnected:
                default:
                    statusColor = ColorDisconnected;
                    stateTitle = "DISCONNECTED";
                    break;
            }

            if (HostDesktopStreamManager.Instance != null && HostDesktopStreamManager.Instance.IsHostStreamActive)
            {
                statusColor = ColorConnected;
                stateTitle = "REAL HOST OS [STREAMING]";
                if (_hostInfoText != null)
                {
                    _hostInfoText.text = $"Host: <b>{System.Environment.MachineName} (Windows Native)</b> | Mode: <b>GDI BitBlt Direct DMA (Phase 4)</b> | Input: <b>Active</b>";
                }
            }
            else if (_hostInfoText != null)
            {
                _hostInfoText.text = $"Host: <b>{metrics.HostName}</b>   |   IP: <b>{metrics.HostIP}:8080</b>   |   Protocol: <b>{metrics.Protocol}</b>   |   Cipher: <b>{metrics.Encryption}</b>";
            }

            if (_statusDot != null) _statusDot.color = statusColor;

            if (_statusText != null)
            {
                _statusText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(statusColor)}>[*]</color> <b>{stateTitle}</b>";
            }

            UpdateMetricsUI(metrics);
            UpdatePresetHighlights(RemoteDesktopManager.Instance.QualityPreset);
        }

        private void UpdateMetricsUI(RemoteMetrics m)
        {
            if (RemoteDesktopManager.Instance == null) return;
            bool connected = RemoteDesktopManager.Instance.IsConnected;

            if (_latencyText != null)
            {
                _latencyText.text = connected ? $"{m.LatencyMs:0.0} ms" : "-- ms";
                _latencyText.color = connected ? (m.LatencyMs < 35f ? ColorConnected : ColorConnecting) : ColorDisconnected;
            }

            if (_fpsText != null)
            {
                _fpsText.text = connected ? $"{m.Fps:0.0} FPS" : "-- FPS";
                _fpsText.color = connected ? Color.cyan : ColorDisconnected;
            }

            if (_bitrateText != null)
            {
                _bitrateText.text = connected ? $"{m.BitrateMbps:0.1} Mbps" : "-- Mbps";
            }

            if (_packetLossText != null)
            {
                _packetLossText.text = connected ? $"{m.PacketLossPercent:0.00}%" : "--%";
                _packetLossText.color = connected ? (m.PacketLossPercent > 0.5f ? ColorError : ColorConnected) : ColorDisconnected;
            }
        }
    }
}
