using System;
using System.Collections;
using UnityEngine;

namespace VRRemoteDesktop.Remote
{
    /// <summary>
    /// Mock remote screen provider fulfilling Section 16 & Section 30 of Master Requirements.
    /// Simulates high-performance remote desktop streaming without needing an active remote server.
    /// </summary>
    public class MockRemoteScreenProvider : MonoBehaviour, IRemoteScreenProvider
    {
        [Header("Mock Stream Settings")]
        [SerializeField] private Vector2Int _resolution = new Vector2Int(1920, 1080);
        [SerializeField] private float _simulatedFps = 60.0f;
        [SerializeField] private float _simulatedLatencyMs = 18.0f;

        private RemoteConnectionState _state = RemoteConnectionState.Disconnected;
        private Coroutine _streamRoutine;
        private string _activePreset = "ULTRA";

        public bool IsStreaming => _state == RemoteConnectionState.Connected;
        public RemoteConnectionState State => _state;
        public Vector2Int Resolution => _resolution;
        public float CurrentFps => _simulatedFps;
        public float CurrentLatencyMs => _simulatedLatencyMs;
        public Texture ScreenTexture => null; // Mock provider renders via WorldSpace Canvas / Desktop UI

        public event Action<RemoteConnectionState> OnStateChanged;
#pragma warning disable 0067
        public event Action<Texture> OnFrameReceived;
#pragma warning restore 0067
        public event Action<Vector2Int> OnResolutionChanged;
#pragma warning disable 0067
        public event Action<string> OnError;
#pragma warning restore 0067

        public void Connect(string host, int port, string authToken = "")
        {
            if (_streamRoutine != null) StopCoroutine(_streamRoutine);
            _streamRoutine = StartCoroutine(ConnectRoutine(host, port));
        }

        public void Disconnect()
        {
            if (_streamRoutine != null)
            {
                StopCoroutine(_streamRoutine);
                _streamRoutine = null;
            }
            SetState(RemoteConnectionState.Disconnected);
        }

        public void SetQualityPreset(string preset)
        {
            _activePreset = preset.ToUpper();
            switch (_activePreset)
            {
                case "LOW":
                    _resolution = new Vector2Int(1280, 720);
                    _simulatedFps = 30.0f;
                    _simulatedLatencyMs = 38.0f;
                    break;
                case "MEDIUM":
                    _resolution = new Vector2Int(1600, 900);
                    _simulatedFps = 60.0f;
                    _simulatedLatencyMs = 26.0f;
                    break;
                case "HIGH":
                    _resolution = new Vector2Int(1920, 1080);
                    _simulatedFps = 60.0f;
                    _simulatedLatencyMs = 21.0f;
                    break;
                case "ULTRA":
                default:
                    _resolution = new Vector2Int(1920, 1080);
                    _simulatedFps = 60.0f;
                    _simulatedLatencyMs = 18.0f;
                    break;
            }

            OnResolutionChanged?.Invoke(_resolution);
        }

        private IEnumerator ConnectRoutine(string host, int port)
        {
            SetState(RemoteConnectionState.Connecting);
            yield return new WaitForSeconds(1.2f);

            SetState(RemoteConnectionState.Connected);
            SetQualityPreset(_activePreset);
        }

        private void SetState(RemoteConnectionState newState)
        {
            _state = newState;
            OnStateChanged?.Invoke(_state);
        }
    }
}
