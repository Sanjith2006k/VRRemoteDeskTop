using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRRemoteDesktop.UI.Windows
{
    /// <summary>
    /// Interactive Cyber Terminal application supporting commands:
    /// help, status, neofetch, ping, ls, cat, date, clear, notepad, browser, files, settings, keyboard, exit.
    /// </summary>
    public class TerminalApp : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _outputHistoryText;
        [SerializeField] private TMP_InputField _commandInputField;
        [SerializeField] private Button _executeButton;
        [SerializeField] private ScrollRect _scrollRect;

        private StringBuilder _history = new StringBuilder();
        private DesktopWindow _parentWindow;

        public TMP_InputField CommandInputField => _commandInputField;

        private void Awake()
        {
            _parentWindow = GetComponentInParent<DesktopWindow>();
            HookEvents();
        }

        private void Start()
        {
            PrintInitialBanner();
        }

        private void Update()
        {
            // Enter key handling: when input field is focused or terminal window is the active front window
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                bool enterPressed = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;

                if (enterPressed)
                {
                    bool isFieldFocused = _commandInputField != null && _commandInputField.isFocused;
                    bool isTerminalFront = _parentWindow != null && _parentWindow.IsOpen && !_parentWindow.IsMinimized &&
                                           _parentWindow.transform.parent != null &&
                                           _parentWindow.transform.GetSiblingIndex() == _parentWindow.transform.parent.childCount - 1;

                    if (isFieldFocused || isTerminalFront)
                    {
                        SubmitCurrentCommand();
                    }
                }
            }
        }

        public void Setup(TMP_InputField inputField, Button executeBtn, TextMeshProUGUI outputText, ScrollRect scrollRect, DesktopWindow parentWin = null)
        {
            _commandInputField = inputField;
            _executeButton = executeBtn;
            _outputHistoryText = outputText;
            _scrollRect = scrollRect;

            if (_commandInputField != null)
            {
                _commandInputField.onFocusSelectAll = false;
                if (_commandInputField.textComponent != null)
                {
                    _commandInputField.textComponent.fontSize = Mathf.Max(_commandInputField.textComponent.fontSize, 17f);
                }
            }

            if (_outputHistoryText != null)
            {
                _outputHistoryText.fontSize = Mathf.Max(_outputHistoryText.fontSize, 18f);
            }

            if (parentWin != null)
            {
                _parentWindow = parentWin;
            }
            else if (_parentWindow == null)
            {
                _parentWindow = GetComponentInParent<DesktopWindow>();
            }

            if (_parentWindow != null)
            {
                _parentWindow.OnWindowFocused -= HandleWindowFocused;
                _parentWindow.OnWindowFocused += HandleWindowFocused;
            }

            HookEvents();

            if (_history.Length == 0)
            {
                PrintInitialBanner();
            }
            else
            {
                UpdateOutput();
            }
        }

        private void OnDestroy()
        {
            if (_parentWindow != null)
            {
                _parentWindow.OnWindowFocused -= HandleWindowFocused;
            }
        }

        private void HandleWindowFocused(DesktopWindow win)
        {
            FocusInputField();
        }

        public void FocusInputField()
        {
            if (_commandInputField != null)
            {
                _commandInputField.ActivateInputField();
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_commandInputField.gameObject);
                }
                if (Keyboard.VirtualKeyboard.Instance != null && Keyboard.VirtualKeyboard.Instance.IsOpen)
                {
                    Keyboard.VirtualKeyboard.Instance.SetTargetInputField(_commandInputField);
                }
            }
        }

        private void HookEvents()
        {
            if (_executeButton != null)
            {
                _executeButton.onClick.RemoveAllListeners();
                _executeButton.onClick.AddListener(SubmitCurrentCommand);
            }

            if (_commandInputField != null)
            {
                _commandInputField.onSubmit.RemoveAllListeners();
                _commandInputField.onSubmit.AddListener((val) => SubmitCurrentCommand());

                _commandInputField.onSelect.RemoveListener(HandleFieldSelected);
                _commandInputField.onSelect.AddListener(HandleFieldSelected);
            }
        }

        private void HandleFieldSelected(string val)
        {
            if (Keyboard.VirtualKeyboard.Instance != null)
            {
                Keyboard.VirtualKeyboard.Instance.SetTargetInputField(_commandInputField);
            }
        }

        private void PrintInitialBanner()
        {
            _history.Clear();
            _history.AppendLine("<color=#00ffff>=========================================================</color>");
            _history.AppendLine("<color=#ff00ff><b>  VR REMOTE WORKSTATION - VSH TERMINAL v2.4 (x86_64)</b></color>");
            _history.AppendLine("<color=#00ffff>=========================================================</color>");
            _history.AppendLine("<color=#c0d4ec>Type '<color=#00ffff>help</color>' for commands or '<color=#00ffff>notepad</color>' to open text editor.</color>");
            _history.AppendLine("<color=#c0d4ec>Connected to host: <color=#00ff88>DESKTOP-VR-HOST (LAN | 10 Gbps)</color></color>");
            _history.AppendLine("");

            UpdateOutput();
        }

        public void SubmitCurrentCommand()
        {
            if (_commandInputField == null) return;
            string input = _commandInputField.text.Trim();
            _commandInputField.text = "";

            if (string.IsNullOrEmpty(input)) return;

            ExecuteCommand(input);

            _commandInputField.ActivateInputField();
        }

        public void ExecuteCommand(string cmdLine)
        {
            _history.AppendLine($"<color=#00ff88>root@vr-desktop</color>:<color=#00ffff>~</color>$ <color=#ffffff><b>{cmdLine}</b></color>");

            string[] parts = cmdLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1] : "";

            switch (cmd)
            {
                case "help":
                    _history.AppendLine("<color=#ffc040>AVAILABLE SYSTEM COMMANDS:</color>");
                    _history.AppendLine("  <color=#00ffff>status</color>     - Display live remote connection diagnostics & telemetry");
                    _history.AppendLine("  <color=#00ffff>connect</color>    - Initiate WebRTC remote desktop link");
                    _history.AppendLine("  <color=#00ffff>disconnect</color> - Terminate remote desktop session");
                    _history.AppendLine("  <color=#00ffff>reconnect</color>  - Re-negotiate remote stream");
                    _history.AppendLine("  <color=#00ffff>ping</color>       - Ping remote host (DESKTOP-VR-HOST)");
                    _history.AppendLine("  <color=#00ffff>neofetch</color>   - Display workstation hardware & OS overview");
                    _history.AppendLine("  <color=#00ffff>ls</color>         - List directory contents");
                    _history.AppendLine("  <color=#00ffff>cat <file></color> - Display contents of a file");
                    _history.AppendLine("  <color=#00ffff>notepad</color>    - Launch Cyber Notepad text editor");
                    _history.AppendLine("  <color=#00ffff>connection</color> - Launch Remote Connection Manager");
                    _history.AppendLine("  <color=#00ffff>browser</color>    - Launch Quantum Web Browser");
                    _history.AppendLine("  <color=#00ffff>files</color>      - Launch Workspace File Explorer");
                    _history.AppendLine("  <color=#00ffff>monitor</color>    - Launch System Monitor");
                    _history.AppendLine("  <color=#00ffff>keyboard</color>   - Toggle virtual on-screen keyboard");
                    _history.AppendLine("  <color=#00ffff>clear</color>      - Clear terminal screen");
                    _history.AppendLine("  <color=#00ffff>exit</color>       - Close terminal window");
                    break;

                case "status":
                    if (VRRemoteDesktop.Remote.RemoteDesktopManager.Instance != null)
                    {
                        var m = VRRemoteDesktop.Remote.RemoteDesktopManager.Instance.Metrics;
                        var st = VRRemoteDesktop.Remote.RemoteDesktopManager.Instance.State;
                        string stCol = st == VRRemoteDesktop.Remote.RemoteConnectionState.Connected ? "#00ff88" : "#ffaa00";
                        _history.AppendLine($"<color={stCol}>SYSTEM STATUS: {st.ToString().ToUpper()}</color>");
                        _history.AppendLine($"  Host:         {m.HostName} ({m.HostIP})");
                        _history.AppendLine($"  Protocol:     {m.Protocol}");
                        _history.AppendLine($"  Frame Rate:   {m.Fps:0.0} FPS");
                        _history.AppendLine($"  Latency:      {m.LatencyMs:0.1} ms");
                        _history.AppendLine($"  Packet Loss:  {m.PacketLossPercent:0.00}%");
                        _history.AppendLine($"  Bandwidth:    {m.BitrateMbps:0.1} Mbps");
                        _history.AppendLine($"  Encryption:   {m.Encryption}");
                    }
                    else
                    {
                        _history.AppendLine("<color=#00ff88>SYSTEM STATUS: OPTIMAL</color>");
                        _history.AppendLine("  Host:         DESKTOP-VR-HOST (192.168.1.145)");
                        _history.AppendLine("  Protocol:     Ultra-Low-Latency WebRTC / H.265");
                        _history.AppendLine("  Frame Rate:   60.0 FPS");
                        _history.AppendLine("  Latency:      18.2 ms");
                    }
                    break;

                case "neofetch":
                case "sysinfo":
                    _history.AppendLine("<color=#00ffff>         /\\          </color> <color=#ff00ff><b>root@vr-workstation</b></color>");
                    _history.AppendLine("<color=#00ffff>        /  \\         </color> <color=#888888>-------------------</color>");
                    _history.AppendLine("<color=#00ffff>       / /\\ \\        </color> <color=#00ffff>OS:</color> VR-OS 6.6 Cyberpunk Spatial");
                    _history.AppendLine("<color=#00ffff>      / /__\\ \\       </color> <color=#00ffff>Host:</color> Holographic Workspace Rig");
                    _history.AppendLine("<color=#00ffff>     / /    \\ \\      </color> <color=#00ffff>Kernel:</color> 6.1.4-quantum-rt");
                    _history.AppendLine("<color=#00ffff>    /_/      \\_\\     </color> <color=#00ffff>Uptime:</color> 4 hours, 18 mins");
                    _history.AppendLine("<color=#00ffff>                     </color> <color=#00ffff>Resolution:</color> 1920x1080 @ 60Hz");
                    _history.AppendLine("<color=#00ffff>                     </color> <color=#00ffff>CPU:</color> Quantum Octa-Core @ 3.8 GHz");
                    _history.AppendLine("<color=#00ffff>                     </color> <color=#00ffff>GPU:</color> Neural Ray-Core RTX Virtual");
                    _history.AppendLine("<color=#00ffff>                     </color> <color=#00ffff>Memory:</color> 6.2 GB / 16.0 GB (38%)");
                    break;

                case "ping":
                    float livePing = 18.2f;
                    if (VRRemoteDesktop.Remote.RemoteDesktopManager.Instance != null && VRRemoteDesktop.Remote.RemoteDesktopManager.Instance.IsConnected)
                    {
                        livePing = VRRemoteDesktop.Remote.RemoteDesktopManager.Instance.Metrics.LatencyMs;
                    }
                    _history.AppendLine($"PING 192.168.1.145 (DESKTOP-VR-HOST) 56(84) bytes of data.");
                    _history.AppendLine($"64 bytes from 192.168.1.145: icmp_seq=1 ttl=64 time={(livePing - 0.3f):0.1} ms");
                    _history.AppendLine($"64 bytes from 192.168.1.145: icmp_seq=2 ttl=64 time={(livePing + 0.2f):0.1} ms");
                    _history.AppendLine($"64 bytes from 192.168.1.145: icmp_seq=3 ttl=64 time={(livePing - 0.1f):0.1} ms");
                    _history.AppendLine($"64 bytes from 192.168.1.145: icmp_seq=4 ttl=64 time={livePing:0.1} ms");
                    _history.AppendLine($"--- 192.168.1.145 ping statistics ---");
                    _history.AppendLine($"4 packets transmitted, 4 received, 0% packet loss, avg {livePing:0.1} ms");
                    break;

                case "connect":
                    _history.AppendLine("<color=#00ffff>Initiating WebRTC remote desktop connection...</color>");
                    if (VRRemoteDesktop.Remote.RemoteDesktopManager.Instance != null)
                        VRRemoteDesktop.Remote.RemoteDesktopManager.Instance.Connect();
                    _history.AppendLine("<color=#00ff88>[OK] Remote host handshake dispatched.</color>");
                    break;

                case "disconnect":
                    _history.AppendLine("<color=#ffaa00>Closing remote desktop session...</color>");
                    if (VRRemoteDesktop.Remote.RemoteDesktopManager.Instance != null)
                        VRRemoteDesktop.Remote.RemoteDesktopManager.Instance.Disconnect();
                    _history.AppendLine("<color=#888888>[OK] Session terminated.</color>");
                    break;

                case "reconnect":
                    _history.AppendLine("<color=#00ffff>Re-negotiating WebRTC stream...</color>");
                    if (VRRemoteDesktop.Remote.RemoteDesktopManager.Instance != null)
                        VRRemoteDesktop.Remote.RemoteDesktopManager.Instance.Reconnect();
                    _history.AppendLine("<color=#00ff88>[OK] Stream reconnected.</color>");
                    break;

                case "connection":
                case "remotelink":
                    _history.AppendLine("<color=#00ff88>Opening Remote Connection Manager...</color>");
                    DesktopWindowManager.Instance?.OpenWindow("Connection");
                    break;

                case "ls":
                case "dir":
                    _history.AppendLine("<color=#00ffff>notes.txt</color>             <color=#00ffff>quantum_core.sys</color>    <color=#ff00ff>remote_display.hlsl</color>");
                    _history.AppendLine("<color=#00ff88>workstation_config.json</color>    <color=#ffffff>readme_workspace.txt</color>    <color=#ffff00>telemetry_stream.log</color>");
                    break;

                case "cat":
                    if (string.IsNullOrEmpty(arg))
                    {
                        _history.AppendLine("<color=#ff4444>Usage: cat <filename></color>");
                    }
                    else if (arg.Contains("readme"))
                    {
                        _history.AppendLine("<color=#00ffff>=== README_WORKSPACE.TXT ===</color>");
                        _history.AppendLine("Welcome to the Holographic VR Remote Desktop environment.");
                        _history.AppendLine("Keyboard, mouse, and multi-window multitasking are active.");
                    }
                    else if (arg.Contains("notes"))
                    {
                        _history.AppendLine("<color=#00ffff>=== NOTES.TXT ===</color>");
                        _history.AppendLine("VR Spatial Workstation Project notes.");
                        _history.AppendLine("All desktop applications and virtual keyboard operational.");
                    }
                    else
                    {
                        _history.AppendLine($"<color=#ff4444>cat: {arg}: No such file or permission denied.</color>");
                    }
                    break;

                case "date":
                case "time":
                    _history.AppendLine($"<color=#00ffff>Current System Time: {DateTime.Now:dddd, MMMM dd yyyy - HH:mm:ss} UTC</color>");
                    break;

                case "whoami":
                    _history.AppendLine("<color=#00ff88>root (VR Station Operator - Administrator)</color>");
                    break;

                case "echo":
                    string restOfLine = cmdLine.Length > 5 ? cmdLine.Substring(5) : "";
                    _history.AppendLine(restOfLine);
                    break;

                case "notepad":
                case "notes":
                case "editor":
                    _history.AppendLine("<color=#00ff88>Launching Notepad text editor...</color>");
                    DesktopWindowManager.Instance?.OpenWindow("Notepad");
                    break;

                case "browser":
                case "web":
                    _history.AppendLine("<color=#00ff88>Launching Quantum Browser...</color>");
                    DesktopWindowManager.Instance?.OpenWindow("Browser");
                    break;

                case "files":
                case "explorer":
                    _history.AppendLine("<color=#00ff88>Launching Workspace Files...</color>");
                    DesktopWindowManager.Instance?.OpenWindow("Files");
                    break;

                case "monitor":
                case "sysmon":
                    _history.AppendLine("<color=#00ff88>Launching System Monitor...</color>");
                    DesktopWindowManager.Instance?.OpenWindow("SysMonitor");
                    break;

                case "keyboard":
                case "vk":
                    _history.AppendLine("<color=#00ff88>Toggling Virtual Keyboard...</color>");
                    Keyboard.VirtualKeyboard.Instance?.Toggle();
                    break;

                case "clear":
                case "cls":
                    _history.Clear();
                    break;

                case "exit":
                case "quit":
                    if (_parentWindow != null)
                    {
                        _parentWindow.Close();
                    }
                    else
                    {
                        _history.AppendLine("<color=#ff4444>Session logout initiated.</color>");
                    }
                    break;

                default:
                    _history.AppendLine($"<color=#ff4444>vsh: command not found: {cmd}. Type '<color=#00ffff>help</color>' for a list of commands.</color>");
                    break;
            }

            _history.AppendLine("");
            UpdateOutput();
        }

        private void UpdateOutput()
        {
            if (_outputHistoryText != null)
            {
                _outputHistoryText.text = _history.ToString();
            }

            if (_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}
