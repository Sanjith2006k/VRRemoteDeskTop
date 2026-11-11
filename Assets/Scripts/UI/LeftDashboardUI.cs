using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRRemoteDesktop.UI
{
    public enum PerformanceProfile
    {
        Balanced,
        TurboOverclock,
        EcoQuiet
    }

    /// <summary>
    /// Rich hardware telemetry, multi-core performance metrics, and interactive
    /// power tuner dashboard rendered on the Left Auxiliary Display (L-01).
    /// Features large high-contrast fonts, interactive profile selectors,
    /// dynamic animated core meters, and hardware stress testing.
    /// </summary>
    public class LeftDashboardUI : MonoBehaviour
    {
        public static LeftDashboardUI Instance { get; private set; }

        public enum LeftScreenMode
        {
            Telemetry,
            Browser,
            MusicPlayer
        }

        [Header("Profile & Mode")]
        [SerializeField] private PerformanceProfile _currentProfile = PerformanceProfile.Balanced;
        [SerializeField] private LeftScreenMode _currentMode = LeftScreenMode.Telemetry;
        [SerializeField] private bool _fanBoostActive = false;

        private GameObject _telemetryContainer;
        private GameObject _browserContainer;
        private GameObject _musicContainer;
        private QuantumWebBrowserUI _browserUI;
        private Image _telemetryTabBg;
        private Image _browserTabBg;
        private Image _musicTabBg;
        private TextMeshProUGUI _telemetryTabTxt;
        private TextMeshProUGUI _browserTabTxt;
        private TextMeshProUGUI _musicTabTxt;

        // Header Quick Music Controls
        private Button _headerPlayBtn;
        private TextMeshProUGUI _headerPlayBtnText;
        private Image _headerPlayBtnBg;

        // Music Screen UI
        private TextMeshProUGUI _musicNowPlayingTitle;
        private TextMeshProUGUI _musicTimeText;
        private Slider _musicTimeSlider;
        private TextMeshProUGUI _musicPlayBtnText;
        private Image[] _trackItemBgs = new Image[5];
        private TextMeshProUGUI[] _trackItemStatusTexts = new TextMeshProUGUI[5];
        private RectTransform _visualizerRing1;
        private RectTransform _visualizerRing2;

        // UI References (Self-constructed or bound)
        private TextMeshProUGUI _clockText;
        private TextMeshProUGUI _cpuOverallText;
        private Image[] _coreFills = new Image[8];
        private TextMeshProUGUI[] _coreTexts = new TextMeshProUGUI[8];
        private TextMeshProUGUI _gpuOverallText;
        private Image _gpuFill;
        private Image _vramFill;
        private TextMeshProUGUI _vramText;
        private TextMeshProUGUI _gpuDetailsText;
        private TextMeshProUGUI _ramText;
        private Image _ramFill;
        private TextMeshProUGUI _nvmeReadText;
        private TextMeshProUGUI _nvmeWriteText;
        private Image _nvmeActivityDot;

        // Interactive Buttons
        private Button _turboBtn;
        private Button _balancedBtn;
        private Button _ecoBtn;
        private Button _stressTestBtn;
        private TextMeshProUGUI _stressTestBtnText;
        private Button _fanBoostBtn;
        private TextMeshProUGUI _fanBoostBtnText;
        private Slider _benchmarkSlider;
        private TextMeshProUGUI _benchmarkStatusText;

        private bool _isBenchmarking = false;
        private float _updateTimer = 0f;

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
            BuildDashboardUI();
            VRRemoteDesktop.Environment.RoomSpeakerController.OnPlaybackChanged -= HandlePlaybackChanged;
            VRRemoteDesktop.Environment.RoomSpeakerController.OnPlaybackChanged += HandlePlaybackChanged;
        }

        private void OnDestroy()
        {
            VRRemoteDesktop.Environment.RoomSpeakerController.OnPlaybackChanged -= HandlePlaybackChanged;
        }

        private void HandlePlaybackChanged(string track, bool isPlaying)
        {
            UpdateMusicUI();
        }

        public void BuildDashboardUI()
        {
            // Clear old children if present to ensure clean state
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Canvas size: 1920 x 1080
            // 1. Top Header Banner
            BuildHeaderBanner();

            // 2. Telemetry Root Container
            _telemetryContainer = new GameObject("TelemetryContainer", typeof(RectTransform));
            _telemetryContainer.transform.SetParent(transform, false);
            RectTransform trt = _telemetryContainer.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            // Build Telemetry Cards inside container
            BuildCpuCard(_telemetryContainer.transform);
            BuildGpuCard(_telemetryContainer.transform);
            BuildPowerTunerCard(_telemetryContainer.transform);
            BuildMemoryStorageCard(_telemetryContainer.transform);

            // 3. Browser Root Container
            _browserContainer = new GameObject("BrowserContainer", typeof(RectTransform));
            _browserContainer.transform.SetParent(transform, false);
            RectTransform brt = _browserContainer.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = new Vector2(0f, -90f);

            _browserUI = _browserContainer.AddComponent<QuantumWebBrowserUI>();

            // 4. Peaceful Music Player Container
            _musicContainer = new GameObject("MusicContainer", typeof(RectTransform));
            _musicContainer.transform.SetParent(transform, false);
            RectTransform mrt = _musicContainer.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero;
            mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero;
            mrt.offsetMax = new Vector2(0f, -90f);

            BuildMusicPlayerScreen(_musicContainer.transform);

            SetMode(_currentMode);
            UpdateProfileUI();
            UpdateMusicUI();
        }

        private void BuildHeaderBanner()
        {
            GameObject bannerObj = CreateUIObject("HeaderBanner", transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                 new Vector2(0f, -90f), new Vector2(0f, 0f), new Color(0.04f, 0.08f, 0.16f, 0.96f));

            // Cyan top accent line
            CreateUIObject("TopAccent", bannerObj.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                           new Vector2(0f, -4f), new Vector2(0f, 0f), new Color(0f, 1f, 1f, 1f));

            // Title
            GameObject titleObj = CreateTextObject("Title", bannerObj.transform, new Vector2(0f, 0f), new Vector2(0.27f, 1f),
                                                   new Vector2(24f, 0f), new Vector2(0f, 0f),
                                                   "<color=#00ffff>WORKSTATION</color>  |  <color=#ffffff>AUX DISPLAY L-01</color>",
                                                   24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            // Center Display Mode Switcher Tabs
            GameObject modeTabsBox = CreateUIObject("ModeTabsBox", bannerObj.transform, new Vector2(0.28f, 0.5f), new Vector2(0.60f, 0.5f),
                                                    new Vector2(0f, -24f), new Vector2(0f, 24f), new Color(0.03f, 0.05f, 0.09f, 0.8f));

            // Tab 1: Telemetry
            Button telBtnObj = CreateButton("Tab_Telemetry", modeTabsBox.transform, new Vector2(0f, 0f), new Vector2(0.333f, 1f),
                                            "[STATS] TELEMETRY", new Color(0f, 0.5f, 0.7f, 0.95f), () => SetMode(LeftScreenMode.Telemetry));
            _telemetryTabBg = telBtnObj.GetComponent<Image>();
            _telemetryTabTxt = telBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Tab 2: Browser
            Button broBtnObj = CreateButton("Tab_Browser", modeTabsBox.transform, new Vector2(0.333f, 0f), new Vector2(0.666f, 1f),
                                            "[WEB] BROWSER", new Color(0.06f, 0.12f, 0.20f, 0.8f), () => ShowBrowser());
            _browserTabBg = broBtnObj.GetComponent<Image>();
            _browserTabTxt = broBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Tab 3: Peaceful Music
            Button musBtnObj = CreateButton("Tab_Music", modeTabsBox.transform, new Vector2(0.666f, 0f), new Vector2(1f, 1f),
                                            "[AUDIO] PEACEFUL MUSIC", new Color(0.06f, 0.12f, 0.20f, 0.8f), () => ShowMusicPlayer());
            _musicTabBg = musBtnObj.GetComponent<Image>();
            _musicTabTxt = musBtnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Quick Play Music Header Controls
            GameObject quickMusicBox = CreateUIObject("QuickMusicBox", bannerObj.transform, new Vector2(0.61f, 0.5f), new Vector2(0.83f, 0.5f),
                                                      new Vector2(0f, -24f), new Vector2(0f, 24f), new Color(0.05f, 0.16f, 0.12f, 0.95f));

            _headerPlayBtn = CreateButton("Btn_HeaderPlayMusic", quickMusicBox.transform, new Vector2(0f, 0f), new Vector2(0.72f, 1f),
                                          "> PLAY MUSIC (PEACEFUL)", new Color(0.08f, 0.60f, 0.35f, 1f), OnToggleMusicClicked);
            _headerPlayBtnBg = _headerPlayBtn.GetComponent<Image>();
            _headerPlayBtnText = _headerPlayBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (_headerPlayBtnText != null) _headerPlayBtnText.fontSize = 16;

            Button nextBtn = CreateButton("Btn_HeaderNextMusic", quickMusicBox.transform, new Vector2(0.74f, 0f), new Vector2(1f, 1f),
                                          "[>>] NEXT", new Color(0.06f, 0.35f, 0.25f, 1f), OnNextMusicClicked);
            var nextTxt = nextBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (nextTxt != null) nextTxt.fontSize = 15;

            // Subtitle / Host Status & Live Clock
            GameObject statusObj = CreateTextObject("StatusClock", bannerObj.transform, new Vector2(0.84f, 0f), new Vector2(1f, 1f),
                                                    new Vector2(0f, 0f), new Vector2(-20f, 0f),
                                                    "HOST: PC-01 | <color=#00ff88>ONLINE</color>",
                                                    17, FontStyles.Bold, TextAlignmentOptions.MidlineRight, new Color(0.7f, 0.9f, 1f));
            _clockText = statusObj.GetComponent<TextMeshProUGUI>();
        }

        public void SetMode(LeftScreenMode mode)
        {
            _currentMode = mode;
            if (_telemetryContainer != null) _telemetryContainer.SetActive(mode == LeftScreenMode.Telemetry);
            if (_browserContainer != null) _browserContainer.SetActive(mode == LeftScreenMode.Browser);
            if (_musicContainer != null) _musicContainer.SetActive(mode == LeftScreenMode.MusicPlayer);

            if (_telemetryTabBg != null)
                _telemetryTabBg.color = (mode == LeftScreenMode.Telemetry) ? new Color(0f, 0.5f, 0.7f, 0.95f) : new Color(0.06f, 0.12f, 0.20f, 0.8f);
            if (_browserTabBg != null)
                _browserTabBg.color = (mode == LeftScreenMode.Browser) ? new Color(0f, 0.7f, 0.8f, 0.95f) : new Color(0.06f, 0.12f, 0.20f, 0.8f);
            if (_musicTabBg != null)
                _musicTabBg.color = (mode == LeftScreenMode.MusicPlayer) ? new Color(0.08f, 0.60f, 0.35f, 0.95f) : new Color(0.06f, 0.12f, 0.20f, 0.8f);

            if (_telemetryTabTxt != null)
                _telemetryTabTxt.color = (mode == LeftScreenMode.Telemetry) ? Color.white : new Color(0.7f, 0.85f, 1f);
            if (_browserTabTxt != null)
                _browserTabTxt.color = (mode == LeftScreenMode.Browser) ? Color.white : new Color(0.7f, 0.85f, 1f);
            if (_musicTabTxt != null)
                _musicTabTxt.color = (mode == LeftScreenMode.MusicPlayer) ? Color.white : new Color(0.7f, 0.85f, 1f);

            if (mode == LeftScreenMode.MusicPlayer)
            {
                UpdateMusicUI();
            }
        }

        public void ShowBrowser()
        {
            SetMode(LeftScreenMode.Browser);
            if (_browserUI != null) _browserUI.OpenBrowser();
        }

        public void ShowTelemetry()
        {
            SetMode(LeftScreenMode.Telemetry);
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();
        }

        public void ShowMusicPlayer()
        {
            SetMode(LeftScreenMode.MusicPlayer);
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();
            UpdateMusicUI();
        }

        private void BuildCpuCard(Transform parent = null)
        {
            Transform p = parent != null ? parent : transform;
            GameObject card = CreateCardPanel("Card_CPU", p, new Vector2(0.02f, 0.52f), new Vector2(0.485f, 0.90f),
                                             "CPU MULTI-CORE TELEMETRY", new Color(0f, 1f, 1f));

            // Overall Readout
            GameObject overallObj = CreateTextObject("CpuOverall", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                     new Vector2(24f, -90f), new Vector2(-24f, -44f),
                                                     "CPU LOAD: 38.5%  |  4.85 GHz  |  52 degC", 30, FontStyles.Bold,
                                                     TextAlignmentOptions.MidlineLeft, new Color(0f, 1f, 0.9f));
            _cpuOverallText = overallObj.GetComponent<TextMeshProUGUI>();

            // 8 Core Bars Container (2 columns x 4 rows)
            GameObject grid = new GameObject("CoresGrid", typeof(RectTransform));
            grid.transform.SetParent(card.transform, false);
            RectTransform grt = grid.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0f, 0f);
            grt.anchorMax = new Vector2(1f, 1f);
            grt.offsetMin = new Vector2(24f, 44f);
            grt.offsetMax = new Vector2(-24f, -95f);

            float colWidth = 0.48f;
            float rowHeight = 0.22f;

            for (int i = 0; i < 8; i++)
            {
                int col = i % 2;
                int row = 3 - (i / 2);

                float minX = col * 0.52f;
                float maxX = minX + colWidth;
                float minY = row * rowHeight + 0.04f;
                float maxY = minY + 0.18f;

                GameObject rowObj = new GameObject($"CoreRow_{i}", typeof(RectTransform));
                rowObj.transform.SetParent(grid.transform, false);
                RectTransform rrt = rowObj.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(minX, minY);
                rrt.anchorMax = new Vector2(maxX, maxY);
                rrt.offsetMin = Vector2.zero;
                rrt.offsetMax = Vector2.zero;

                // Core label
                CreateTextObject("Label", rowObj.transform, new Vector2(0f, 0f), new Vector2(0.32f, 1f),
                                 Vector2.zero, Vector2.zero, $"CORE 0{i + 1}", 20, FontStyles.Bold,
                                 TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.85f, 1f));

                // Progress bar background
                GameObject barBg = CreateUIObject("BarBg", rowObj.transform, new Vector2(0.34f, 0.15f), new Vector2(0.78f, 0.85f),
                                                  Vector2.zero, Vector2.zero, new Color(0.06f, 0.12f, 0.22f, 1f));

                // Progress bar fill
                GameObject barFill = CreateUIObject("BarFill", barBg.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                    Vector2.zero, Vector2.zero, new Color(0f, 1f, 0.85f, 1f));
                Image fillImg = barFill.GetComponent<Image>();
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillAmount = 0.45f;
                _coreFills[i] = fillImg;

                // Percentage text
                GameObject pctObj = CreateTextObject("Pct", rowObj.transform, new Vector2(0.80f, 0f), new Vector2(1f, 1f),
                                                     Vector2.zero, Vector2.zero, "45%", 20, FontStyles.Bold,
                                                     TextAlignmentOptions.MidlineRight, Color.white);
                _coreTexts[i] = pctObj.GetComponent<TextMeshProUGUI>();
            }

            // Architecture Footer
            CreateTextObject("Footer", card.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                             new Vector2(24f, 10f), new Vector2(-24f, 38f),
                             "16 Cores (8P + 8E) | 24 Threads | 36MB L3 Cache | TDP 125W", 18, FontStyles.Normal,
                             TextAlignmentOptions.MidlineLeft, new Color(0.5f, 0.7f, 0.9f));
        }

        private void BuildGpuCard(Transform parent = null)
        {
            Transform p = parent != null ? parent : transform;
            GameObject card = CreateCardPanel("Card_GPU", p, new Vector2(0.515f, 0.52f), new Vector2(0.98f, 0.90f),
                                             "GPU PERFORMANCE & VRAM", new Color(0.7f, 0.4f, 1f));

            // Overall GPU Stat
            GameObject overallObj = CreateTextObject("GpuOverall", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                     new Vector2(24f, -90f), new Vector2(-24f, -44f),
                                                     "GPU LOAD: 46%  |  2580 MHz  |  54 degC", 30, FontStyles.Bold,
                                                     TextAlignmentOptions.MidlineLeft, new Color(0.8f, 0.55f, 1f));
            _gpuOverallText = overallObj.GetComponent<TextMeshProUGUI>();

            // Large GPU Engine Load Bar
            CreateTextObject("GpuBarLabel", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(24f, -125f), new Vector2(-24f, -95f),
                             "CORE COMPUTE ENGINE LOAD", 20, FontStyles.Bold,
                             TextAlignmentOptions.MidlineLeft, new Color(0.8f, 0.85f, 1f));

            GameObject gpuBarBg = CreateUIObject("GpuBarBg", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                 new Vector2(24f, -155f), new Vector2(-24f, -130f), new Color(0.08f, 0.1f, 0.2f, 1f));

            GameObject gpuBarFill = CreateUIObject("GpuBarFill", gpuBarBg.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                   Vector2.zero, Vector2.zero, new Color(0.75f, 0.35f, 1f, 1f));
            _gpuFill = gpuBarFill.GetComponent<Image>();
            _gpuFill.type = Image.Type.Filled;
            _gpuFill.fillMethod = Image.FillMethod.Horizontal;
            _gpuFill.fillAmount = 0.46f;

            // VRAM Bar & Text
            GameObject vramTextObj = CreateTextObject("VramText", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                      new Vector2(24f, -195f), new Vector2(-24f, -165f),
                                                      "VRAM: 8.6 / 24.0 GB GDDR6X (36%)", 24, FontStyles.Bold,
                                                      TextAlignmentOptions.MidlineLeft, new Color(0f, 1f, 0.9f));
            _vramText = vramTextObj.GetComponent<TextMeshProUGUI>();

            GameObject vramBarBg = CreateUIObject("VramBarBg", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                  new Vector2(24f, -225f), new Vector2(-24f, -200f), new Color(0.08f, 0.1f, 0.2f, 1f));

            GameObject vramBarFill = CreateUIObject("VramBarFill", vramBarBg.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                    Vector2.zero, Vector2.zero, new Color(0f, 0.9f, 1f, 1f));
            _vramFill = vramBarFill.GetComponent<Image>();
            _vramFill.type = Image.Type.Filled;
            _vramFill.fillMethod = Image.FillMethod.Horizontal;
            _vramFill.fillAmount = 0.36f;

            // Technical Specs & Frametime
            GameObject detailsObj = CreateTextObject("Details", card.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                     new Vector2(24f, 12f), new Vector2(-24f, -235f),
                                                     "ENGINE: Vulkan Ray-Tracing Enabled  |  BUS: PCIe 5.0 x16\nFAN SPEED: 1420 RPM (42%)  |  HOTSPOT: 62 degC\n<color=#00ff88>RENDER LATENCY: 7.2 ms  |  120.0 FPS LOCKED</color>",
                                                     20, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.7f, 0.85f, 1f));
            _gpuDetailsText = detailsObj.GetComponent<TextMeshProUGUI>();
        }

        private void BuildPowerTunerCard(Transform parent = null)
        {
            Transform p = parent != null ? parent : transform;
            GameObject card = CreateCardPanel("Card_Tuning", p, new Vector2(0.02f, 0.05f), new Vector2(0.485f, 0.49f),
                                             "POWER PROFILES & HARDWARE BENCHMARK", new Color(1f, 0.7f, 0.1f));

            CreateTextObject("Sub", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(24f, -80f), new Vector2(-24f, -48f),
                             "SELECT HARDWARE PROFILE OR EXECUTE SYSTEM BENCHMARK:", 20, FontStyles.Bold,
                             TextAlignmentOptions.MidlineLeft, new Color(0.8f, 0.9f, 1f));

            // Profile Buttons row: Turbo, Balanced, Eco
            _turboBtn = CreateButton("Btn_Turbo", card.transform, new Vector2(0.03f, 0.60f), new Vector2(0.32f, 0.78f),
                                     "[PWR] TURBO OC", new Color(0.6f, 0.2f, 0.05f), () => SetProfile(PerformanceProfile.TurboOverclock));

            _balancedBtn = CreateButton("Btn_Balanced", card.transform, new Vector2(0.35f, 0.60f), new Vector2(0.65f, 0.78f),
                                        "[BAL] BALANCED", new Color(0.05f, 0.35f, 0.45f), () => SetProfile(PerformanceProfile.Balanced));

            _ecoBtn = CreateButton("Btn_Eco", card.transform, new Vector2(0.68f, 0.60f), new Vector2(0.97f, 0.78f),
                                   "[ECO] ECO QUIET", new Color(0.05f, 0.4f, 0.2f), () => SetProfile(PerformanceProfile.EcoQuiet));

            // Stress Test Section
            _stressTestBtn = CreateButton("Btn_StressTest", card.transform, new Vector2(0.03f, 0.36f), new Vector2(0.55f, 0.54f),
                                          "[SYNC] RUN HARDWARE STRESS TEST", new Color(0.15f, 0.22f, 0.45f), OnStartStressTestClicked);
            _stressTestBtnText = _stressTestBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Fan Boost Toggle
            _fanBoostBtn = CreateButton("Btn_FanBoost", card.transform, new Vector2(0.58f, 0.36f), new Vector2(0.97f, 0.54f),
                                        "[COOL] FAN BOOST: OFF", new Color(0.1f, 0.2f, 0.35f), OnToggleFanBoost);
            _fanBoostBtnText = _fanBoostBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Benchmark Progress Bar
            GameObject benchBg = CreateUIObject("BenchBg", card.transform, new Vector2(0.03f, 0.22f), new Vector2(0.97f, 0.30f),
                                                Vector2.zero, Vector2.zero, new Color(0.06f, 0.1f, 0.18f, 1f));

            GameObject benchFill = CreateUIObject("BenchFill", benchBg.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                  Vector2.zero, Vector2.zero, new Color(0f, 1f, 0.85f, 1f));
            var fillImg = benchFill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            // Store fillImg in slider wrapper
            var slider = benchBg.AddComponent<Slider>();
            slider.fillRect = benchFill.GetComponent<RectTransform>();
            slider.value = 0f;
            _benchmarkSlider = slider;

            // Benchmark Status
            GameObject statusObj = CreateTextObject("Status", card.transform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.20f),
                                                    Vector2.zero, Vector2.zero,
                                                    "<color=#00ff88>[OK] STATUS: ALL 16 CORES & MEMORY OPTIMAL (0 ERRORS)</color>", 20, FontStyles.Bold,
                                                    TextAlignmentOptions.MidlineLeft, Color.white);
            _benchmarkStatusText = statusObj.GetComponent<TextMeshProUGUI>();
        }

        private void BuildMemoryStorageCard(Transform parent = null)
        {
            Transform p = parent != null ? parent : transform;
            GameObject card = CreateCardPanel("Card_Memory", p, new Vector2(0.515f, 0.05f), new Vector2(0.98f, 0.49f),
                                             "MEMORY ALLOCATION & NVMe STORAGE BUS", new Color(0f, 0.8f, 1f));

            // RAM Section
            GameObject ramTextObj = CreateTextObject("RamText", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                     new Vector2(24f, -80f), new Vector2(-24f, -48f),
                                                     "RAM ALLOCATION: 14.8 / 32.0 GB DDR5-6000 (46%)", 26, FontStyles.Bold,
                                                     TextAlignmentOptions.MidlineLeft, new Color(0f, 1f, 0.9f));
            _ramText = ramTextObj.GetComponent<TextMeshProUGUI>();

            GameObject ramBarBg = CreateUIObject("RamBarBg", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                 new Vector2(24f, -112f), new Vector2(-24f, -88f), new Color(0.08f, 0.12f, 0.22f, 1f));

            GameObject ramBarFill = CreateUIObject("RamBarFill", ramBarBg.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                   Vector2.zero, Vector2.zero, new Color(0f, 0.8f, 1f, 1f));
            _ramFill = ramBarFill.GetComponent<Image>();
            _ramFill.type = Image.Type.Filled;
            _ramFill.fillMethod = Image.FillMethod.Horizontal;
            _ramFill.fillAmount = 0.46f;

            // NVMe Section
            CreateTextObject("StorageTitle", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(24f, -165f), new Vector2(-24f, -125f),
                             "PCIe Gen4 NVMe M.2 ARRAY (RAID 0 - 4TB)", 22, FontStyles.Bold,
                             TextAlignmentOptions.MidlineLeft, new Color(0.8f, 0.9f, 1f));

            GameObject rObj = CreateTextObject("Read", card.transform, new Vector2(0.04f, 0.24f), new Vector2(0.48f, 0.48f),
                                               Vector2.zero, Vector2.zero,
                                               "^ NVMe READ\n<size=30><color=#00ffff><b>5,420 MB/s</b></color></size>", 22, FontStyles.Bold,
                                               TextAlignmentOptions.MidlineLeft, Color.white);
            _nvmeReadText = rObj.GetComponent<TextMeshProUGUI>();

            GameObject wObj = CreateTextObject("Write", card.transform, new Vector2(0.52f, 0.24f), new Vector2(0.96f, 0.48f),
                                               Vector2.zero, Vector2.zero,
                                               "v NVMe WRITE\n<size=30><color=#ff77bb><b>4,180 MB/s</b></color></size>", 22, FontStyles.Bold,
                                               TextAlignmentOptions.MidlineLeft, Color.white);
            _nvmeWriteText = wObj.GetComponent<TextMeshProUGUI>();

            // Pulsing activity indicator & Mini Music Quick-Bar
            CreateTextObject("BusStatus", card.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                             new Vector2(24f, 52f), new Vector2(-24f, 78f),
                             "I/O QUEUE DEPTH: 32  |  SECTOR HEALTH: 100% OK  |  DMA CACHE: SYNCED", 18, FontStyles.Normal,
                             TextAlignmentOptions.MidlineLeft, new Color(0.5f, 0.75f, 0.9f));

            CreateButton("Btn_MiniPlayMusic", card.transform, new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.11f),
                         "[AUDIO] PLAY MUSIC FROM S:\\arvr project\\songs (PEACEFUL ATMOSPHERE)", new Color(0.08f, 0.55f, 0.35f, 0.95f), OnToggleMusicClicked);
        }

        private void Update()
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= 0.2f)
            {
                _updateTimer = 0f;
                RefreshTelemetry();
                UpdateMusicPlaybackTick();
            }

            if (_clockText != null)
            {
                _clockText.text = $"HOST: PC-01 | <color=#00ff88>ALL CORES ONLINE</color> | {System.DateTime.Now:HH:mm:ss}";
            }

            // Visualizer animation when in MusicPlayer mode
            if (_currentMode == LeftScreenMode.MusicPlayer)
            {
                var speaker = VRRemoteDesktop.Environment.RoomSpeakerController.Instance;
                if (speaker != null && speaker.IsPlaying)
                {
                    float t = Time.time;
                    if (_visualizerRing1 != null)
                    {
                        float pulse1 = 1f + 0.15f * Mathf.Sin(t * 4.2f);
                        _visualizerRing1.localScale = new Vector3(pulse1, pulse1, 1f);
                        _visualizerRing1.localEulerAngles = new Vector3(0f, 0f, t * 25f);
                    }
                    if (_visualizerRing2 != null)
                    {
                        float pulse2 = 1f + 0.20f * Mathf.Cos(t * 3.5f);
                        _visualizerRing2.localScale = new Vector3(pulse2, pulse2, 1f);
                        _visualizerRing2.localEulerAngles = new Vector3(0f, 0f, -t * 35f);
                    }
                }
            }
        }

        private void UpdateMusicPlaybackTick()
        {
            var speaker = VRRemoteDesktop.Environment.RoomSpeakerController.Instance;
            if (speaker != null && speaker.IsPlaying)
            {
                if (_musicTimeSlider != null && speaker.CurrentTrackLength > 0.1f)
                {
                    _musicTimeSlider.value = Mathf.Clamp01(speaker.CurrentTrackTime / speaker.CurrentTrackLength);
                }
                if (_musicTimeText != null)
                {
                    _musicTimeText.text = $"{FormatTime(speaker.CurrentTrackTime)} / {FormatTime(speaker.CurrentTrackLength)}  |  STAGE TOWER SPEAKERS [FL & FR]";
                }
            }
        }

        private void RefreshTelemetry()
        {
            float t = Time.time;
            float targetLoadMultiplier = _currentProfile == PerformanceProfile.TurboOverclock ? 1.6f :
                                         (_currentProfile == PerformanceProfile.EcoQuiet ? 0.65f : 1.0f);

            float baseGpu = _currentProfile == PerformanceProfile.TurboOverclock ? 75f :
                            (_currentProfile == PerformanceProfile.EcoQuiet ? 25f : 46f);

            // CPU updates
            float avgCpu = 0f;
            for (int i = 0; i < 8; i++)
            {
                float noise = Mathf.PerlinNoise(i * 1.8f, t * 0.5f);
                float load = Mathf.Clamp((noise * 60f + 15f) * targetLoadMultiplier, 8f, 98f);
                if (_isBenchmarking) load = Mathf.Clamp(88f + Mathf.Sin(t * 10f) * 10f, 75f, 99f);
                avgCpu += load;

                if (_coreFills[i] != null) _coreFills[i].fillAmount = load / 100f;
                if (_coreTexts[i] != null) _coreTexts[i].text = $"{load:0}%";
            }
            avgCpu /= 8f;

            float cpuGhz = _currentProfile == PerformanceProfile.TurboOverclock ? 5.25f :
                           (_currentProfile == PerformanceProfile.EcoQuiet ? 3.40f : 4.85f);
            int cpuTemp = _currentProfile == PerformanceProfile.TurboOverclock ? 68 + (int)(avgCpu * 0.15f) :
                          (_currentProfile == PerformanceProfile.EcoQuiet ? 42 : 52 + (int)(avgCpu * 0.1f));

            if (_cpuOverallText != null)
            {
                _cpuOverallText.text = $"CPU LOAD: {avgCpu:0.0}%  |  {cpuGhz:0.00} GHz  |  {cpuTemp} degC";
            }

            // GPU updates
            float gpuLoad = Mathf.Clamp(baseGpu + Mathf.Sin(t * 0.8f) * 12f, 10f, 98f);
            if (_isBenchmarking) gpuLoad = 96f;

            if (_gpuFill != null) _gpuFill.fillAmount = gpuLoad / 100f;
            int gpuTemp = 50 + (int)(gpuLoad * 0.18f);
            if (_fanBoostActive) gpuTemp = Mathf.Max(38, gpuTemp - 14);

            int fanRpm = _fanBoostActive ? 2800 : (1200 + (int)(gpuLoad * 8f));

            if (_gpuOverallText != null)
            {
                _gpuOverallText.text = $"GPU LOAD: {gpuLoad:0}%  |  2580 MHz  |  {gpuTemp} degC";
            }

            if (_gpuDetailsText != null)
            {
                _gpuDetailsText.text = $"ENGINE: Vulkan Ray-Tracing Enabled  |  BUS: PCIe 5.0 x16\nFAN SPEED: {fanRpm} RPM ({(_fanBoostActive ? 100 : (int)(fanRpm / 28f)):0}%)  |  HOTSPOT: {gpuTemp + 8} degC\n<color=#00ff88>RENDER LATENCY: 7.2 ms  |  120.0 FPS LOCKED</color>";
            }

            // RAM & Storage
            float ramUsed = 14.8f + Mathf.Sin(t * 0.15f) * 1.2f;
            if (_ramFill != null) _ramFill.fillAmount = ramUsed / 32.0f;
            if (_ramText != null) _ramText.text = $"RAM ALLOCATION: {ramUsed:0.1} / 32.0 GB DDR5-6000 ({(ramUsed / 32f) * 100f:0}%)";

            float rSpeed = 4800f + Mathf.PerlinNoise(0f, t * 1.5f) * 900f;
            float wSpeed = 3800f + Mathf.PerlinNoise(1f, t * 1.5f) * 600f;
            if (_nvmeReadText != null) _nvmeReadText.text = $"^ NVMe READ\n<size=30><color=#00ffff><b>{rSpeed:0} MB/s</b></color></size>";
            if (_nvmeWriteText != null) _nvmeWriteText.text = $"v NVMe WRITE\n<size=30><color=#ff77bb><b>{wSpeed:0} MB/s</b></color></size>";
        }

        public void SetProfile(PerformanceProfile profile)
        {
            _currentProfile = profile;
            UpdateProfileUI();
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayConnectChime();
        }

        private void UpdateProfileUI()
        {
            if (_turboBtn != null)
            {
                _turboBtn.GetComponent<Image>().color = _currentProfile == PerformanceProfile.TurboOverclock ?
                    new Color(1f, 0.35f, 0.05f) : new Color(0.25f, 0.12f, 0.06f);
            }
            if (_balancedBtn != null)
            {
                _balancedBtn.GetComponent<Image>().color = _currentProfile == PerformanceProfile.Balanced ?
                    new Color(0f, 0.85f, 1f) : new Color(0.06f, 0.2f, 0.28f);
            }
            if (_ecoBtn != null)
            {
                _ecoBtn.GetComponent<Image>().color = _currentProfile == PerformanceProfile.EcoQuiet ?
                    new Color(0.1f, 1f, 0.5f) : new Color(0.06f, 0.25f, 0.14f);
            }
        }

        public void OnToggleFanBoost()
        {
            _fanBoostActive = !_fanBoostActive;
            if (_fanBoostBtnText != null)
            {
                _fanBoostBtnText.text = _fanBoostActive ? "[COOL] FAN BOOST: MAX (100%)" : "[COOL] FAN BOOST: OFF";
            }
            if (_fanBoostBtn != null)
            {
                _fanBoostBtn.GetComponent<Image>().color = _fanBoostActive ?
                    new Color(0f, 0.7f, 1f) : new Color(0.1f, 0.2f, 0.35f);
            }
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();
        }

        public void OnStartStressTestClicked()
        {
            if (_isBenchmarking) return;
            StartCoroutine(RunBenchmarkRoutine());
        }

        private IEnumerator RunBenchmarkRoutine()
        {
            _isBenchmarking = true;
            if (_stressTestBtn != null) _stressTestBtn.interactable = false;
            if (_stressTestBtnText != null) _stressTestBtnText.text = "TESTING HARDWARE...";

            float elapsed = 0f;
            float duration = 3.5f;

            string[] phases = new string[] {
                "STRESSING 8 HIGH-PERFORMANCE CORES...",
                "CHECKING DDR5 MEMORY BUS INTEGRITY...",
                "VERIFYING VRAM ECC MATRIX...",
                "TESTING NVMe BUFFER PARITY...",
                "BENCHMARK COMPLETE: ZERO ERRORS"
            };

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                if (_benchmarkSlider != null) _benchmarkSlider.value = progress;

                int phaseIdx = Mathf.Clamp((int)(progress * phases.Length), 0, phases.Length - 1);
                if (_benchmarkStatusText != null)
                {
                    _benchmarkStatusText.text = $"<color=#00ffff>RUNNING ({progress * 100f:0}%): {phases[phaseIdx]}</color>";
                }
                yield return null;
            }

            if (_benchmarkSlider != null) _benchmarkSlider.value = 1f;
            if (_benchmarkStatusText != null)
            {
                _benchmarkStatusText.text = "<color=#00ff88>[OK] BENCHMARK PASSED: 100% HEALTH (SCORE: 28,490 PTS)</color>";
            }

            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayConnectChime();
            if (_stressTestBtn != null) _stressTestBtn.interactable = true;
            if (_stressTestBtnText != null) _stressTestBtnText.text = "[SYNC] RUN HARDWARE STRESS TEST";
            _isBenchmarking = false;
        }

        // Helper UI Builders
        private GameObject CreateCardPanel(string name, Transform parent, Vector2 aMin, Vector2 aMax, string title, Color accentColor)
        {
            GameObject card = CreateUIObject(name, parent, aMin, aMax, Vector2.zero, Vector2.zero, new Color(0.035f, 0.06f, 0.11f, 0.94f));

            // Glowing top accent border
            GameObject topAccent = CreateUIObject("CardAccent", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
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

            // Button label
            GameObject lblObj = CreateTextObject("Text", btnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                                                 label, 20, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            return btn;
        }

        private Button CreateButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;

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

        #region Peaceful Music Player Screen & Control Logic
        private void BuildMusicPlayerScreen(Transform parent)
        {
            // Left Card: Now Playing & Audio Controls
            GameObject playerCard = CreateCardPanel("Card_AudioPlayer", parent, new Vector2(0.02f, 0.04f), new Vector2(0.485f, 0.96f),
                                                    "ZEN ACOUSTICS & HIFI ROOM SPEAKER CONTROLLER", new Color(0.1f, 0.95f, 0.55f));

            // Album / Visualizer Box
            GameObject visBox = CreateUIObject("VisualizerBox", playerCard.transform, new Vector2(0.12f, 0.50f), new Vector2(0.88f, 0.88f),
                                               Vector2.zero, Vector2.zero, new Color(0.02f, 0.05f, 0.08f, 0.98f));

            // Visualizer Accent Border
            CreateUIObject("VisBorder", visBox.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                           Vector2.zero, Vector2.zero, new Color(0.1f, 0.85f, 0.55f, 0.20f));

            // Visualizer Rings
            GameObject ring1 = CreateUIObject("Ring1", visBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                              new Vector2(-120f, -120f), new Vector2(120f, 120f), new Color(0f, 0.9f, 0.8f, 0.25f));
            _visualizerRing1 = ring1.GetComponent<RectTransform>();

            GameObject ring2 = CreateUIObject("Ring2", visBox.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                              new Vector2(-80f, -80f), new Vector2(80f, 80f), new Color(0.1f, 1f, 0.6f, 0.35f));
            _visualizerRing2 = ring2.GetComponent<RectTransform>();

            // Center Music Icon
            CreateTextObject("VisIcon", visBox.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                             Vector2.zero, Vector2.zero, "[AUDIO]", 72, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.3f, 1f, 0.7f));

            // Now Playing Title
            GameObject titleObj = CreateTextObject("NowPlayingTitle", playerCard.transform, new Vector2(0.04f, 0.41f), new Vector2(0.96f, 0.49f),
                                                   Vector2.zero, Vector2.zero,
                                                   "<color=#00ffff>NOW PLAYING:</color> VELARIOMUSIC - PIANO RELAXING", 26, FontStyles.Bold,
                                                   TextAlignmentOptions.Center, Color.white);
            _musicNowPlayingTitle = titleObj.GetComponent<TextMeshProUGUI>();

            // Source & Atmosphere Tags
            CreateTextObject("AtmosphereTag", playerCard.transform, new Vector2(0.04f, 0.33f), new Vector2(0.96f, 0.40f),
                             Vector2.zero, Vector2.zero,
                             "SOURCE: <color=#00e5ff>S:\\arvr project\\songs (5 Tracks)</color>  |  <color=#00ff88>ATMOSPHERE: PEACEFUL JADE TWILIGHT</color>", 19, FontStyles.Bold,
                             TextAlignmentOptions.Center, new Color(0.8f, 0.95f, 1f));

            // Time Slider & Readout
            GameObject timeSliderBg = CreateUIObject("TimeSliderBg", playerCard.transform, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.29f),
                                                     Vector2.zero, Vector2.zero, new Color(0.08f, 0.12f, 0.20f, 1f));

            GameObject timeSliderFill = CreateUIObject("TimeSliderFill", timeSliderBg.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                       Vector2.zero, Vector2.zero, new Color(0f, 1f, 0.75f, 1f));
            var fillImg = timeSliderFill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            var slider = timeSliderBg.AddComponent<Slider>();
            slider.fillRect = timeSliderFill.GetComponent<RectTransform>();
            slider.value = 0f;
            _musicTimeSlider = slider;

            GameObject timeTxtObj = CreateTextObject("TimeText", playerCard.transform, new Vector2(0.06f, 0.21f), new Vector2(0.94f, 0.25f),
                                                     Vector2.zero, Vector2.zero,
                                                     "00:00 / 00:00  |  STAGE TOWER SPEAKERS [FL & FR]", 18, FontStyles.Normal,
                                                     TextAlignmentOptions.Center, new Color(0.6f, 0.85f, 0.95f));
            _musicTimeText = timeTxtObj.GetComponent<TextMeshProUGUI>();

            // Transport Row: [PREV] [PLAY/PAUSE] [NEXT] [STOP]
            CreateButton("Btn_Prev", playerCard.transform, new Vector2(0.06f, 0.11f), new Vector2(0.25f, 0.19f),
                         "[<<] PREV", new Color(0.08f, 0.25f, 0.40f), OnPrevMusicClicked);

            var playBtn = CreateButton("Btn_MainPlay", playerCard.transform, new Vector2(0.28f, 0.11f), new Vector2(0.52f, 0.19f),
                                       "> PLAY", new Color(0.08f, 0.60f, 0.35f), OnToggleMusicClicked);
            _musicPlayBtnText = playBtn.GetComponentInChildren<TextMeshProUGUI>();

            CreateButton("Btn_Next", playerCard.transform, new Vector2(0.55f, 0.11f), new Vector2(0.74f, 0.19f),
                         "[>>] NEXT", new Color(0.08f, 0.25f, 0.40f), OnNextMusicClicked);

            CreateButton("Btn_Stop", playerCard.transform, new Vector2(0.77f, 0.11f), new Vector2(0.94f, 0.19f),
                         "[STOP] STOP", new Color(0.40f, 0.12f, 0.15f), OnStopMusicClicked);

            // Volume Row
            CreateButton("Btn_VolM", playerCard.transform, new Vector2(0.06f, 0.03f), new Vector2(0.18f, 0.09f),
                         "[-] -", new Color(0.06f, 0.15f, 0.25f), () => AdjustSpeakerVolume(-0.1f));

            CreateTextObject("VolLabel", playerCard.transform, new Vector2(0.20f, 0.03f), new Vector2(0.80f, 0.09f),
                             Vector2.zero, Vector2.zero,
                             "ROOM SPEAKER VOLUME: 85%  |  SURROUND 3D SPATIAL BLEND", 18, FontStyles.Bold,
                             TextAlignmentOptions.Center, new Color(0.7f, 0.9f, 1f));

            CreateButton("Btn_VolP", playerCard.transform, new Vector2(0.82f, 0.03f), new Vector2(0.94f, 0.09f),
                         "[+] +", new Color(0.06f, 0.15f, 0.25f), () => AdjustSpeakerVolume(0.1f));

            // Right Card: 5 Peaceful Songs Playlist
            GameObject listCard = CreateCardPanel("Card_Playlist", parent, new Vector2(0.515f, 0.04f), new Vector2(0.98f, 0.96f),
                                                  "5 PEACEFUL MP3 TRACKS (S:\\arvr project\\songs)", new Color(0f, 0.8f, 1f));

            CreateTextObject("PlaylistSub", listCard.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(24f, -78f), new Vector2(-24f, -48f),
                             "SELECT ANY TRACK TO PLAY THROUGH ROOM SPEAKERS & ACTIVATE PEACEFUL LIGHTING:", 20, FontStyles.Bold,
                             TextAlignmentOptions.MidlineLeft, new Color(0.8f, 0.9f, 1f));

            string[] trackTitles = new string[]
            {
                "Atlasaudio - Relaxing",
                "Leberch - Relaxing",
                "The Mountain - Relaxing",
                "Velariomusic - Piano Relaxing",
                "Velariomusic - Relaxing"
            };

            string[] trackSubtitles = new string[]
            {
                "Ambient Harmony  |  2:57  |  atlasaudio-relaxing-621597.mp3",
                "Deep Serenity & Soft Synth  |  4:12  |  leberch-relaxing-576861.mp3",
                "Nature Atmosphere & Wind  |  3:45  |  the_mountain-relaxing-492810.mp3",
                "Acoustic Grand Piano  |  3:38  |  velariomusic-piano-relaxing-606301.mp3",
                "Tranquil Lofi Ambient Breeze  |  3:05  |  velariomusic-relaxing-582876.mp3"
            };

            float startY = -95f;
            float itemH = 105f;
            float spacing = 16f;

            for (int i = 0; i < 5; i++)
            {
                int trackIndex = i;
                float topOffset = startY - i * (itemH + spacing);
                float botOffset = topOffset - itemH;

                GameObject itemObj = CreateUIObject($"TrackItem_{i}", listCard.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                    new Vector2(24f, botOffset), new Vector2(-24f, topOffset),
                                                    new Color(0.04f, 0.08f, 0.14f, 0.85f));
                _trackItemBgs[i] = itemObj.GetComponent<Image>();

                // Clickable overlay button for the whole card
                var itemBtn = itemObj.AddComponent<Button>();
                itemBtn.transition = Selectable.Transition.ColorTint;
                ColorBlock cb = itemBtn.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
                cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                itemBtn.colors = cb;
                itemBtn.onClick.AddListener(() => PlayTrackByIndex(trackIndex));

                // Left track number badge
                GameObject numBadge = CreateUIObject("NumBadge", itemObj.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                                     new Vector2(16f, -32f), new Vector2(80f, 32f), new Color(0.08f, 0.18f, 0.30f, 0.95f));
                CreateTextObject("NumText", numBadge.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                                 $"0{i + 1}", 26, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0f, 1f, 0.9f));

                // Track Title & details
                CreateTextObject("TrackTitle", itemObj.transform, new Vector2(0f, 0.5f), new Vector2(0.70f, 0.5f),
                                 new Vector2(96f, -2f), new Vector2(0f, 32f),
                                 $"<b>{trackTitles[i]}</b>", 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

                CreateTextObject("TrackSub", itemObj.transform, new Vector2(0f, 0.5f), new Vector2(0.70f, 0.5f),
                                 new Vector2(96f, -32f), new Vector2(0f, -2f),
                                 trackSubtitles[i], 18, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, new Color(0.6f, 0.8f, 0.95f));

                // Right Status Badge
                GameObject statusBadge = CreateUIObject("StatusBadge", itemObj.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                                        new Vector2(-225f, -24f), new Vector2(-125f, 24f), new Color(0.06f, 0.12f, 0.20f, 0.9f));
                var stTmp = CreateTextObject("StatusText", statusBadge.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                                             "<color=#557799>READY</color>", 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
                _trackItemStatusTexts[i] = stTmp.GetComponent<TextMeshProUGUI>();

                // Right Play Button
                CreateButton($"Btn_Play_{i}", itemObj.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                             new Vector2(-110f, -24f), new Vector2(-16f, 24f), "> PLAY", new Color(0.08f, 0.55f, 0.35f), () => PlayTrackByIndex(trackIndex));
            }
        }

        public void OnToggleMusicClicked()
        {
            if (VRRemoteDesktop.Environment.RoomSpeakerController.Instance != null)
            {
                VRRemoteDesktop.Environment.RoomSpeakerController.Instance.TogglePlayPause();
            }
            VRRemoteDesktop.Environment.RoomLightingController.Instance?.ApplyZenTranquilLighting();
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();
            UpdateMusicUI();
        }

        public void OnNextMusicClicked()
        {
            if (VRRemoteDesktop.Environment.RoomSpeakerController.Instance != null)
            {
                VRRemoteDesktop.Environment.RoomSpeakerController.Instance.PlayNextSong();
            }
            VRRemoteDesktop.Environment.RoomLightingController.Instance?.ApplyZenTranquilLighting();
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayConnectChime();
            UpdateMusicUI();
        }

        public void OnPrevMusicClicked()
        {
            if (VRRemoteDesktop.Environment.RoomSpeakerController.Instance != null)
            {
                VRRemoteDesktop.Environment.RoomSpeakerController.Instance.PlayPreviousSong();
            }
            VRRemoteDesktop.Environment.RoomLightingController.Instance?.ApplyZenTranquilLighting();
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayConnectChime();
            UpdateMusicUI();
        }

        public void OnStopMusicClicked()
        {
            if (VRRemoteDesktop.Environment.RoomSpeakerController.Instance != null)
            {
                VRRemoteDesktop.Environment.RoomSpeakerController.Instance.StopMusic();
            }
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();
            UpdateMusicUI();
        }

        public void PlayTrackByIndex(int index)
        {
            if (VRRemoteDesktop.Environment.RoomSpeakerController.Instance != null)
            {
                VRRemoteDesktop.Environment.RoomSpeakerController.Instance.PlaySongAtIndex(index);
            }
            VRRemoteDesktop.Environment.RoomLightingController.Instance?.ApplyZenTranquilLighting();
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayConnectChime();
            UpdateMusicUI();
        }

        public void AdjustSpeakerVolume(float delta)
        {
            var speaker = VRRemoteDesktop.Environment.RoomSpeakerController.Instance;
            if (speaker != null)
            {
                speaker.SpeakerVolume = Mathf.Clamp01(speaker.SpeakerVolume + delta);
            }
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();
            UpdateMusicUI();
        }

        private void UpdateMusicUI()
        {
            var speaker = VRRemoteDesktop.Environment.RoomSpeakerController.Instance;
            bool isPlaying = speaker != null && speaker.IsPlaying;

            if (_headerPlayBtnText != null)
            {
                _headerPlayBtnText.text = isPlaying ? "[||] PAUSE MUSIC" : "> PLAY MUSIC (PEACEFUL)";
            }
            if (_headerPlayBtnBg != null)
            {
                _headerPlayBtnBg.color = isPlaying ? new Color(0.60f, 0.35f, 0.05f, 1f) : new Color(0.08f, 0.60f, 0.35f, 1f);
            }

            if (_musicPlayBtnText != null)
            {
                _musicPlayBtnText.text = isPlaying ? "[||] PAUSE" : "> PLAY";
            }

            if (_musicNowPlayingTitle != null)
            {
                _musicNowPlayingTitle.text = isPlaying
                    ? $"<color=#00ffff>NOW PLAYING:</color> {speaker.GetCleanTitleAtIndex(speaker.CurrentSongIndex).ToUpper()}"
                    : "<color=#88aacc>STANDBY | PRESS PLAY TO START PEACEFUL MUSIC</color>";
            }

            int currentIdx = speaker != null ? speaker.CurrentSongIndex : -1;
            for (int i = 0; i < _trackItemBgs.Length; i++)
            {
                if (_trackItemBgs[i] == null) continue;
                bool isThisTrack = (i == currentIdx && isPlaying);
                _trackItemBgs[i].color = isThisTrack 
                    ? new Color(0.08f, 0.32f, 0.22f, 0.95f) 
                    : new Color(0.04f, 0.08f, 0.14f, 0.85f);

                if (_trackItemStatusTexts[i] != null)
                {
                    _trackItemStatusTexts[i].text = isThisTrack 
                        ? "<color=#00ff88>> PLAYING</color>" 
                        : "<color=#557799>READY</color>";
                }
            }
        }

        private string FormatTime(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds)) seconds = 0f;
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins:00}:{secs:00}";
        }
        #endregion
    }
}
