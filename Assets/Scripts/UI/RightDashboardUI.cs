using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.UI
{
    public enum LogCategory
    {
        All,
        Network,
        Security,
        System
    }

    /// <summary>
    /// Network Operations Center (NOC), real-time quantum audio visualizer,
    /// interactive media controller, and cyber room lighting automation dashboard
    /// rendered on the Right Auxiliary Display (R-01).
    /// Features large high-contrast fonts, interactive filterable logs,
    /// animated equalizer spectrum bars, and physical room theme triggers.
    /// </summary>
    public class RightDashboardUI : MonoBehaviour
    {
        public static RightDashboardUI Instance { get; private set; }

        private struct LogEntry
        {
            public string Timestamp;
            public LogCategory Category;
            public string Tag;
            public string Message;
            public string ColorHex;
        }

        private List<LogEntry> _allLogs = new List<LogEntry>();
        private LogCategory _activeFilter = LogCategory.All;

        // Equalizer references
        private Image[] _spectrumBars = new Image[16];
        private float[] _barHeights = new float[16];
        private float[] _targetBarHeights = new float[16];

        // Media references
        private TextMeshProUGUI _trackTitleText;
        private Image _volumeFill;
        private TextMeshProUGUI _volumeText;
        private float _currentVolume = 0.8f;
        private bool _isPlaying = true;
        private int _trackIndex = 0;
        private readonly string[] _trackList = new string[]
        {
            "~ CYBER SYNTHWAVE DREAMS (48 kHz Lossless)",
            "~ NEON ALLEY 2088 - SPATIAL AMBIENCE",
            "~ ORBITAL LOW-FI DATA STREAM",
            "~ QUANTUM MATRIX CHILLOUT"
        };

        // Room Automation references
        private TextMeshProUGUI _themeStatusText;
        private TextMeshProUGUI _shieldStatusText;
        private bool _shieldActive = false;
        private Button _btnScreenShare;
        private TextMeshProUGUI _screenShareText;
        private Image _screenShareImg;
        private bool _isScreenSharing = false;

        // Log UI
        private TextMeshProUGUI _logStreamText;
        private TextMeshProUGUI _pingResultText;
        private TextMeshProUGUI _clockText;

        // Filter button images
        private Button _btnAll;
        private Button _btnNet;
        private Button _btnSec;
        private Button _btnSys;

        private float _logTimer = 0f;
        private float _spectrumTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SeedInitialLogs();
            BuildDashboardUI();
        }

        private void SeedInitialLogs()
        {
            AddLog(LogCategory.Network, "[NET]", "Virtual desktop spatial link established. Stream active.", "#00ff88");
            AddLog(LogCategory.Security, "[SEC]", "ChaCha20-Poly1305 hardware cipher handshake verified.", "#cc66ff");
            AddLog(LogCategory.System, "[SYS]", "Quantum spatial audio bus bound at 48 kHz / 24-bit.", "#00ffff");
            AddLog(LogCategory.Network, "[NET]", "WebRTC H.265 Ultra calibrated: 0.00% packet loss.", "#00ff88");
            AddLog(LogCategory.Security, "[SEC]", "Observation boundary secure. Defense shield standing by.", "#ffaa00");
            AddLog(LogCategory.System, "[SYS]", "Auxiliary screens L-01 and R-01 synchronized with master.", "#00ffff");
        }

        public void BuildDashboardUI()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Canvas size: 1920 x 1080
            // 1. Top Header Banner
            BuildHeaderBanner();

            // 2. Card 1: Command Operations Stream & Logs (Left column: x 30 to 1000, y 50 to 980)
            BuildLogFeedCard();

            // 3. Card 2: Quantum Audio Visualizer & Media Player (Top Right: x 1040 to 1890, y 520 to 980)
            BuildAudioVisualizerCard();

            // 4. Card 3: Cyber Room & Environment Controls (Bottom Right: x 1040 to 1890, y 50 to 480)
            BuildRoomAutomationCard();

            UpdateFilterButtonStyles();
            RefreshLogDisplay();
        }

        private void BuildHeaderBanner()
        {
            GameObject bannerObj = CreateUIObject("HeaderBanner", transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                 new Vector2(0f, -90f), new Vector2(0f, 0f), new Color(0.08f, 0.04f, 0.16f, 0.96f));

            // Violet top accent line
            CreateUIObject("TopAccent", bannerObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                           new Vector2(0f, -4f), new Vector2(0f, 0f), new Color(0.8f, 0.3f, 1f, 1f));

            // Title
            CreateTextObject("Title", bannerObj.transform, new Vector2(0f, 0f), new Vector2(0.7f, 1f),
                             new Vector2(32f, 0f), new Vector2(0f, 0f),
                             "<color=#cc66ff>NETWORK OPERATIONS CENTER & ACTIVITY FEED</color>  |  <color=#ffffff>AUXILIARY DISPLAY R-01</color>",
                             32, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            // Host & Stream Details
            GameObject statusObj = CreateTextObject("StatusClock", bannerObj.transform, new Vector2(0.6f, 0f), new Vector2(1f, 1f),
                                                    new Vector2(0f, 0f), new Vector2(-32f, 0f),
                                                    "10 GbE OPTICAL  |  ChaCha20-Poly1305  |  4K 60FPS",
                                                    22, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.85f, 0.75f, 1f));
            _clockText = statusObj.GetComponent<TextMeshProUGUI>();
        }

        private void BuildLogFeedCard()
        {
            GameObject card = CreateCardPanel("Card_Logs", transform, new Vector2(0.02f, 0.05f), new Vector2(0.53f, 0.90f),
                                             "LIVE COMMAND OPERATIONS STREAM", new Color(0.8f, 0.3f, 1f));

            // Filter Tabs Row
            _btnAll = CreateButton("Btn_All", card.transform, new Vector2(0.03f, 0.88f), new Vector2(0.16f, 0.95f),
                                   "ALL", new Color(0.3f, 0.15f, 0.5f), () => SetFilter(LogCategory.All));

            _btnNet = CreateButton("Btn_Net", card.transform, new Vector2(0.18f, 0.88f), new Vector2(0.31f, 0.95f),
                                   "NET", new Color(0.1f, 0.25f, 0.4f), () => SetFilter(LogCategory.Network));

            _btnSec = CreateButton("Btn_Sec", card.transform, new Vector2(0.33f, 0.88f), new Vector2(0.46f, 0.95f),
                                   "SEC", new Color(0.1f, 0.25f, 0.4f), () => SetFilter(LogCategory.Security));

            _btnSys = CreateButton("Btn_Sys", card.transform, new Vector2(0.48f, 0.88f), new Vector2(0.61f, 0.95f),
                                   "SYS", new Color(0.1f, 0.25f, 0.4f), () => SetFilter(LogCategory.System));

            // Action Buttons (Right side of tab row)
            CreateButton("Btn_Ping", card.transform, new Vector2(0.65f, 0.88f), new Vector2(0.80f, 0.95f),
                         "[NET] PING HOST", new Color(0.05f, 0.4f, 0.3f), OnPingHostClicked);

            CreateButton("Btn_Clear", card.transform, new Vector2(0.82f, 0.88f), new Vector2(0.97f, 0.95f),
                         "[CLEAR] CLEAR", new Color(0.35f, 0.1f, 0.15f), OnClearLogsClicked);

            // Ping Result Bar
            GameObject pingObj = CreateTextObject("PingResult", card.transform, new Vector2(0.03f, 0.82f), new Vector2(0.97f, 0.87f),
                                                  Vector2.zero, Vector2.zero,
                                                  "<color=#00ff88>LINK LATENCY: 3.4 ms  |  JITTER: 0.1 ms  |  PACKET LOSS: 0.00% [OPTIMAL]</color>",
                                                  20, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);
            _pingResultText = pingObj.GetComponent<TextMeshProUGUI>();

            // Log Console Area
            GameObject logArea = CreateUIObject("LogArea", card.transform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.80f),
                                                Vector2.zero, Vector2.zero, new Color(0.025f, 0.04f, 0.08f, 0.98f));

            GameObject logTextObj = CreateTextObject("LogText", logArea.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                     new Vector2(20f, 16f), new Vector2(-20f, -16f),
                                                     "", 20, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);
            _logStreamText = logTextObj.GetComponent<TextMeshProUGUI>();
            _logStreamText.lineSpacing = 16f;
        }

        private void BuildAudioVisualizerCard()
        {
            GameObject card = CreateCardPanel("Card_Audio", transform, new Vector2(0.55f, 0.52f), new Vector2(0.98f, 0.90f),
                                             "QUANTUM AUDIO SPECTRUM & MEDIA", new Color(0f, 1f, 0.9f));

            // Equalizer Bars Container (16 dynamic vertical bars)
            GameObject eqContainer = new GameObject("EqBars", typeof(RectTransform));
            eqContainer.transform.SetParent(card.transform, false);
            RectTransform ert = eqContainer.GetComponent<RectTransform>();
            ert.anchorMin = new Vector2(0.03f, 0.54f);
            ert.anchorMax = new Vector2(0.97f, 0.88f);
            ert.offsetMin = Vector2.zero;
            ert.offsetMax = Vector2.zero;

            float barWidth = 1f / 16f;
            for (int i = 0; i < 16; i++)
            {
                float xMin = i * barWidth + 0.01f;
                float xMax = (i + 1) * barWidth - 0.01f;

                GameObject bg = CreateUIObject($"BarBg_{i}", eqContainer.transform, new Vector2(xMin, 0f), new Vector2(xMax, 1f),
                                               Vector2.zero, Vector2.zero, new Color(0.08f, 0.12f, 0.22f, 1f));

                GameObject fill = CreateUIObject($"BarFill_{i}", bg.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                 Vector2.zero, Vector2.zero, Color.Lerp(new Color(0.8f, 0.2f, 1f), new Color(0f, 1f, 1f), i / 15f));
                Image img = fill.GetComponent<Image>();
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Vertical;
                img.fillAmount = 0.5f;

                _spectrumBars[i] = img;
                _barHeights[i] = 0.5f;
                _targetBarHeights[i] = 0.5f;
            }

            // Now Playing Title
            GameObject trackObj = CreateTextObject("TrackTitle", card.transform, new Vector2(0.03f, 0.40f), new Vector2(0.97f, 0.52f),
                                                   Vector2.zero, Vector2.zero,
                                                   _trackList[_trackIndex], 24, FontStyles.Bold,
                                                   TextAlignmentOptions.MidlineLeft, new Color(0f, 1f, 0.9f));
            _trackTitleText = trackObj.GetComponent<TextMeshProUGUI>();

            // Media Controls Row: Play/Pause, Next, Vol-, Vol+, Mute
            CreateButton("Btn_Play", card.transform, new Vector2(0.03f, 0.22f), new Vector2(0.24f, 0.36f),
                         "[> / ||] PLAY/PAUSE", new Color(0.1f, 0.35f, 0.5f), OnTogglePlayPause);

            CreateButton("Btn_Next", card.transform, new Vector2(0.26f, 0.22f), new Vector2(0.47f, 0.36f),
                         "[>>] NEXT TRACK", new Color(0.2f, 0.15f, 0.45f), OnNextTrackClicked);

            CreateButton("Btn_VolDown", card.transform, new Vector2(0.49f, 0.22f), new Vector2(0.62f, 0.36f),
                         "[-] -", new Color(0.1f, 0.2f, 0.35f), () => AdjustVolume(-0.1f));

            CreateButton("Btn_VolUp", card.transform, new Vector2(0.64f, 0.22f), new Vector2(0.77f, 0.36f),
                         "[+] +", new Color(0.1f, 0.2f, 0.35f), () => AdjustVolume(0.1f));

            CreateButton("Btn_Mute", card.transform, new Vector2(0.79f, 0.22f), new Vector2(0.97f, 0.36f),
                         "[MUTE] MUTE", new Color(0.35f, 0.12f, 0.15f), OnMuteClicked);

            // Volume Bar
            GameObject volBg = CreateUIObject("VolBg", card.transform, new Vector2(0.03f, 0.08f), new Vector2(0.75f, 0.16f),
                                              Vector2.zero, Vector2.zero, new Color(0.08f, 0.12f, 0.22f, 1f));

            GameObject volFill = CreateUIObject("VolFill", volBg.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                Vector2.zero, Vector2.zero, new Color(0f, 0.9f, 1f));
            _volumeFill = volFill.GetComponent<Image>();
            _volumeFill.type = Image.Type.Filled;
            _volumeFill.fillMethod = Image.FillMethod.Horizontal;
            _volumeFill.fillAmount = _currentVolume;

            GameObject volTextObj = CreateTextObject("VolText", card.transform, new Vector2(0.78f, 0.08f), new Vector2(0.97f, 0.16f),
                                                     Vector2.zero, Vector2.zero,
                                                     "VOL: 80%", 20, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);
            _volumeText = volTextObj.GetComponent<TextMeshProUGUI>();
        }

        private void BuildRoomAutomationCard()
        {
            GameObject card = CreateCardPanel("Card_Room", transform, new Vector2(0.55f, 0.05f), new Vector2(0.98f, 0.49f),
                                             "CYBER ROOM & LIGHTING AUTOMATION", new Color(1f, 0.4f, 0.8f));

            CreateTextObject("Sub", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(24f, -75f), new Vector2(-24f, -48f),
                             "PHYSICAL ENVIRONMENT & OBSERVATION ROOM CONTROLS:", 20, FontStyles.Bold,
                             TextAlignmentOptions.MidlineLeft, new Color(0.85f, 0.9f, 1f));

            // Row 1: Theme Cycle & Overhead Light Toggle (Y: 0.70 to 0.84)
            CreateButton("Btn_Theme", card.transform, new Vector2(0.03f, 0.70f), new Vector2(0.48f, 0.84f),
                         "[THEME] CYCLE ROOM THEME", new Color(0.45f, 0.1f, 0.6f), OnCycleThemeClicked);

            CreateButton("Btn_Lights", card.transform, new Vector2(0.52f, 0.70f), new Vector2(0.97f, 0.84f),
                         "[LIGHTS] AMBIENT LIGHTS", new Color(0.1f, 0.35f, 0.55f), OnToggleOverheadLights);

            // Row 2: Shield Lockdown & Chime (Y: 0.53 to 0.67)
            CreateButton("Btn_Shield", card.transform, new Vector2(0.03f, 0.53f), new Vector2(0.48f, 0.67f),
                         "[SEC] SHIELD LOCKDOWN", new Color(0.55f, 0.2f, 0.05f), OnToggleShieldLockdown);

            CreateButton("Btn_Chime", card.transform, new Vector2(0.52f, 0.53f), new Vector2(0.97f, 0.67f),
                         "[!] PLAY SYSTEM CHIME", new Color(0.1f, 0.4f, 0.3f), OnQuickChimeClicked);

            // Row 3: Prominent SCREEN SHARE TO IMAX Button (Y: 0.32 to 0.49)
            _btnScreenShare = CreateButton("Btn_ScreenShare", card.transform, new Vector2(0.03f, 0.32f), new Vector2(0.97f, 0.49f),
                                           "<b>[FEED] SHARE MIDDLE MONITOR TO IMAX SCREEN</b>",
                                           new Color(0.12f, 0.28f, 0.50f), OnToggleScreenShareClicked);
            _screenShareImg = _btnScreenShare.GetComponent<Image>();
            _screenShareText = _btnScreenShare.GetComponentInChildren<TextMeshProUGUI>();

            // Row 4: Theme Status Readout
            GameObject themeObj = CreateTextObject("ThemeStatus", card.transform, new Vector2(0.03f, 0.16f), new Vector2(0.97f, 0.28f),
                                                   Vector2.zero, Vector2.zero,
                                                   "ACTIVE ROOM THEME: <color=#00ffff>CYBERPUNK NEON</color>  |  IMAX: <color=#80c8ff>STANDBY</color>", 21, FontStyles.Bold,
                                                   TextAlignmentOptions.MidlineLeft, Color.white);
            _themeStatusText = themeObj.GetComponent<TextMeshProUGUI>();

            // Row 5: Shield / Host Status
            GameObject shieldObj = CreateTextObject("ShieldStatus", card.transform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.15f),
                                                    Vector2.zero, Vector2.zero,
                                                    "STATUS: <color=#00ff88>NORMAL OBSERVATION</color>  |  HOST: DESKTOP-VR-HOST (ONLINE)", 20, FontStyles.Bold,
                                                    TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.9f, 1f));
            _shieldStatusText = shieldObj.GetComponent<TextMeshProUGUI>();

            UpdateScreenShareButtonVisuals();
        }

        private void Update()
        {
            // 1. Clock update
            if (_clockText != null)
            {
                _clockText.text = $"10 GbE OPTICAL  |  ChaCha20-Poly1305  |  {System.DateTime.Now:HH:mm:ss}";
            }

            // 2. Animate Quantum Audio Equalizer
            _spectrumTimer += Time.deltaTime;
            if (_spectrumTimer >= 0.06f)
            {
                _spectrumTimer = 0f;
                UpdateEqualizerSpectrum();
            }

            // 3. Periodic Log Feed
            _logTimer += Time.deltaTime;
            if (_logTimer >= 4.0f)
            {
                _logTimer = 0f;
                AddPeriodicLog();
            }
        }

        private void UpdateEqualizerSpectrum()
        {
            float t = Time.time;
            for (int i = 0; i < 16; i++)
            {
                if (_isPlaying)
                {
                    float noise = Mathf.PerlinNoise(i * 0.45f, t * 4f);
                    float beat = Mathf.Sin(t * 8f + i * 0.3f) * 0.25f;
                    _targetBarHeights[i] = Mathf.Clamp(noise * 0.8f + beat + 0.15f, 0.08f, 0.98f);
                }
                else
                {
                    _targetBarHeights[i] = 0.05f;
                }

                _barHeights[i] = Mathf.Lerp(_barHeights[i], _targetBarHeights[i], 0.35f);
                if (_spectrumBars[i] != null)
                {
                    _spectrumBars[i].fillAmount = _barHeights[i];
                }
            }
        }

        private void AddPeriodicLog()
        {
            LogCategory[] cats = new LogCategory[] { LogCategory.Network, LogCategory.Security, LogCategory.System };
            string[] tags = new string[] { "[NET]", "[SEC]", "[SYS]" };
            string[] hexes = new string[] { "#00ff88", "#cc66ff", "#00ffff" };

            string[] msgs = new string[]
            {
                "WebRTC keyframe synchronized. Frame latency: 7.2 ms.",
                "Spatial audio buffer synchronized with room acoustics.",
                "Entropy seed verified: 256-bit encryption key confirmed.",
                "PCIe storage buffer flushed to remote virtual SAN.",
                "Multi-display spatial coordinates verified across 3 monitors.",
                "QoS stream pacing steady: 0 jitter detected."
            };

            int idx = Random.Range(0, msgs.Length);
            int catIdx = Random.Range(0, cats.Length);

            AddLog(cats[catIdx], tags[catIdx], msgs[idx], hexes[catIdx]);
            RefreshLogDisplay();
        }

        private void AddLog(LogCategory cat, string tag, string msg, string hexColor)
        {
            _allLogs.Add(new LogEntry
            {
                Timestamp = System.DateTime.Now.ToString("HH:mm:ss"),
                Category = cat,
                Tag = tag,
                Message = msg,
                ColorHex = hexColor
            });

            if (_allLogs.Count > 18)
            {
                _allLogs.RemoveAt(0);
            }
        }

        private void RefreshLogDisplay()
        {
            if (_logStreamText == null) return;

            List<string> lines = new List<string>();
            foreach (var log in _allLogs)
            {
                if (_activeFilter == LogCategory.All || log.Category == _activeFilter)
                {
                    lines.Add($"<color=#7799bb>[{log.Timestamp}]</color> <color={log.ColorHex}><b>{log.Tag}</b></color> {log.Message}");
                }
            }

            _logStreamText.text = string.Join("\n", lines);
        }

        public void SetFilter(LogCategory cat)
        {
            _activeFilter = cat;
            UpdateFilterButtonStyles();
            RefreshLogDisplay();
            AudioManager.Instance?.PlayClick();
        }

        private void UpdateFilterButtonStyles()
        {
            Color activeColor = new Color(0.8f, 0.3f, 1f);
            Color inactiveColor = new Color(0.08f, 0.16f, 0.28f);

            if (_btnAll != null) _btnAll.GetComponent<Image>().color = _activeFilter == LogCategory.All ? activeColor : inactiveColor;
            if (_btnNet != null) _btnNet.GetComponent<Image>().color = _activeFilter == LogCategory.Network ? activeColor : inactiveColor;
            if (_btnSec != null) _btnSec.GetComponent<Image>().color = _activeFilter == LogCategory.Security ? activeColor : inactiveColor;
            if (_btnSys != null) _btnSys.GetComponent<Image>().color = _activeFilter == LogCategory.System ? activeColor : inactiveColor;
        }

        public void OnPingHostClicked()
        {
            float ping = Random.Range(2.8f, 4.2f);
            if (_pingResultText != null)
            {
                _pingResultText.text = $"<color=#00ff88>LINK LATENCY: {ping:0.1} ms  |  JITTER: 0.1 ms  |  LOSS: 0.00% [OPTIMAL]</color>";
            }
            AudioManager.Instance?.PlayConnectChime();
        }

        public void OnClearLogsClicked()
        {
            _allLogs.Clear();
            AddLog(LogCategory.System, "[SYS]", "Activity feed cleared by operator.", "#00ffff");
            RefreshLogDisplay();
            AudioManager.Instance?.PlayClick();
        }

        public void OnTogglePlayPause()
        {
            _isPlaying = !_isPlaying;
            if (_trackTitleText != null)
            {
                _trackTitleText.text = _isPlaying ? _trackList[_trackIndex] : "[||] PAUSED";
            }
            AudioManager.Instance?.PlayClick();

            if (VRRemoteDesktop.Environment.RoomSpeakerController.Instance != null)
            {
                VRRemoteDesktop.Environment.RoomSpeakerController.Instance.TogglePlayPause();
            }
            if (_isPlaying)
            {
                VRRemoteDesktop.Environment.RoomLightingController.Instance?.ApplyZenTranquilLighting();
            }
        }

        public void OnNextTrackClicked()
        {
            _trackIndex = (_trackIndex + 1) % _trackList.Length;
            _isPlaying = true;
            if (_trackTitleText != null)
            {
                _trackTitleText.text = _trackList[_trackIndex];
            }
            AudioManager.Instance?.PlayConnectChime();

            VRRemoteDesktop.Environment.RoomSpeakerController.Instance?.PlayNextSong();
            VRRemoteDesktop.Environment.RoomLightingController.Instance?.ApplyZenTranquilLighting();
        }

        public void AdjustVolume(float delta)
        {
            _currentVolume = Mathf.Clamp01(_currentVolume + delta);
            if (_volumeFill != null) _volumeFill.fillAmount = _currentVolume;
            if (_volumeText != null) _volumeText.text = $"VOL: {(_currentVolume * 100f):0}%";
            AudioManager.Instance?.PlayClick();
        }

        public void OnMuteClicked()
        {
            _currentVolume = _currentVolume > 0f ? 0f : 0.8f;
            if (_volumeFill != null) _volumeFill.fillAmount = _currentVolume;
            if (_volumeText != null) _volumeText.text = $"VOL: {(_currentVolume * 100f):0}%";
            AudioManager.Instance?.PlayClick();
        }

        private int _themeIndex = 0;
        private readonly string[] _themeNames = new string[] { "CYBERPUNK CYAN", "MATRIX EMERALD", "SYNTHWAVE SUNSET", "SOLAR AMBER" };

        public void OnCycleThemeClicked()
        {
            ThemeManager.Instance?.CycleTheme();
            _themeIndex = (_themeIndex + 1) % _themeNames.Length;
            if (_themeStatusText != null)
            {
                _themeStatusText.text = $"ACTIVE ROOM THEME: <color=#00ffff>{_themeNames[_themeIndex]}</color>";
            }
            AudioManager.Instance?.PlayConnectChime();
        }

        public void OnToggleOverheadLights()
        {
            var pLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            foreach (var l in pLights)
            {
                if (l.type == LightType.Point && l.name.Contains("Wall"))
                {
                    l.intensity = l.intensity > 1.5f ? 0.8f : 3.0f;
                }
            }
            AudioManager.Instance?.PlayClick();
        }

        public void OnToggleShieldLockdown()
        {
            _shieldActive = !_shieldActive;
            if (_shieldStatusText != null)
            {
                _shieldStatusText.text = _shieldActive ?
                    "<color=#ff3366>SHIELD: ACTIVE LOCKDOWN [LEVEL 4 DEFENSE]</color>" :
                    "STATUS: <color=#00ff88>NORMAL OBSERVATION</color>  |  HOST: DESKTOP-VR-HOST (ONLINE)";
            }
            AudioManager.Instance?.PlayDisconnect();
        }

        public void OnQuickChimeClicked()
        {
            AudioManager.Instance?.PlayConnectChime();
        }

        public void OnToggleScreenShareClicked()
        {
            if (VRRemoteDesktop.Environment.ScreenShareToImaxManager.Instance == null)
            {
                var mgrObj = new GameObject("ScreenShareToImaxManager", typeof(VRRemoteDesktop.Environment.ScreenShareToImaxManager));
            }

            _isScreenSharing = VRRemoteDesktop.Environment.ScreenShareToImaxManager.Instance.ToggleScreenShare();
            UpdateScreenShareButtonVisuals();

            if (_isScreenSharing)
            {
                AddLog(LogCategory.System, "[IMAX]", "Middle Monitor desktop stream mirrored to 70mm IMAX projector array.", "#00ffff");
                AudioManager.Instance?.PlayConnectChime();
            }
            else
            {
                AddLog(LogCategory.System, "[IMAX]", "IMAX screen sharing terminated. Returned to standby.", "#88aacc");
                AudioManager.Instance?.PlayDisconnect();
            }
        }

        private void UpdateScreenShareButtonVisuals()
        {
            if (_screenShareText != null)
            {
                _screenShareText.text = _isScreenSharing ?
                    "<b><color=#00ffff>[FEED] SCREEN SHARE ACTIVE</color> -- <color=#00ff88>MIRRORING TO IMAX [ON]</color></b>" :
                    "<b>[FEED] SHARE MIDDLE MONITOR TO IMAX SCREEN</b>";
            }

            if (_screenShareImg != null)
            {
                Color targetBg = _isScreenSharing ? new Color(0.06f, 0.45f, 0.38f) : new Color(0.12f, 0.28f, 0.50f);
                _screenShareImg.color = targetBg;
                if (_btnScreenShare != null)
                {
                    ColorBlock cb = _btnScreenShare.colors;
                    cb.normalColor = targetBg;
                    cb.highlightedColor = targetBg * 1.35f;
                    cb.pressedColor = targetBg * 0.8f;
                    _btnScreenShare.colors = cb;
                }
            }

            if (_themeStatusText != null)
            {
                string imaxStatus = _isScreenSharing ? "<color=#00ff88>LIVE 4K MIDDLE PC MIRROR</color>" : "<color=#80c8ff>STANDBY</color>";
                string themeName = VRRemoteDesktop.Environment.RoomLightingController.Instance != null ?
                    VRRemoteDesktop.Environment.RoomLightingController.Instance.CurrentPreset.ToString() : "CYBERPUNK NEON";
                _themeStatusText.text = $"ACTIVE ROOM THEME: <color=#00ffff>{themeName}</color>  |  IMAX: {imaxStatus}";
            }
        }

        // Helper UI Builders
        private GameObject CreateCardPanel(string name, Transform parent, Vector2 aMin, Vector2 aMax, string title, Color accentColor)
        {
            GameObject card = CreateUIObject(name, parent, aMin, aMax, Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.11f, 0.94f));

            // Glowing top accent border
            CreateUIObject("CardAccent", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                           new Vector2(0f, -4f), new Vector2(0f, 0f), accentColor);

            // Title Header
            CreateTextObject("CardTitle", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(24f, -46f), new Vector2(-24f, -8f),
                             $"<color=#{ColorUtility.ToHtmlStringRGB(accentColor)}><b>{title}</b></color>", 24, FontStyles.Bold,
                             TextAlignmentOptions.MidlineLeft, Color.white);

            return card;
        }

        private GameObject CreateUIObject(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            obj.GetComponent<Image>().color = color;
            return obj;
        }

        private GameObject CreateTextObject(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax,
                                           string content, float fontSize, FontStyles style, TextAlignmentOptions align, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return obj;
        }

        private Button CreateButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.35f;
            cb.pressedColor = bgColor * 0.75f;
            cb.selectedColor = bgColor * 1.2f;
            btn.colors = cb;

            if (onClick != null) btn.onClick.AddListener(onClick);

            CreateTextObject("Text", btnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                             label, 20, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            return btn;
        }
    }
}
