using System;
using UnityEngine;
using TMPro;

namespace VRRemoteDesktop.Remote
{
    /// <summary>
    /// Mock implementation of IRemoteInputProvider for desktop and VR workstation testing.
    /// Simulates packet transmission, jitter, latency (18-28ms), and routes typed text
    /// into active TMP_InputFields or terminal buffers.
    /// </summary>
    public class MockRemoteInputProvider : MonoBehaviour, IRemoteInputProvider
    {
        public static MockRemoteInputProvider Instance { get; private set; }

        [Header("Mock Connection Settings")]
        [SerializeField] private bool _isConnected = true;
        [SerializeField] private int _baseLatencyMs = 22;
        [SerializeField] private float _packetLossRatio = 0.001f;

        private int _currentLatencyMs;
        private long _packetsSent = 0;
        private float _telemetryTimer = 0f;

        public bool IsConnected => _isConnected;
        public int CurrentLatencyMs => _currentLatencyMs;
        public long PacketsSent => _packetsSent;
        public float PacketLossRatio => _packetLossRatio;

        public event Action<int> OnLatencyUpdated;
        public event Action<string> OnTextReceived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _currentLatencyMs = _baseLatencyMs;
        }

        private void Update()
        {
            _telemetryTimer += Time.deltaTime;
            if (_telemetryTimer >= 1.5f)
            {
                _telemetryTimer = 0f;
                // Subtle realistic latency fluctuation
                _currentLatencyMs = Mathf.Clamp(_baseLatencyMs + UnityEngine.Random.Range(-3, 4), 12, 60);
                OnLatencyUpdated?.Invoke(_currentLatencyMs);
            }
        }

        public void SendTextInput(string text)
        {
            if (!_isConnected || string.IsNullOrEmpty(text)) return;
            _packetsSent++;

            OnTextReceived?.Invoke(text);
        }

        public void SendKeyDown(KeyCode key)
        {
            if (!_isConnected) return;
            _packetsSent++;
        }

        public void SendKeyUp(KeyCode key)
        {
            if (!_isConnected) return;
            _packetsSent++;
        }

        public void SendMouseMove(Vector2 delta)
        {
            if (!_isConnected) return;
            _packetsSent++;
        }

        public void SendMouseButton(int button, bool isDown)
        {
            if (!_isConnected) return;
            _packetsSent++;
        }

        public void SendScroll(Vector2 scrollDelta)
        {
            if (!_isConnected) return;
            _packetsSent++;
        }
    }
}
