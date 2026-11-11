using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using VRRemoteDesktop.Remote;

namespace VRRemoteDesktop.Networking
{
    /// <summary>
    /// Network-capable remote input provider (Section 15 of Master Requirements).
    /// Serializes and sends mouse, keyboard, and touch events over a live TCP/UDP socket
    /// to an external host server. Fallback to mock mode if not connected.
    /// </summary>
    public class NetworkRemoteInputProvider : MonoBehaviour, IRemoteInputProvider
    {
        [Header("Socket Configuration")]
        [SerializeField] private string _targetHost = "127.0.0.1";
        [SerializeField] private int _targetPort = 8081;
        [SerializeField] private bool _autoConnect = false;

        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private Thread _readThread;
        private volatile bool _isRunning;
        private int _currentLatencyMs = 0;
        private long _packetsSent = 0;

        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;
        public int CurrentLatencyMs => _currentLatencyMs;
        public long PacketsSent => _packetsSent;

        public event Action<int> OnLatencyUpdated;
        public event Action<string> OnTextReceived;

        private void Start()
        {
            if (_autoConnect)
            {
                ConnectToHost(_targetHost, _targetPort);
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void ConnectToHost(string host, int port)
        {
            Disconnect();
            _targetHost = host;
            _targetPort = port;

            try
            {
                _tcpClient = new TcpClient();
                _tcpClient.NoDelay = true;
                _tcpClient.BeginConnect(_targetHost, _targetPort, OnConnectedCallback, null);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkRemoteInputProvider] Connection attempt failed: {ex.Message}");
            }
        }

        private void OnConnectedCallback(IAsyncResult ar)
        {
            try
            {
                if (_tcpClient != null && _tcpClient.Client != null)
                {
                    _tcpClient.EndConnect(ar);
                    _stream = _tcpClient.GetStream();
                    _isRunning = true;
                    _readThread = new Thread(ReadWorker) { IsBackground = true };
                    _readThread.Start();
                    Debug.Log($"[NetworkRemoteInputProvider] Successfully connected to {_targetHost}:{_targetPort}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkRemoteInputProvider] Failed to establish stream: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            _isRunning = false;
            if (_stream != null)
            {
                try { _stream.Close(); } catch { }
                _stream = null;
            }
            if (_tcpClient != null)
            {
                try { _tcpClient.Close(); } catch { }
                _tcpClient = null;
            }
            if (_readThread != null && _readThread.IsAlive)
            {
                try { _readThread.Join(200); } catch { }
                _readThread = null;
            }
        }

        public void SendTextInput(string text)
        {
            SendPayload(new NetworkInputPayload
            {
                Type = NetworkMessageType.TextInput,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Text = text
            });
        }

        public void SendKeyDown(KeyCode key)
        {
            SendPayload(new NetworkInputPayload
            {
                Type = NetworkMessageType.KeyDown,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                KeyCode = (int)key
            });
        }

        public void SendKeyUp(KeyCode key)
        {
            SendPayload(new NetworkInputPayload
            {
                Type = NetworkMessageType.KeyUp,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                KeyCode = (int)key
            });
        }

        public void SendMouseMove(Vector2 delta)
        {
            SendPayload(new NetworkInputPayload
            {
                Type = NetworkMessageType.MouseMove,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                X = delta.x,
                Y = delta.y
            });
        }

        public void SendMouseButton(int button, bool isDown)
        {
            SendPayload(new NetworkInputPayload
            {
                Type = NetworkMessageType.MouseButton,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Button = button,
                KeyCode = isDown ? 1 : 0
            });
        }

        public void SendScroll(Vector2 scrollDelta)
        {
            SendPayload(new NetworkInputPayload
            {
                Type = NetworkMessageType.MouseScroll,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                X = scrollDelta.x,
                Y = scrollDelta.y
            });
        }

        private void SendPayload(NetworkInputPayload payload)
        {
            if (!IsConnected || _stream == null) return;

            try
            {
                string json = JsonUtility.ToJson(payload);
                byte[] data = Encoding.UTF8.GetBytes(json + "\n");
                _stream.Write(data, 0, data.Length);
                _packetsSent++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NetworkRemoteInputProvider] Packet send error: {ex.Message}");
            }
        }

        private void ReadWorker()
        {
            byte[] buffer = new byte[2048];
            StringBuilder sb = new StringBuilder();

            while (_isRunning && _stream != null)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) break;

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    sb.Append(chunk);

                    string content = sb.ToString();
                    int newlineIdx;
                    while ((newlineIdx = content.IndexOf('\n')) >= 0)
                    {
                        string line = content.Substring(0, newlineIdx).Trim();
                        content = content.Substring(newlineIdx + 1);
                        ProcessIncomingMessage(line);
                    }
                    sb.Clear();
                    sb.Append(content);
                }
                catch
                {
                    break;
                }
            }
        }

        private void ProcessIncomingMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            // Echo ping or message dispatch
            if (msg.StartsWith("PONG:"))
            {
                if (long.TryParse(msg.Substring(5), out long sentTime))
                {
                    _currentLatencyMs = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - sentTime);
                    OnLatencyUpdated?.Invoke(_currentLatencyMs);
                }
            }
            else
            {
                OnTextReceived?.Invoke(msg);
            }
        }
    }
}
