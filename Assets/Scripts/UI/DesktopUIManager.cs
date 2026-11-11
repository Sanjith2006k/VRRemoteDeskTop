using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace VRRemoteDesktop.UI
{
    /// <summary>
    /// Master controller for the Phase 4 Futuristic Remote Desktop UI.
    /// Manages the top status bar, real-time clock, simulated telemetry (CPU/RAM/GPU/Network),
    /// desktop application shortcuts, bottom dock, and theme distribution.
    /// </summary>
    public class DesktopUIManager : MonoBehaviour
    {
        public static DesktopUIManager Instance { get; private set; }

        [Header("Top Status Bar")]
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _dateText;
        [SerializeField] private TextMeshProUGUI _connectionText;
        [SerializeField] private TextMeshProUGUI _latencyText;
        [SerializeField] private TextMeshProUGUI _computerNameText;
        [SerializeField] private TextMeshProUGUI _networkStatusText;
        [SerializeField] private TextMeshProUGUI _resolutionText;
        [SerializeField] private Button _settingsButton;

        [Header("System Telemetry Widget")]
        [SerializeField] private TextMeshProUGUI _cpuText;
        [SerializeField] private Image _cpuBarFill;
        [SerializeField] private TextMeshProUGUI _ramText;
        [SerializeField] private Image _ramBarFill;
        [SerializeField] private TextMeshProUGUI _gpuText;
        [SerializeField] private Image _gpuBarFill;
        [SerializeField] private TextMeshProUGUI _networkIOText;

        [Header("Background & Frame Panels")]
        [SerializeField] private Image _background;
        [SerializeField] private Image _topBarBackground;
        [SerializeField] private Image _dockBackground;
        [SerializeField] private Image _telemetryWidgetBg;
        [SerializeField] private Image _hostInfoWidgetBg;

        private float _telemetryTimer = 0f;
        private float _baseCpu = 14f;
        private float _baseGpu = 26f;
        private float _baseRamGB = 5.8f;
        private int _baseLatency = 21;

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
            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.OnThemeChanged += ApplyTheme;
                ApplyTheme();
            }

            // Initial Simulation State
            SetComputerName("DESKTOP-VR-LOCAL [SIMULATED]");
            SetResolution("1920 x 1080 @ 90Hz");
            SetNetworkStatus("INTERNAL PIPELINE");
            SetConnectionStatus("SIMULATION READY", true);
            SetLatency(1);
            UpdateTelemetry();

            EnforceEnhancedReadability();

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(() => LaunchApp("Settings"));
            }
        }

        /// <summary>
        /// Scales and optimizes font sizes across the Desktop UI for high readability in VR.
        /// <summary>
        /// Scales and optimizes font sizes across the Desktop UI for high readability in VR (18-24pt).
        /// </summary>
        private void EnforceEnhancedReadability()
        {
            // 1. TopBar Layout & Font Sizes
            if (_topBarBackground != null)
            {
                RectTransform tbRt = _topBarBackground.rectTransform;
                if (tbRt != null) tbRt.sizeDelta = new Vector2(tbRt.sizeDelta.x, 62f);
            }

            // Left Section
            if (_computerNameText != null)
            {
                _computerNameText.fontSize = 20f;
                _computerNameText.fontStyle = FontStyles.Bold;
                var rt = _computerNameText.rectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(230f, 40f);
            }
            if (_networkStatusText != null)
            {
                _networkStatusText.fontSize = 18f;
                _networkStatusText.fontStyle = FontStyles.Bold;
                var rt = _networkStatusText.rectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(170f, 40f);
            }
            if (_resolutionText != null)
            {
                _resolutionText.fontSize = 17f;
                var rt = _resolutionText.rectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(190f, 40f);
            }

            // Brand / OS Title in center
            Transform centerTr = transform.Find("TopBar/CenterSection/BrandTitle");
            if (centerTr != null)
            {
                var titleTmp = centerTr.GetComponent<TextMeshProUGUI>();
                if (titleTmp != null)
                {
                    titleTmp.fontSize = 20f;
                    titleTmp.fontStyle = FontStyles.Bold;
                }
            }

            // Right Section
            if (_latencyText != null)
            {
                _latencyText.fontSize = 18f;
                _latencyText.fontStyle = FontStyles.Bold;
                var rt = _latencyText.rectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(110f, 40f);
            }
            if (_connectionText != null)
            {
                _connectionText.fontSize = 18f;
                _connectionText.fontStyle = FontStyles.Bold;
                // Widen badge parent so "CONNECTED (ENCRYPTED)" doesn't overlap or get clipped
                RectTransform badgeRt = _connectionText.transform.parent as RectTransform;
                if (badgeRt != null)
                {
                    badgeRt.sizeDelta = new Vector2(250f, 36f);
                }
            }
            if (_timeText != null)
            {
                _timeText.fontSize = 22f;
                _timeText.fontStyle = FontStyles.Bold;
                var rt = _timeText.rectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(120f, 40f);
            }
            if (_dateText != null)
            {
                _dateText.fontSize = 18f;
                var rt = _dateText.rectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(130f, 40f);
            }
            if (_settingsButton != null)
            {
                var sRt = _settingsButton.GetComponent<RectTransform>();
                if (sRt != null) sRt.sizeDelta = new Vector2(110f, 36f);
                var sTmp = _settingsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (sTmp != null)
                {
                    sTmp.fontSize = 16f;
                    sTmp.fontStyle = FontStyles.Bold;
                }
            }

            // StationBadge
            Transform stationTr = transform.Find("TopBar/LeftSection/StationBadge");
            if (stationTr != null)
            {
                var sbRt = stationTr as RectTransform;
                if (sbRt != null) sbRt.sizeDelta = new Vector2(130f, 36f);
                var sTmp = stationTr.GetComponentInChildren<TextMeshProUGUI>();
                if (sTmp != null)
                {
                    sTmp.fontSize = 17f;
                    sTmp.fontStyle = FontStyles.Bold;
                }
            }

            // 2. Desktop Application Grid (Left Column Icons)
            Transform gridTr = transform.Find("DesktopGrid");
            if (gridTr != null)
            {
                var gridRt = gridTr as RectTransform;
                if (gridRt != null)
                {
                    gridRt.sizeDelta = new Vector2(320f, 550f);
                    var vlg = gridTr.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                    if (vlg != null) vlg.enabled = false;
                }

                for (int i = 0; i < gridTr.childCount; i++)
                {
                    Transform iconItem = gridTr.GetChild(i);
                    var itemRt = iconItem as RectTransform;
                    if (itemRt != null) itemRt.sizeDelta = new Vector2(140f, 126f);

                    Transform box = iconItem.Find("IconBox");
                    if (box != null)
                    {
                        var boxRt = box as RectTransform;
                        if (boxRt != null) boxRt.sizeDelta = new Vector2(70f, 70f);
                        var symTmp = box.GetComponentInChildren<TextMeshProUGUI>();
                        if (symTmp != null)
                        {
                            symTmp.fontSize = 24f;
                            symTmp.fontStyle = FontStyles.Bold;
                        }
                    }

                    Transform titleTr = iconItem.Find("Title");
                    if (titleTr != null)
                    {
                        var tRt = titleTr as RectTransform;
                        if (tRt != null)
                        {
                            tRt.anchoredPosition = new Vector2(0f, 26f);
                            tRt.sizeDelta = new Vector2(20f, 26f);
                        }
                        var tTmp = titleTr.GetComponent<TextMeshProUGUI>();
                        if (tTmp != null)
                        {
                            tTmp.fontSize = 20f;
                            tTmp.fontStyle = FontStyles.Bold;
                        }
                    }

                    Transform subTr = iconItem.Find("Sub");
                    if (subTr != null)
                    {
                        var sRt = subTr as RectTransform;
                        if (sRt != null)
                        {
                            sRt.anchoredPosition = new Vector2(0f, 2f);
                            sRt.sizeDelta = new Vector2(20f, 22f);
                        }
                        var sTmp = subTr.GetComponent<TextMeshProUGUI>();
                        if (sTmp != null)
                        {
                            sTmp.fontSize = 16f;
                        }
                    }
                }
            }

            // 3. Right Telemetry Widget (System Performance)
            Transform metricsTr = transform.Find("SystemMetricsCard");
            if (metricsTr != null)
            {
                var mRt = metricsTr as RectTransform;
                if (mRt != null) mRt.sizeDelta = new Vector2(380f, 290f);

                Transform header = metricsTr.Find("Header");
                if (header != null)
                {
                    var hTmp = header.GetComponentInChildren<TextMeshProUGUI>();
                    if (hTmp != null)
                    {
                        hTmp.fontSize = 20f;
                        hTmp.fontStyle = FontStyles.Bold;
                    }
                }

                foreach (var tmp in metricsTr.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    if (tmp.name == "Label")
                    {
                        tmp.fontSize = 18f;
                        tmp.fontStyle = FontStyles.Bold;
                    }
                    else if (tmp.name == "Value")
                    {
                        tmp.fontSize = 20f;
                        tmp.fontStyle = FontStyles.Bold;
                    }
                }
            }

            // 4. Remote Link Profile Card
            Transform hostCardTr = transform.Find("HostInfoCard");
            if (hostCardTr != null)
            {
                var hRt = hostCardTr as RectTransform;
                if (hRt != null)
                {
                    hRt.anchoredPosition = new Vector2(-40f, -380f);
                    hRt.sizeDelta = new Vector2(380f, 240f);
                }

                Transform hh = hostCardTr.Find("HostHeader");
                if (hh != null)
                {
                    var hhTmp = hh.GetComponentInChildren<TextMeshProUGUI>();
                    if (hhTmp != null)
                    {
                        hhTmp.fontSize = 20f;
                        hhTmp.fontStyle = FontStyles.Bold;
                    }
                }

                for (int i = 0; i < 5; i++)
                {
                    Transform dTr = hostCardTr.Find($"Detail_{i}");
                    if (dTr != null)
                    {
                        var dRt = dTr as RectTransform;
                        if (dRt != null)
                        {
                            dRt.anchoredPosition = new Vector2(0f, -48f - (i * 32f));
                            dRt.sizeDelta = new Vector2(-28f, 28f);
                        }
                        var dTmp = dTr.GetComponentInChildren<TextMeshProUGUI>();
                        if (dTmp != null)
                        {
                            dTmp.fontSize = 17f;
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.OnThemeChanged -= ApplyTheme;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            // Clock & Date updates
            DateTime now = DateTime.Now;
            if (_timeText != null)
            {
                _timeText.text = now.ToString("HH:mm:ss");
            }
            if (_dateText != null)
            {
                _dateText.text = now.ToString("ddd, MMM dd").ToUpper();
            }

            // Realistic slight telemetry fluctuation
            _telemetryTimer += Time.deltaTime;
            if (_telemetryTimer >= 1.2f)
            {
                _telemetryTimer = 0f;
                UpdateTelemetry();
            }
        }

        private void UpdateTelemetry()
        {
            // Subtle realistic jitter
            float cpu = Mathf.Clamp(_baseCpu + UnityEngine.Random.Range(-3f, 6f), 8f, 95f);
            float ram = Mathf.Clamp(_baseRamGB + UnityEngine.Random.Range(-0.2f, 0.4f), 4f, 15.5f);
            float gpu = Mathf.Clamp(_baseGpu + UnityEngine.Random.Range(-4f, 8f), 12f, 98f);
            int latency = Mathf.Clamp(_baseLatency + UnityEngine.Random.Range(-3, 4), 14, 45);
            float netDown = UnityEngine.Random.Range(48.2f, 58.6f);
            float netUp = UnityEngine.Random.Range(1.4f, 2.8f);

            SetLatency(latency);

            if (_cpuText != null) _cpuText.text = $"{cpu:0}%";
            if (_cpuBarFill != null) _cpuBarFill.fillAmount = cpu / 100f;

            if (_ramText != null) _ramText.text = $"{ram:0.1} / 16.0 GB ({((ram / 16f) * 100f):0}%)";
            if (_ramBarFill != null) _ramBarFill.fillAmount = ram / 16f;

            if (_gpuText != null) _gpuText.text = $"{gpu:0}%";
            if (_gpuBarFill != null) _gpuBarFill.fillAmount = gpu / 100f;

            if (_networkIOText != null)
            {
                _networkIOText.text = $"NETWORK I/O:  UP: {netUp:0.1} MB/s   DN: {netDown:0.1} MB/s";
            }
        }

        public void ApplyTheme()
        {
            var theme = ThemeManager.Instance?.CurrentTheme;
            if (theme == null) return;

            if (_background != null) _background.color = theme.BackgroundColor;

            Color barColor = new Color(theme.BackgroundColor.r * 1.3f, theme.BackgroundColor.g * 1.3f, theme.BackgroundColor.b * 1.4f, 0.92f);
            if (_topBarBackground != null) _topBarBackground.color = barColor;
            if (_dockBackground != null) _dockBackground.color = barColor;

            Color cardBg = new Color(theme.BackgroundColor.r * 1.5f, theme.BackgroundColor.g * 1.5f, theme.BackgroundColor.b * 1.6f, 0.88f);
            if (_telemetryWidgetBg != null) _telemetryWidgetBg.color = cardBg;
            if (_hostInfoWidgetBg != null) _hostInfoWidgetBg.color = cardBg;

            if (_timeText != null) _timeText.color = theme.TextPrimary;
            if (_dateText != null) _dateText.color = theme.TextSecondary;
            if (_connectionText != null) _connectionText.color = theme.SuccessColor;
            if (_latencyText != null) _latencyText.color = theme.SuccessColor;
            if (_computerNameText != null) _computerNameText.color = theme.TextPrimary;
            if (_networkStatusText != null) _networkStatusText.color = theme.PrimaryColor;
            if (_resolutionText != null) _resolutionText.color = theme.TextSecondary;
        }

        public void SetConnectionStatus(string status, bool isConnected)
        {
            if (_connectionText != null)
            {
                _connectionText.text = status;
                if (ThemeManager.Instance?.CurrentTheme != null)
                {
                    _connectionText.color = isConnected ?
                        ThemeManager.Instance.CurrentTheme.SuccessColor :
                        ThemeManager.Instance.CurrentTheme.ErrorColor;
                }
            }
        }

        public void SetLatency(int ms)
        {
            if (_latencyText != null)
            {
                _latencyText.text = $"RTT: {ms} ms";
            }
        }

        public void SetComputerName(string name)
        {
            if (_computerNameText != null) _computerNameText.text = name;
        }

        public void SetNetworkStatus(string status)
        {
            if (_networkStatusText != null) _networkStatusText.text = status;
        }

        public void SetResolution(string res)
        {
            if (_resolutionText != null) _resolutionText.text = res;
        }

        public void LaunchApp(string appName)
        {
            Debug.Log($"[DesktopUIManager] Launching application: {appName}");
            var winMgr = VRRemoteDesktop.UI.Windows.DesktopWindowManager.Instance;
            if (winMgr == null)
            {
                winMgr = FindAnyObjectByType<VRRemoteDesktop.UI.Windows.DesktopWindowManager>();
            }

            if (winMgr != null)
            {
                winMgr.OpenWindow(appName);
            }
            else
            {
                Debug.LogWarning($"[DesktopUIManager] DesktopWindowManager not available to launch {appName}");
            }
        }
    }
}
