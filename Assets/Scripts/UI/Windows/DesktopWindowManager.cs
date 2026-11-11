using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.UI;
using VRRemoteDesktop.UI.Keyboard;
using VRRemoteDesktop.Remote;

namespace VRRemoteDesktop.UI.Windows

{
    /// <summary>
    /// Master coordinator for Phase 7 Desktop Window System.
    /// Manages window creation, layout, depth ordering (focus), opening/closing,
    /// and dispatches window launches from DesktopUIManager and desktop/dock shortcuts.
    /// </summary>

    public class DesktopWindowManager : MonoBehaviour

    {
        private static DesktopWindowManager _instance;

        public static DesktopWindowManager Instance

        {
            get

            {
                if (_instance == null)

                {
                    _instance = Object.FindAnyObjectByType<DesktopWindowManager>();
                }

                return _instance;
            }

            private set => _instance = value;
        }

        [Header("Target Canvas")]
        [SerializeField] private RectTransform _canvasRect;

        [SerializeField] private RectTransform _windowsContainer;

        [Header("Pre-configured Windows (Optional)")]
        [SerializeField] private List<DesktopWindow> _managedWindows = new List<DesktopWindow>();

        private Dictionary<string, DesktopWindow> _windowMap = new Dictionary<string, DesktopWindow>(System.StringComparer.OrdinalIgnoreCase);

        private TMP_FontAsset _defaultFont;

        private DesktopWindow _focusedWindow;

        public DesktopWindow FocusedWindow => _focusedWindow;

        public event System.Action<DesktopWindow> OnWindowOpened;

        public event System.Action<DesktopWindow> OnWindowClosed;

        private void Awake()

        {
            if (_instance != null && _instance != this)

            {
                Destroy(this);

                return;
            }

            _instance = this;

            ResolveContainers();

            LoadDefaultFont();

            RegisterExistingWindows();

            BuildMissingWindows();

            EnsureVirtualKeyboard();
        }

        private void Start()

        {
            EnsureVirtualKeyboard();

            EnsureDesktopShortcuts();

            // Close all windows initially so desktop is clean

            foreach (var w in _windowMap.Values)

            {
                if (w != null) w.Close();
            }
        }

        private void OnDestroy()

        {
            if (Instance == this) Instance = null;
        }

        private void ResolveContainers()

        {
            if (_canvasRect == null)

            {
                var canvasObj = GameObject.Find("DesktopCanvas");

                if (canvasObj != null) _canvasRect = canvasObj.GetComponent<RectTransform>();
            }

            if (_windowsContainer == null && _canvasRect != null)

            {
                Transform containerTr = _canvasRect.Find("WindowsContainer");

                if (containerTr == null)

                {
                    GameObject contObj = new GameObject("WindowsContainer", typeof(RectTransform));

                    contObj.transform.SetParent(_canvasRect, false);

                    _windowsContainer = contObj.GetComponent<RectTransform>();

                    _windowsContainer.anchorMin = Vector2.zero;

                    _windowsContainer.anchorMax = Vector2.one;

                    _windowsContainer.offsetMin = Vector2.zero;

                    _windowsContainer.offsetMax = Vector2.zero;

                    // Place container right before VRCursor so cursor remains on top

                    Transform cursorTr = _canvasRect.Find("VRCursor");

                    if (cursorTr != null)

                    {
                        int cursorIdx = cursorTr.GetSiblingIndex();

                        _windowsContainer.SetSiblingIndex(cursorIdx);
                    }
                }

                else

                {
                    _windowsContainer = containerTr.GetComponent<RectTransform>();
                }
            }
        }

        private void LoadDefaultFont()

        {
#if UNITY_EDITOR

            _defaultFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

#endif

            if (_defaultFont == null)

            {
                _defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            if (_defaultFont == null)

            {
                var anyTmp = FindAnyObjectByType<TextMeshProUGUI>();

                if (anyTmp != null) _defaultFont = anyTmp.font;
            }
        }

        private void RegisterExistingWindows()

        {
            foreach (var win in _managedWindows)

            {
                if (win != null && !_windowMap.ContainsKey(win.AppName))

                {
                    _windowMap[win.AppName] = win;

                    win.OnWindowFocused += HandleWindowFocused;
                }
            }
        }

        private void BuildMissingWindows()

        {
            if (_windowsContainer == null) return;

            string[] apps = { "Files", "Terminal", "Browser", "Settings", "SysMonitor", "Notepad", "Connection", "LiveCam" };

            foreach (var app in apps)

            {
                if (!_windowMap.ContainsKey(app))

                {
                    DesktopWindow win = CreateAppWindow(app);

                    if (win != null)

                    {
                        _windowMap[app] = win;

                        _managedWindows.Add(win);

                        win.OnWindowFocused += HandleWindowFocused;
                    }
                }
            }

            // Aliases

            if (_windowMap.ContainsKey("SysMonitor") && !_windowMap.ContainsKey("System Monitor"))

            {
                _windowMap["System Monitor"] = _windowMap["SysMonitor"];
            }

            if (_windowMap.ContainsKey("Connection"))

            {
                _windowMap["RemoteLink"] = _windowMap["Connection"];

                _windowMap["Remote Desktop"] = _windowMap["Connection"];

                _windowMap["Host"] = _windowMap["Connection"];
            }

            if (_windowMap.ContainsKey("Terminal") && !_windowMap.ContainsKey("Keyboard"))

            {
                _windowMap["Keyboard"] = _windowMap["Terminal"];
            }

            if (_windowMap.ContainsKey("Notepad"))

            {
                _windowMap["Notes"] = _windowMap["Notepad"];

                _windowMap["Editor"] = _windowMap["Notepad"];

                _windowMap["TextEditor"] = _windowMap["Notepad"];
            }

            if (_windowMap.ContainsKey("LiveCam"))

            {
                _windowMap["Camera"] = _windowMap["LiveCam"];

                _windowMap["Webcam"] = _windowMap["LiveCam"];

                _windowMap["CCTV"] = _windowMap["LiveCam"];
            }
        }

        public void OpenWindow(string appName)

        {
            if (string.IsNullOrEmpty(appName)) return;

            string clean = appName.Replace("Icon_", "").Replace("Dock_", "").Trim();

            if (clean.Equals("Home", System.StringComparison.OrdinalIgnoreCase) ||

                clean.Equals("Explorer", System.StringComparison.OrdinalIgnoreCase) ||

                clean.Equals("File", System.StringComparison.OrdinalIgnoreCase) ||

                clean.Equals("Documents", System.StringComparison.OrdinalIgnoreCase))

            {
                clean = "Files";
            }

            if (clean.Equals("RemoteLink", System.StringComparison.OrdinalIgnoreCase)) clean = "Terminal";

            if (clean.Equals("Connection", System.StringComparison.OrdinalIgnoreCase)) clean = "SysMonitor";

            if (clean.Equals("Camera", System.StringComparison.OrdinalIgnoreCase) ||

                clean.Equals("Webcam", System.StringComparison.OrdinalIgnoreCase) ||

                clean.Equals("CCTV", System.StringComparison.OrdinalIgnoreCase) ||

                clean.Equals("Surveillance", System.StringComparison.OrdinalIgnoreCase))

            {
                clean = "LiveCam";
            }

            if (clean.Equals("Notes", System.StringComparison.OrdinalIgnoreCase) ||

                clean.Equals("Editor", System.StringComparison.OrdinalIgnoreCase) ||

                clean.Equals("TextEditor", System.StringComparison.OrdinalIgnoreCase))

            {
                clean = "Notepad";
            }

            if (clean.Equals("Keyboard", System.StringComparison.OrdinalIgnoreCase))

            {
                EnsureVirtualKeyboard();

                if (VirtualKeyboard.Instance != null)

                {
                    VirtualKeyboard.Instance.Toggle();

                    return;
                }
            }

            if (clean.Equals("Browser", System.StringComparison.OrdinalIgnoreCase))

            {
                if (LeftDashboardUI.Instance != null)

                {
                    LeftDashboardUI.Instance.ShowBrowser();
                }
            }

            ResolveContainers();

            // Find window case-insensitively

            DesktopWindow win = null;

            foreach (var kvp in _windowMap)

            {
                if (kvp.Key.Equals(clean, System.StringComparison.OrdinalIgnoreCase) && kvp.Value != null)

                {
                    win = kvp.Value;

                    break;
                }
            }

            if (win == null)

            {
                foreach (var w in _managedWindows)

                {
                    if (w != null && (w.AppName.Equals(clean, System.StringComparison.OrdinalIgnoreCase) || w.gameObject.name.Contains(clean)))

                    {
                        win = w;

                        _windowMap[clean] = win;

                        break;
                    }
                }
            }

            if (win == null)

            {
                var allWins = GetComponentsInChildren<DesktopWindow>(true);

                foreach (var w in allWins)

                {
                    if (w != null && (w.AppName.Equals(clean, System.StringComparison.OrdinalIgnoreCase) || w.gameObject.name.Contains(clean)))

                    {
                        win = w;

                        _windowMap[clean] = win;

                        if (!_managedWindows.Contains(win)) _managedWindows.Add(win);

                        break;
                    }
                }
            }

            // Create on demand if not found!

            if (win == null)

            {
                win = CreateAppWindow(clean);

                if (win != null)

                {
                    _windowMap[clean] = win;

                    if (!_managedWindows.Contains(win)) _managedWindows.Add(win);

                    win.OnWindowFocused += HandleWindowFocused;
                }
            }

            if (win != null)

            {
                Debug.Log($"[DesktopWindowManager] Opening window: {win.AppName}");

                win.gameObject.SetActive(true);

                if (!win.IsOpen)

                {
                    win.Open();

                    OnWindowOpened?.Invoke(win);
                }

                else if (win.IsMinimized)

                {
                    win.Restore();
                }

                else

                {
                    win.Focus();
                }

                BringToFront(win);
            }

            else

            {
                Debug.LogWarning($"[DesktopWindowManager] Unknown app requested: {appName} (clean: {clean})");
            }
        }

        public void CloseWindow(string appName)

        {
            if (_windowMap.TryGetValue(appName, out DesktopWindow win))

            {
                win.Close();

                OnWindowClosed?.Invoke(win);

                if (_focusedWindow == win) _focusedWindow = null;
            }
        }

        public void ToggleWindow(string appName)

        {
            if (_windowMap.TryGetValue(appName, out DesktopWindow win))

            {
                if (win.IsOpen && !win.IsMinimized)

                {
                    win.Close();

                    OnWindowClosed?.Invoke(win);

                    if (_focusedWindow == win) _focusedWindow = null;
                }

                else

                {
                    OpenWindow(appName);
                }
            }
        }

        public void BringToFront(DesktopWindow window)

        {
            if (window != null)

            {
                _focusedWindow = window;

                if (_windowsContainer != null)

                {
                    _windowsContainer.SetAsLastSibling();
                }

                window.transform.SetAsLastSibling();

                // Ensure cursor stays above all windows

                if (_canvasRect != null)

                {
                    Transform cursorTr = _canvasRect.Find("VRCursor");

                    if (cursorTr != null)

                    {
                        cursorTr.SetAsLastSibling();
                    }
                }
            }
        }

        private void HandleWindowFocused(DesktopWindow window)

        {
            _focusedWindow = window;

            BringToFront(window);
        }

        public void EnsureVirtualKeyboard()

        {
            if (MockRemoteInputProvider.Instance == null)

            {
                var providerObj = new GameObject("MockRemoteInputProvider", typeof(MockRemoteInputProvider));

                DontDestroyOnLoad(providerObj);
            }

            if (VirtualKeyboard.Instance == null && _canvasRect != null)

            {
                Transform kbTr = _canvasRect.Find("VirtualKeyboard");

                if (kbTr == null)

                {
                    GameObject kbObj = new GameObject("VirtualKeyboard", typeof(RectTransform), typeof(CanvasGroup), typeof(VirtualKeyboard));

                    kbObj.transform.SetParent(_canvasRect, false);

                    // Place keyboard right before cursor so cursor is on top

                    Transform cursorTr = _canvasRect.Find("VRCursor");

                    if (cursorTr != null)

                    {
                        kbObj.transform.SetSiblingIndex(cursorTr.GetSiblingIndex());
                    }
                }
            }
        }

        // =========================================================================

        // PROCEDURAL PROCEDURES FOR BUILDING THE 5 CYBERPUNK WINDOWS

        // =========================================================================

        private DesktopWindow CreateAppWindow(string appType)

        {
            Vector2 size = new Vector2(800, 520);

            Vector2 pos = Vector2.zero;

            string title = "Application";

            switch (appType)

            {
                case "Files":

                    size = new Vector2(820, 500);

                    pos = new Vector2(-40, 20);

                    title = "FILES - Workspace Explorer";

                    break;

                case "Terminal":

                    size = new Vector2(780, 480);

                    pos = new Vector2(30, -30);

                    title = "CYBER TERMINAL - vsh shell";

                    break;

                case "Browser":

                    size = new Vector2(860, 540);

                    pos = new Vector2(0, 0);

                    title = "QUANTUM BROWSER - Web Navigator";

                    break;

                case "Settings":

                    size = new Vector2(760, 480);

                    pos = new Vector2(50, 40);

                    title = "SYSTEM SETTINGS - Configuration";

                    break;

                case "SysMonitor":

                    size = new Vector2(820, 510);

                    pos = new Vector2(-20, -10);

                    title = "SYSTEM MONITOR - Telemetry & Diagnostics";

                    break;

                case "Notepad":

                    size = new Vector2(820, 520);

                    pos = new Vector2(20, -10);

                    title = "CYBER NOTEPAD - notes.txt";

                    break;

                case "Connection":

                    size = new Vector2(820, 520);

                    pos = new Vector2(10, 15);

                    title = "REMOTE CONNECTION - WebRTC Host Link";

                    break;

                case "LiveCam":

                    size = new Vector2(860, 540);

                    pos = new Vector2(0, 10);

                    title = "[CAM] CCTV SURVEILLANCE & WEBCAM FEED - LIVE";

                    break;
            }

            // Window Base GameObject

            GameObject winObj = new GameObject("Window_" + appType, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(DesktopWindow));

            winObj.transform.SetParent(_windowsContainer, false);

            RectTransform winRt = winObj.GetComponent<RectTransform>();

            winRt.anchorMin = new Vector2(0.5f, 0.5f);

            winRt.anchorMax = new Vector2(0.5f, 0.5f);

            winRt.anchoredPosition = pos;

            winRt.sizeDelta = size;

            // Background & Outer Glow Border

            Image winImg = winObj.GetComponent<Image>();

            winImg.color = new Color(0.04f, 0.06f, 0.10f, 0.96f);

            // Border Accent

            GameObject borderObj = new GameObject("GlowBorder", typeof(RectTransform), typeof(Image));

            borderObj.transform.SetParent(winObj.transform, false);

            RectTransform borderRt = borderObj.GetComponent<RectTransform>();

            borderRt.anchorMin = Vector2.zero;

            borderRt.anchorMax = Vector2.one;

            borderRt.offsetMin = new Vector2(-1, -1);

            borderRt.offsetMax = new Vector2(1, 1);

            Image borderImg = borderObj.GetComponent<Image>();

            borderImg.color = new Color(0.0f, 0.8f, 1.0f, 0.35f);

            borderRt.SetAsFirstSibling();

            // Title Bar

            GameObject titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));

            titleBar.transform.SetParent(winObj.transform, false);

            RectTransform tbRt = titleBar.GetComponent<RectTransform>();

            tbRt.anchorMin = new Vector2(0, 1);

            tbRt.anchorMax = new Vector2(1, 1);

            tbRt.pivot = new Vector2(0.5f, 1);

            tbRt.anchoredPosition = Vector2.zero;

            tbRt.sizeDelta = new Vector2(0, 38);

            Image tbImg = titleBar.GetComponent<Image>();

            tbImg.color = new Color(0.07f, 0.10f, 0.18f, 0.98f);

            // Title Bar Accent line

            GameObject accentLine = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));

            accentLine.transform.SetParent(titleBar.transform, false);

            RectTransform alRt = accentLine.GetComponent<RectTransform>();

            alRt.anchorMin = new Vector2(0, 0);

            alRt.anchorMax = new Vector2(1, 0);

            alRt.sizeDelta = new Vector2(0, 2);

            alRt.anchoredPosition = Vector2.zero;

            Image alImg = accentLine.GetComponent<Image>();

            alImg.color = new Color(0f, 1f, 1f, 0.6f);

            // Title Text

            GameObject titleTextObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));

            titleTextObj.transform.SetParent(titleBar.transform, false);

            RectTransform ttRt = titleTextObj.GetComponent<RectTransform>();

            ttRt.anchorMin = new Vector2(0, 0);

            ttRt.anchorMax = new Vector2(0.7f, 1);

            ttRt.offsetMin = new Vector2(38, 0);

            ttRt.offsetMax = Vector2.zero;

            TextMeshProUGUI ttTmp = titleTextObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) ttTmp.font = _defaultFont;

            ttTmp.text = title;

            ttTmp.fontSize = 18;

            ttTmp.fontStyle = FontStyles.Bold;

            ttTmp.color = Color.white;

            ttTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // App Icon

            GameObject appIconObj = new GameObject("AppIcon", typeof(RectTransform), typeof(Image));

            appIconObj.transform.SetParent(titleBar.transform, false);

            RectTransform aiRt = appIconObj.GetComponent<RectTransform>();

            aiRt.anchorMin = new Vector2(0, 0.5f);

            aiRt.anchorMax = new Vector2(0, 0.5f);

            aiRt.anchoredPosition = new Vector2(20, 0);

            aiRt.sizeDelta = new Vector2(16, 16);

            Image aiImg = appIconObj.GetComponent<Image>();

            aiImg.color = new Color(0f, 1f, 1f, 1f);

            // Window Buttons: Close, Maximize, Minimize

            Button closeBtn = CreateHeaderButton(titleBar.transform, "CloseBtn", new Vector2(-22, -19), new Color(1f, 0.2f, 0.35f), "X");

            Button maxBtn = CreateHeaderButton(titleBar.transform, "MaxBtn", new Vector2(-58, -19), new Color(0f, 0.9f, 0.9f), "[ ]");

            Button minBtn = CreateHeaderButton(titleBar.transform, "MinBtn", new Vector2(-94, -19), new Color(1f, 0.75f, 0.1f), "-");

            // Content Area Container

            GameObject contentObj = new GameObject("ContentArea", typeof(RectTransform));

            contentObj.transform.SetParent(winObj.transform, false);

            RectTransform caRt = contentObj.GetComponent<RectTransform>();

            caRt.anchorMin = Vector2.zero;

            caRt.anchorMax = Vector2.one;

            caRt.offsetMin = new Vector2(8, 8);

            caRt.offsetMax = new Vector2(-8, -42);

            // Wire DesktopWindow Component

            DesktopWindow win = winObj.GetComponent<DesktopWindow>();

            win.Setup(appType, title, size, pos);

            win.SetupButtons(closeBtn, minBtn, maxBtn);

            // Inject references via reflection or helper fields

            System.Reflection.FieldInfo fTitleBar = typeof(DesktopWindow).GetField("_titleBarRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fTitleBar != null) fTitleBar.SetValue(win, tbRt);

            System.Reflection.FieldInfo fTitleText = typeof(DesktopWindow).GetField("_titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fTitleText != null) fTitleText.SetValue(win, ttTmp);

            System.Reflection.FieldInfo fClose = typeof(DesktopWindow).GetField("_closeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fClose != null) fClose.SetValue(win, closeBtn);

            System.Reflection.FieldInfo fMax = typeof(DesktopWindow).GetField("_maximizeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fMax != null) fMax.SetValue(win, maxBtn);

            System.Reflection.FieldInfo fMin = typeof(DesktopWindow).GetField("_minimizeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fMin != null) fMin.SetValue(win, minBtn);

            System.Reflection.FieldInfo fContent = typeof(DesktopWindow).GetField("_contentArea", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fContent != null) fContent.SetValue(win, caRt);

            // Wire draggable title bar

            var dragHandler = titleBar.AddComponent<WindowTitleBarDragHandler>();

            dragHandler.Setup(win);

            // Attach specific application content component

            AttachAppContent(win, appType, caRt);

            return win;
        }

        private Button CreateHeaderButton(Transform parent, string name, Vector2 pos, Color normalColor, string symbol)

        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(1, 1);

            rt.anchorMax = new Vector2(1, 1);

            rt.anchoredPosition = pos;

            rt.sizeDelta = new Vector2(30, 26);

            Image img = btnObj.GetComponent<Image>();

            img.color = new Color(normalColor.r, normalColor.g, normalColor.b, 0.25f);

            img.raycastTarget = true;

            Button btn = btnObj.GetComponent<Button>();

            ColorBlock cb = btn.colors;

            cb.normalColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.25f);

            cb.highlightedColor = normalColor;

            cb.pressedColor = normalColor * 0.7f;

            btn.colors = cb;

            GameObject symObj = new GameObject("Symbol", typeof(RectTransform), typeof(TextMeshProUGUI));

            symObj.transform.SetParent(btnObj.transform, false);

            RectTransform srt = symObj.GetComponent<RectTransform>();

            srt.anchorMin = Vector2.zero;

            srt.anchorMax = Vector2.one;

            srt.offsetMin = Vector2.zero;

            srt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = symObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tmp.font = _defaultFont;

            tmp.text = symbol;

            tmp.fontSize = 14;

            tmp.raycastTarget = false;

            tmp.fontStyle = FontStyles.Bold;

            tmp.alignment = TextAlignmentOptions.Center;

            tmp.color = normalColor;

            return btn;
        }

        private void AttachAppContent(DesktopWindow win, string appType, RectTransform contentArea)

        {
            switch (appType)

            {
                case "Files":

                    BuildFilesContent(contentArea);

                    break;

                case "Terminal":

                    BuildTerminalContent(win, contentArea);

                    break;

                case "Browser":

                    BuildBrowserContent(contentArea);

                    break;

                case "Settings":

                    BuildSettingsContent(contentArea);

                    break;

                case "SysMonitor":

                    BuildSysMonitorContent(contentArea);

                    break;

                case "Notepad":

                    BuildNotepadContent(win, contentArea);

                    break;

                case "Connection":

                    BuildConnectionContent(win, contentArea);

                    break;

                case "LiveCam":

                    BuildLiveCamContent(win, contentArea);

                    break;
            }
        }

        // =========================================================================

        // APP SPECIFIC CONTENT BUILDERS

        // =========================================================================

        private void BuildFilesContent(RectTransform parent)

        {
            // 1. Left Sidebar (Explorer Navigation)

            GameObject sidebar = new GameObject("Sidebar", typeof(RectTransform), typeof(Image));

            sidebar.transform.SetParent(parent, false);

            RectTransform sbRt = sidebar.GetComponent<RectTransform>();

            sbRt.anchorMin = new Vector2(0f, 0f);

            sbRt.anchorMax = new Vector2(0.24f, 1f);

            sbRt.offsetMin = Vector2.zero;

            sbRt.offsetMax = Vector2.zero;

            Image sbImg = sidebar.GetComponent<Image>();

            sbImg.color = new Color(0.025f, 0.04f, 0.075f, 0.98f);

            // Right border on sidebar

            GameObject sbBorder = new GameObject("SbBorder", typeof(RectTransform), typeof(Image));

            sbBorder.transform.SetParent(sidebar.transform, false);

            RectTransform sbbRt = sbBorder.GetComponent<RectTransform>();

            sbbRt.anchorMin = new Vector2(1f, 0f);

            sbbRt.anchorMax = new Vector2(1f, 1f);

            sbbRt.sizeDelta = new Vector2(1f, 0f);

            sbbRt.anchoredPosition = Vector2.zero;

            Image sbbImg = sbBorder.GetComponent<Image>();

            sbbImg.color = new Color(0f, 0.8f, 1f, 0.25f);

            // Sidebar Header Bar

            GameObject sbHeader = new GameObject("SbHeader", typeof(RectTransform), typeof(TextMeshProUGUI));

            sbHeader.transform.SetParent(sidebar.transform, false);

            RectTransform sbhRt = sbHeader.GetComponent<RectTransform>();

            sbhRt.anchorMin = new Vector2(0f, 1f);

            sbhRt.anchorMax = new Vector2(1f, 1f);

            sbhRt.pivot = new Vector2(0.5f, 1f);

            sbhRt.sizeDelta = new Vector2(0f, 36f);

            sbhRt.anchoredPosition = Vector2.zero;

            TextMeshProUGUI sbhTmp = sbHeader.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) sbhTmp.font = _defaultFont;

            sbhTmp.text = "<b><color=#00ffff>EXPLORER</color></b> <color=#507090>v2.4</color>";

            sbhTmp.fontSize = 16;

            sbhTmp.alignment = TextAlignmentOptions.MidlineLeft;

            sbhTmp.margin = new Vector4(12, 0, 0, 0);

            // Category Container

            GameObject catCont = new GameObject("Categories", typeof(RectTransform), typeof(VerticalLayoutGroup));

            catCont.transform.SetParent(sidebar.transform, false);

            RectTransform ccRt = catCont.GetComponent<RectTransform>();

            ccRt.anchorMin = new Vector2(0f, 0f);

            ccRt.anchorMax = new Vector2(1f, 1f);

            ccRt.offsetMin = new Vector2(6f, 10f);

            ccRt.offsetMax = new Vector2(-6f, -38f);

            VerticalLayoutGroup ccVlg = catCont.GetComponent<VerticalLayoutGroup>();

            ccVlg.spacing = 3f;

            ccVlg.childControlHeight = true;

            ccVlg.childForceExpandHeight = false;

            ccVlg.childControlWidth = true;

            ccVlg.childForceExpandWidth = true;

            // 2. Main File Area Container (Top 62% of right area)

            GameObject mainList = new GameObject("FileListArea", typeof(RectTransform), typeof(Image));

            mainList.transform.SetParent(parent, false);

            RectTransform mlRt = mainList.GetComponent<RectTransform>();

            mlRt.anchorMin = new Vector2(0.245f, 0.38f);

            mlRt.anchorMax = new Vector2(1f, 1f);

            mlRt.offsetMin = Vector2.zero;

            mlRt.offsetMax = Vector2.zero;

            Image mlImg = mainList.GetComponent<Image>();

            mlImg.color = new Color(0.035f, 0.055f, 0.095f, 0.95f);

            // Breadcrumb / Path Bar

            GameObject pathBar = new GameObject("PathBar", typeof(RectTransform), typeof(Image));

            pathBar.transform.SetParent(mainList.transform, false);

            RectTransform pbRt = pathBar.GetComponent<RectTransform>();

            pbRt.anchorMin = new Vector2(0f, 1f);

            pbRt.anchorMax = new Vector2(1f, 1f);

            pbRt.pivot = new Vector2(0.5f, 1f);

            pbRt.sizeDelta = new Vector2(0f, 32f);

            pbRt.anchoredPosition = Vector2.zero;

            Image pbImg = pathBar.GetComponent<Image>();

            pbImg.color = new Color(0.05f, 0.08f, 0.13f, 0.95f);

            GameObject pathTextObj = new GameObject("PathText", typeof(RectTransform), typeof(TextMeshProUGUI));

            pathTextObj.transform.SetParent(pathBar.transform, false);

            RectTransform ptRt = pathTextObj.GetComponent<RectTransform>();

            ptRt.anchorMin = Vector2.zero;

            ptRt.anchorMax = Vector2.one;

            ptRt.offsetMin = new Vector2(10f, 0f);

            ptRt.offsetMax = new Vector2(-10f, 0f);

            TextMeshProUGUI ptTmp = pathTextObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) ptTmp.font = _defaultFont;

            ptTmp.text = "<color=#00ffff><b>/workspace/system_files</b></color>  <color=#507090>|  6 ITEMS (16.6 MB)</color>";

            ptTmp.fontSize = 16;

            ptTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Columns Header Bar

            GameObject colHeader = new GameObject("ColHeader", typeof(RectTransform), typeof(Image));

            colHeader.transform.SetParent(mainList.transform, false);

            RectTransform chRt = colHeader.GetComponent<RectTransform>();

            chRt.anchorMin = new Vector2(0f, 1f);

            chRt.anchorMax = new Vector2(1f, 1f);

            chRt.pivot = new Vector2(0.5f, 1f);

            chRt.sizeDelta = new Vector2(0f, 28f);

            chRt.anchoredPosition = new Vector2(0f, -32f);

            Image chImg = colHeader.GetComponent<Image>();

            chImg.color = new Color(0.06f, 0.09f, 0.15f, 0.95f);

            CreateColLabel(colHeader.transform, "NAME", new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(34f, 0f), Vector2.zero, TextAlignmentOptions.MidlineLeft, new Color(0f, 0.9f, 1f));

            CreateColLabel(colHeader.transform, "TYPE", new Vector2(0.48f, 0f), new Vector2(0.68f, 1f), new Vector2(4f, 0f), Vector2.zero, TextAlignmentOptions.MidlineLeft, new Color(0.65f, 0.78f, 0.9f));

            CreateColLabel(colHeader.transform, "SIZE", new Vector2(0.68f, 0f), new Vector2(0.83f, 1f), new Vector2(4f, 0f), Vector2.zero, TextAlignmentOptions.MidlineLeft, new Color(0.65f, 0.78f, 0.9f));

            CreateColLabel(colHeader.transform, "MODIFIED", new Vector2(0.83f, 0f), new Vector2(1f, 1f), new Vector2(4f, 0f), new Vector2(-12f, 0f), TextAlignmentOptions.MidlineRight, new Color(0.65f, 0.78f, 0.9f));

            // Scrollable File List Container

            GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));

            scrollObj.transform.SetParent(mainList.transform, false);

            RectTransform sRt = scrollObj.GetComponent<RectTransform>();

            sRt.anchorMin = Vector2.zero;

            sRt.anchorMax = Vector2.one;

            sRt.offsetMin = new Vector2(2f, 2f);

            sRt.offsetMax = new Vector2(-2f, -52f);

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();

            GameObject fileCont = new GameObject("FilesContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

            fileCont.transform.SetParent(scrollObj.transform, false);

            RectTransform fcRt = fileCont.GetComponent<RectTransform>();

            fcRt.anchorMin = new Vector2(0f, 1f);

            fcRt.anchorMax = new Vector2(1f, 1f);

            fcRt.pivot = new Vector2(0.5f, 1f);

            fcRt.sizeDelta = new Vector2(0f, 300f);

            fcRt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup vlg = fileCont.GetComponent<VerticalLayoutGroup>();

            vlg.spacing = 3f;

            vlg.childControlHeight = true;

            vlg.childForceExpandHeight = false;

            vlg.childControlWidth = true;

            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = fileCont.GetComponent<ContentSizeFitter>();

            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            sr.viewport = sRt;

            sr.content = fcRt;

            sr.vertical = true;

            sr.horizontal = false;

            sr.scrollSensitivity = 20f;

            // 3. Bottom Preview Drawer (Bottom 37.5% of right area)

            GameObject preview = new GameObject("PreviewPane", typeof(RectTransform), typeof(Image));

            preview.transform.SetParent(parent, false);

            RectTransform prRt = preview.GetComponent<RectTransform>();

            prRt.anchorMin = new Vector2(0.245f, 0f);

            prRt.anchorMax = new Vector2(1f, 0.375f);

            prRt.offsetMin = Vector2.zero;

            prRt.offsetMax = Vector2.zero;

            Image prImg = preview.GetComponent<Image>();

            prImg.color = new Color(0.02f, 0.035f, 0.065f, 0.98f);

            // Top Border Accent on Preview Drawer

            GameObject prBorder = new GameObject("PrBorder", typeof(RectTransform), typeof(Image));

            prBorder.transform.SetParent(preview.transform, false);

            RectTransform prbRt = prBorder.GetComponent<RectTransform>();

            prbRt.anchorMin = new Vector2(0f, 1f);

            prbRt.anchorMax = new Vector2(1f, 1f);

            prbRt.sizeDelta = new Vector2(0f, 1f);

            prbRt.anchoredPosition = Vector2.zero;

            Image prbImg = prBorder.GetComponent<Image>();

            prbImg.color = new Color(0f, 0.8f, 1f, 0.35f);

            // Preview Header Bar (FileName + FileInfo + Open Button)

            GameObject prHeader = new GameObject("PrHeader", typeof(RectTransform));

            prHeader.transform.SetParent(preview.transform, false);

            RectTransform prhRt = prHeader.GetComponent<RectTransform>();

            prhRt.anchorMin = new Vector2(0f, 1f);

            prhRt.anchorMax = new Vector2(1f, 1f);

            prhRt.pivot = new Vector2(0.5f, 1f);

            prhRt.sizeDelta = new Vector2(0f, 36f);

            prhRt.anchoredPosition = Vector2.zero;

            // File Name

            GameObject fnObj = new GameObject("FileName", typeof(RectTransform), typeof(TextMeshProUGUI));

            fnObj.transform.SetParent(prHeader.transform, false);

            RectTransform fnRt = fnObj.GetComponent<RectTransform>();

            fnRt.anchorMin = new Vector2(0f, 0f);

            fnRt.anchorMax = new Vector2(0.38f, 1f);

            fnRt.offsetMin = new Vector2(12f, 0f);

            fnRt.offsetMax = Vector2.zero;

            TextMeshProUGUI fnTmp = fnObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) fnTmp.font = _defaultFont;

            fnTmp.text = "quantum_core.sys";

            fnTmp.fontSize = 18;

            fnTmp.fontStyle = FontStyles.Bold;

            fnTmp.color = new Color(0f, 1f, 1f);

            fnTmp.alignment = TextAlignmentOptions.MidlineLeft;

            fnTmp.overflowMode = TextOverflowModes.Ellipsis;

            // File Info

            GameObject infoObj = new GameObject("FileInfo", typeof(RectTransform), typeof(TextMeshProUGUI));

            infoObj.transform.SetParent(prHeader.transform, false);

            RectTransform infoRt = infoObj.GetComponent<RectTransform>();

            infoRt.anchorMin = new Vector2(0.38f, 0f);

            infoRt.anchorMax = new Vector2(0.74f, 1f);

            infoRt.offsetMin = new Vector2(8f, 0f);

            infoRt.offsetMax = Vector2.zero;

            TextMeshProUGUI infoTmp = infoObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) infoTmp.font = _defaultFont;

            infoTmp.text = "System Driver | 14.2 MB | Today, 14:22";

            infoTmp.fontSize = 16;

            infoTmp.color = new Color(0.65f, 0.75f, 0.88f);

            infoTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Open in Notepad Button

            Button openBtn = CreateButton(prHeader.transform, "OpenBtn", "EDIT IN NOTEPAD", new Vector2(-95f, 0f), new Vector2(175f, 26f));

            // Snippet Box Container

            GameObject snippetBox = new GameObject("SnippetBox", typeof(RectTransform), typeof(Image));

            snippetBox.transform.SetParent(preview.transform, false);

            RectTransform snbRt = snippetBox.GetComponent<RectTransform>();

            snbRt.anchorMin = Vector2.zero;

            snbRt.anchorMax = Vector2.one;

            snbRt.offsetMin = new Vector2(10f, 8f);

            snbRt.offsetMax = new Vector2(-10f, -38f);

            Image snbImg = snippetBox.GetComponent<Image>();

            snbImg.color = new Color(0.012f, 0.02f, 0.04f, 1f);

            GameObject snippetObj = new GameObject("Snippet", typeof(RectTransform), typeof(TextMeshProUGUI));

            snippetObj.transform.SetParent(snippetBox.transform, false);

            RectTransform snRt = snippetObj.GetComponent<RectTransform>();

            snRt.anchorMin = Vector2.zero;

            snRt.anchorMax = Vector2.one;

            snRt.offsetMin = new Vector2(8f, 6f);

            snRt.offsetMax = new Vector2(-8f, -6f);

            TextMeshProUGUI snTmp = snippetObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) snTmp.font = _defaultFont;

            snTmp.text = "[KERNEL MODULE 0x8841F]\nSTATUS: ACTIVE\nDRIVER: Spatial Compositor v6.8";

            snTmp.fontSize = 16;

            snTmp.color = new Color(0.0f, 1.0f, 0.55f);

            snTmp.alignment = TextAlignmentOptions.TopLeft;

            snTmp.richText = true;

            snTmp.textWrappingMode = TextWrappingModes.Normal;

            // Attach & Initialize FileManagerApp

            FileManagerApp app = parent.gameObject.GetComponent<FileManagerApp>();

            if (app == null) app = parent.gameObject.AddComponent<FileManagerApp>();

            app.Setup(fileCont.transform, fnTmp, infoTmp, snTmp, openBtn);

            // Populate Sidebar Categories

            CreateSidebarCategory(catCont.transform, "All Files", "6", () => app.FilterCategory("All Files"));

            CreateSidebarCategory(catCont.transform, "System Drivers", "1", () => app.FilterCategory("System Drivers"));

            CreateSidebarCategory(catCont.transform, "Config & Logs", "2", () => app.FilterCategory("Config"));

            CreateSidebarCategory(catCont.transform, "Security Keys", "1", () => app.FilterCategory("Security Keys"));

            CreateSidebarCategory(catCont.transform, "Documents", "2", () => app.FilterCategory("Documents"));
        }

        private void CreateColLabel(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, TextAlignmentOptions alignment, Color color)

        {
            GameObject obj = new GameObject("Col_" + text, typeof(RectTransform), typeof(TextMeshProUGUI));

            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.GetComponent<RectTransform>();

            rt.anchorMin = anchorMin;

            rt.anchorMax = anchorMax;

            rt.offsetMin = offsetMin;

            rt.offsetMax = offsetMax;

            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tmp.font = _defaultFont;

            tmp.text = text;

            tmp.fontSize = 15;

            tmp.fontStyle = FontStyles.Bold;

            tmp.color = color;

            tmp.alignment = alignment;

            tmp.raycastTarget = false;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)

        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(1f, 0.5f);

            rt.anchorMax = new Vector2(1f, 0.5f);

            rt.pivot = new Vector2(0.5f, 0.5f);

            rt.anchoredPosition = anchoredPosition;

            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();

            img.color = new Color(0f, 0.35f, 0.50f, 0.85f);

            img.raycastTarget = true;

            Button btn = btnObj.GetComponent<Button>();

            ColorBlock cb = btn.colors;

            cb.normalColor = new Color(0f, 0.35f, 0.50f, 0.85f);

            cb.highlightedColor = new Color(0f, 0.55f, 0.75f, 0.95f);

            cb.pressedColor = new Color(0f, 0.70f, 0.90f, 1.0f);

            btn.colors = cb;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            textObj.transform.SetParent(btnObj.transform, false);

            StretchFull(textObj.GetComponent<RectTransform>());

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tmp.font = _defaultFont;

            tmp.text = label;

            tmp.fontSize = 15;

            tmp.fontStyle = FontStyles.Bold;

            tmp.alignment = TextAlignmentOptions.Center;

            tmp.color = Color.white;

            tmp.raycastTarget = false;

            return btn;
        }

        private void CreateSidebarCategory(Transform parent, string title, string count, UnityEngine.Events.UnityAction onClick)

        {
            GameObject catObj = new GameObject("Cat_" + title, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));

            catObj.transform.SetParent(parent, false);

            LayoutElement le = catObj.GetComponent<LayoutElement>();

            le.minHeight = 28f;

            le.preferredHeight = 28f;

            le.flexibleHeight = 0f;

            le.flexibleWidth = 1f;

            Image img = catObj.GetComponent<Image>();

            img.color = new Color(0.04f, 0.07f, 0.12f, 0.6f);

            Button btn = catObj.GetComponent<Button>();

            ColorBlock cb = btn.colors;

            cb.normalColor = new Color(0.04f, 0.07f, 0.12f, 0.6f);

            cb.highlightedColor = new Color(0.08f, 0.16f, 0.28f, 0.95f);

            cb.pressedColor = new Color(0.12f, 0.24f, 0.40f, 1.0f);

            btn.colors = cb;

            if (onClick != null) btn.onClick.AddListener(onClick);

            GameObject textObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));

            textObj.transform.SetParent(catObj.transform, false);

            RectTransform trt = textObj.GetComponent<RectTransform>();

            trt.anchorMin = new Vector2(0f, 0f);

            trt.anchorMax = new Vector2(0.78f, 1f);

            trt.offsetMin = new Vector2(10f, 0f);

            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tmp.font = _defaultFont;

            tmp.text = title;

            tmp.fontSize = 16;

            tmp.color = new Color(0.85f, 0.92f, 1f, 1f);

            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            tmp.raycastTarget = false;

            if (!string.IsNullOrEmpty(count))

            {
                GameObject countObj = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));

                countObj.transform.SetParent(catObj.transform, false);

                RectTransform crt = countObj.GetComponent<RectTransform>();

                crt.anchorMin = new Vector2(0.78f, 0f);

                crt.anchorMax = new Vector2(1f, 1f);

                crt.offsetMin = Vector2.zero;

                crt.offsetMax = new Vector2(-8f, 0f);

                TextMeshProUGUI cTmp = countObj.GetComponent<TextMeshProUGUI>();

                if (_defaultFont != null) cTmp.font = _defaultFont;

                cTmp.text = count;

                cTmp.fontSize = 15;

                cTmp.color = new Color(0.0f, 0.85f, 1f, 0.9f);

                cTmp.alignment = TextAlignmentOptions.MidlineRight;

                cTmp.raycastTarget = false;
            }
        }

        private void BuildTerminalContent(DesktopWindow win, RectTransform parent)

        {
            // Black terminal background

            GameObject bgObj = new GameObject("TermBg", typeof(RectTransform), typeof(Image));

            bgObj.transform.SetParent(parent, false);

            RectTransform bgRt = bgObj.GetComponent<RectTransform>();

            bgRt.anchorMin = Vector2.zero;

            bgRt.anchorMax = Vector2.one;

            bgRt.offsetMin = Vector2.zero;

            bgRt.offsetMax = Vector2.zero;

            Image bgImg = bgObj.GetComponent<Image>();

            bgImg.color = new Color(0.015f, 0.025f, 0.045f, 0.98f);

            // History Output Scroll Area using RectMask2D for robust RenderTexture clipping

            GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));

            scrollObj.transform.SetParent(parent, false);

            RectTransform sRt = scrollObj.GetComponent<RectTransform>();

            sRt.anchorMin = new Vector2(0, 0.12f);

            sRt.anchorMax = Vector2.one;

            sRt.offsetMin = new Vector2(10, 0);

            sRt.offsetMax = new Vector2(-10, -8);

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();

            // OutputText: anchored TOP so it grows downward -- required for ScrollRect to scroll correctly

            GameObject textObj = new GameObject("OutputText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));

            textObj.transform.SetParent(scrollObj.transform, false);

            RectTransform tRt = textObj.GetComponent<RectTransform>();

            tRt.anchorMin = new Vector2(0, 1);   // anchored to top-left corner of viewport

            tRt.anchorMax = new Vector2(1, 1);   // anchored to top-right corner of viewport

            tRt.pivot = new Vector2(0.5f, 1f);   // pivot at top so it grows downward

            tRt.anchoredPosition = Vector2.zero;

            tRt.sizeDelta = new Vector2(0, 600); // start tall enough to hold initial banner

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tmp.font = _defaultFont;

            tmp.fontSize = 18;

            tmp.color = new Color(0.95f, 0.98f, 1f, 1f);  // bright crisp white -- clearly readable

            tmp.alignment = TextAlignmentOptions.TopLeft;

            tmp.richText = true;

            tmp.textWrappingMode = TextWrappingModes.Normal;

            tmp.overflowMode = TextOverflowModes.Overflow; // allow text to overflow rect height

            // ContentSizeFitter makes the content rect grow as text is added

            ContentSizeFitter csf = textObj.GetComponent<ContentSizeFitter>();

            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            sr.viewport = sRt;

            sr.content = tRt;

            sr.vertical = true;

            sr.horizontal = false;

            sr.movementType = ScrollRect.MovementType.Clamped;

            sr.scrollSensitivity = 20f;

            // Command Input Bar

            GameObject inputBar = new GameObject("InputBar", typeof(RectTransform), typeof(Image));

            inputBar.transform.SetParent(parent, false);

            RectTransform ibRt = inputBar.GetComponent<RectTransform>();

            ibRt.anchorMin = new Vector2(0, 0);

            ibRt.anchorMax = new Vector2(1, 0.11f);

            ibRt.offsetMin = new Vector2(10, 4);

            ibRt.offsetMax = new Vector2(-10, 0);

            Image ibImg = inputBar.GetComponent<Image>();

            ibImg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);

            ibImg.raycastTarget = true;

            // Prompt Label "root@vr:~$ "

            GameObject promptObj = new GameObject("Prompt", typeof(RectTransform), typeof(TextMeshProUGUI));

            promptObj.transform.SetParent(inputBar.transform, false);

            RectTransform pRt = promptObj.GetComponent<RectTransform>();

            pRt.anchorMin = new Vector2(0, 0);

            pRt.anchorMax = new Vector2(0.22f, 1);

            pRt.offsetMin = new Vector2(10, 0);

            pRt.offsetMax = Vector2.zero;

            TextMeshProUGUI pTmp = promptObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) pTmp.font = _defaultFont;

            pTmp.text = "<color=#00ff88>root@vr</color>:<color=#00ffff>~$</color>";

            pTmp.fontSize = 17;

            pTmp.alignment = TextAlignmentOptions.MidlineLeft;

            pTmp.raycastTarget = false;

            // Input Field using robust CreateStyledInputField

            TMP_InputField inputField = CreateStyledInputField(inputBar.transform, "InputField", "Enter command ('help', 'notepad', 'status')...", 16, false);

            RectTransform ifRt = inputField.GetComponent<RectTransform>();

            ifRt.anchorMin = new Vector2(0.23f, 0.1f);

            ifRt.anchorMax = new Vector2(0.81f, 0.9f);

            ifRt.offsetMin = Vector2.zero;

            ifRt.offsetMax = Vector2.zero;

            // Run Button

            GameObject runBtnObj = new GameObject("RunBtn", typeof(RectTransform), typeof(Image), typeof(Button));

            runBtnObj.transform.SetParent(inputBar.transform, false);

            RectTransform rbRt = runBtnObj.GetComponent<RectTransform>();

            rbRt.anchorMin = new Vector2(0.83f, 0.12f);

            rbRt.anchorMax = new Vector2(0.985f, 0.88f);

            rbRt.offsetMin = Vector2.zero;

            rbRt.offsetMax = Vector2.zero;

            Image rbImg = runBtnObj.GetComponent<Image>();

            rbImg.color = new Color(0f, 0.85f, 1f, 0.9f);

            rbImg.raycastTarget = true;

            Button runBtn = runBtnObj.GetComponent<Button>();

            GameObject rbText = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            rbText.transform.SetParent(runBtnObj.transform, false);

            RectTransform rbtRt = rbText.GetComponent<RectTransform>();

            rbtRt.anchorMin = Vector2.zero;

            rbtRt.anchorMax = Vector2.one;

            rbtRt.offsetMin = Vector2.zero;

            rbtRt.offsetMax = Vector2.zero;

            TextMeshProUGUI rbtTmp = rbText.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) rbtTmp.font = _defaultFont;

            rbtTmp.text = "EXECUTE";

            rbtTmp.fontSize = 15;

            rbtTmp.fontStyle = FontStyles.Bold;

            rbtTmp.alignment = TextAlignmentOptions.Center;

            rbtTmp.color = Color.black;

            rbtTmp.raycastTarget = false;

            // Attach TerminalApp component and call Setup

            TerminalApp term = parent.gameObject.GetComponent<TerminalApp>();

            if (term == null) term = parent.gameObject.AddComponent<TerminalApp>();

            term.Setup(inputField, runBtn, tmp, sr, win);
        }

        private void BuildBrowserContent(RectTransform parent)

        {
            // Address bar

            GameObject barObj = new GameObject("NavBar", typeof(RectTransform), typeof(Image));

            barObj.transform.SetParent(parent, false);

            RectTransform barRt = barObj.GetComponent<RectTransform>();

            barRt.anchorMin = new Vector2(0, 1);

            barRt.anchorMax = new Vector2(1, 1);

            barRt.pivot = new Vector2(0.5f, 1);

            barRt.anchoredPosition = Vector2.zero;

            barRt.sizeDelta = new Vector2(0, 42);

            Image barImg = barObj.GetComponent<Image>();

            barImg.color = new Color(0.06f, 0.09f, 0.15f, 0.95f);

            // URL Text / Input

            GameObject urlObj = new GameObject("URLField", typeof(RectTransform), typeof(TMP_InputField), typeof(Image));

            urlObj.transform.SetParent(barObj.transform, false);

            RectTransform urlRt = urlObj.GetComponent<RectTransform>();

            urlRt.anchorMin = new Vector2(0.05f, 0.15f);

            urlRt.anchorMax = new Vector2(0.82f, 0.85f);

            urlRt.offsetMin = Vector2.zero;

            urlRt.offsetMax = Vector2.zero;

            Image urlBg = urlObj.GetComponent<Image>();

            urlBg.color = new Color(0.03f, 0.05f, 0.08f, 0.9f);

            GameObject utObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            utObj.transform.SetParent(urlObj.transform, false);

            RectTransform utRt = utObj.GetComponent<RectTransform>();

            utRt.anchorMin = Vector2.zero;

            utRt.anchorMax = Vector2.one;

            utRt.offsetMin = new Vector2(12, 0);

            utRt.offsetMax = Vector2.zero;

            TextMeshProUGUI utTmp = utObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) utTmp.font = _defaultFont;

            utTmp.text = "https://quantum-vr.net/search";

            utTmp.fontSize = 17;

            utTmp.color = new Color(0f, 1f, 1f);

            TMP_InputField inputField = urlObj.GetComponent<TMP_InputField>();

            inputField.textComponent = utTmp;

            // Go Button

            GameObject goObj = new GameObject("GoBtn", typeof(RectTransform), typeof(Image), typeof(Button));

            goObj.transform.SetParent(barObj.transform, false);

            RectTransform goRt = goObj.GetComponent<RectTransform>();

            goRt.anchorMin = new Vector2(0.84f, 0.15f);

            goRt.anchorMax = new Vector2(0.97f, 0.85f);

            goRt.offsetMin = Vector2.zero;

            goRt.offsetMax = Vector2.zero;

            Image goImg = goObj.GetComponent<Image>();

            goImg.color = new Color(0.1f, 0.8f, 0.6f);

            Button goBtn = goObj.GetComponent<Button>();

            // Page Container

            GameObject page = new GameObject("PageContent", typeof(RectTransform), typeof(Image));

            page.transform.SetParent(parent, false);

            RectTransform pRt = page.GetComponent<RectTransform>();

            pRt.anchorMin = Vector2.zero;

            pRt.anchorMax = new Vector2(1, 1);

            pRt.offsetMin = Vector2.zero;

            pRt.offsetMax = new Vector2(0, -46);

            Image pImg = page.GetComponent<Image>();

            pImg.color = new Color(0.03f, 0.05f, 0.08f, 0.98f);

            // Webpage headline

            GameObject hlObj = new GameObject("Headline", typeof(RectTransform), typeof(TextMeshProUGUI));

            hlObj.transform.SetParent(page.transform, false);

            RectTransform hlRt = hlObj.GetComponent<RectTransform>();

            hlRt.anchorMin = new Vector2(0.04f, 0.58f);

            hlRt.anchorMax = new Vector2(0.96f, 0.96f);

            hlRt.offsetMin = Vector2.zero;

            hlRt.offsetMax = Vector2.zero;

            TextMeshProUGUI hlTmp = hlObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) hlTmp.font = _defaultFont;

            hlTmp.text = "<color=#00ffff><b>QUANTUM WEB NAVIGATOR ONLINE</b></color>\n" +

                         "<size=15><color=#00ff88>| FULL-RESOLUTION 1920x1080 VIEWPORT OPEN ON LEFT MONITOR (L-01)</color></size>\n" +

                         "<size=13><color=#aaaaaa>Look at your left screen for multi-tab browsing, live search, news, AI assistant, and games.</size></color>";

            hlTmp.fontSize = 22;

            hlTmp.fontStyle = FontStyles.Bold;

            hlTmp.alignment = TextAlignmentOptions.Center;

            // Direct Focus Left Monitor Button

            GameObject switchBtn = new GameObject("SwitchLeftBtn", typeof(RectTransform), typeof(Image), typeof(Button));

            switchBtn.transform.SetParent(page.transform, false);

            RectTransform sbrt = switchBtn.GetComponent<RectTransform>();

            sbrt.anchorMin = new Vector2(0.15f, 0.38f);

            sbrt.anchorMax = new Vector2(0.85f, 0.54f);

            sbrt.offsetMin = Vector2.zero;

            sbrt.offsetMax = Vector2.zero;

            Image sbImg = switchBtn.GetComponent<Image>();

            sbImg.color = new Color(0.08f, 0.35f, 0.5f, 0.95f);

            Button sbBtn = switchBtn.GetComponent<Button>();

            sbBtn.onClick.AddListener(() => {
                if (LeftDashboardUI.Instance != null)

                {
                    LeftDashboardUI.Instance.ShowBrowser();
                }

                VRRemoteDesktop.Audio.AudioManager.Instance?.PlayConnectChime();
            });

            GameObject sbTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            sbTextObj.transform.SetParent(switchBtn.transform, false);

            RectTransform sbtRt = sbTextObj.GetComponent<RectTransform>();

            sbtRt.anchorMin = Vector2.zero;

            sbtRt.anchorMax = Vector2.one;

            TextMeshProUGUI sbtTmp = sbTextObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) sbtTmp.font = _defaultFont;

            sbtTmp.text = "[PC] OPEN / FOCUS BROWSER ON LEFT MONITOR (L-01)";

            sbtTmp.fontSize = 18;

            sbtTmp.fontStyle = FontStyles.Bold;

            sbtTmp.alignment = TextAlignmentOptions.Center;

            sbtTmp.color = Color.white;

            // Quick App Launchers Row: Search, News, AI, CCTV, Arcade

            string[] appNames = new string[] { "[WEB] Search", "[NEWS] News", "[AI] Nexus AI", "[CAM] CCTV", "[GAME] Arcade" };

            for (int i = 0; i < 5; i++)

            {
                int tabIdx = (i == 4) ? 5 : i;

                float minX = 0.05f + i * 0.185f;

                float maxX = minX + 0.165f;

                GameObject qb = new GameObject($"QBtn_{i}", typeof(RectTransform), typeof(Image), typeof(Button));

                qb.transform.SetParent(page.transform, false);

                RectTransform qbrt = qb.GetComponent<RectTransform>();

                qbrt.anchorMin = new Vector2(minX, 0.10f);

                qbrt.anchorMax = new Vector2(maxX, 0.30f);

                qbrt.offsetMin = Vector2.zero;

                qbrt.offsetMax = Vector2.zero;

                Image qbImg = qb.GetComponent<Image>();

                qbImg.color = new Color(0.06f, 0.12f, 0.20f, 0.9f);

                Button qbBtn = qb.GetComponent<Button>();

                qbBtn.onClick.AddListener(() => {
                    if (LeftDashboardUI.Instance != null)

                    {
                        LeftDashboardUI.Instance.ShowBrowser();

                        QuantumWebBrowserUI.Instance?.SwitchTab(tabIdx);
                    }

                    VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();
                });

                GameObject qbtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

                qbtObj.transform.SetParent(qb.transform, false);

                RectTransform qbtRt = qbtObj.GetComponent<RectTransform>();

                qbtRt.anchorMin = Vector2.zero;

                qbtRt.anchorMax = Vector2.one;

                TextMeshProUGUI qbtTmp = qbtObj.GetComponent<TextMeshProUGUI>();

                if (_defaultFont != null) qbtTmp.font = _defaultFont;

                qbtTmp.text = appNames[i];

                qbtTmp.fontSize = 15;

                qbtTmp.fontStyle = FontStyles.Bold;

                qbtTmp.alignment = TextAlignmentOptions.Center;

                qbtTmp.color = new Color(0f, 1f, 1f);
            }

            // Hook up Go button to navigate Left Monitor

            goBtn.onClick.AddListener(() => {
                string url = inputField.text;

                if (LeftDashboardUI.Instance != null)

                {
                    LeftDashboardUI.Instance.ShowBrowser();

                    QuantumWebBrowserUI.Instance?.NavigateTo(QuantumWebBrowserUI.BrowserTab.QuantumSearch, url);
                }
            });
        }

        private void BuildSettingsContent(RectTransform parent)

        {
            // Settings App with Theme switcher

            GameObject container = new GameObject("SettingsContainer", typeof(RectTransform));

            container.transform.SetParent(parent, false);

            RectTransform cRt = container.GetComponent<RectTransform>();

            cRt.anchorMin = Vector2.zero;

            cRt.anchorMax = Vector2.one;

            cRt.offsetMin = Vector2.zero;

            cRt.offsetMax = Vector2.zero;

            // Theme switch headline

            GameObject label = new GameObject("ThemeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));

            label.transform.SetParent(container.transform, false);

            RectTransform lRt = label.GetComponent<RectTransform>();

            lRt.anchorMin = new Vector2(0.05f, 0.8f);

            lRt.anchorMax = new Vector2(0.95f, 0.95f);

            lRt.offsetMin = Vector2.zero;

            lRt.offsetMax = Vector2.zero;

            TextMeshProUGUI lTmp = label.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) lTmp.font = _defaultFont;

            lTmp.text = "DESKTOP THEMES & PREFERENCES";

            lTmp.fontSize = 20;

            lTmp.fontStyle = FontStyles.Bold;

            lTmp.color = Color.white;

            // 4 Theme Buttons

            Button btnCyber = CreateSettingsOptionBtn(container.transform, "ThemeCyber", new Vector2(0.05f, 0.55f), new Vector2(0.48f, 0.75f), "CYBERPUNK NEON", new Color(0f, 1f, 1f));

            Button btnDeep = CreateSettingsOptionBtn(container.transform, "ThemeDeep", new Vector2(0.52f, 0.55f), new Vector2(0.95f, 0.75f), "DEEP SPACE BLUE", new Color(0.2f, 0.6f, 1f));

            Button btnMatrix = CreateSettingsOptionBtn(container.transform, "ThemeMatrix", new Vector2(0.05f, 0.30f), new Vector2(0.48f, 0.50f), "EMERALD MATRIX", new Color(0f, 1f, 0.4f));

            Button btnSolar = CreateSettingsOptionBtn(container.transform, "ThemeSolar", new Vector2(0.52f, 0.30f), new Vector2(0.95f, 0.50f), "SOLAR FLARE", new Color(1f, 0.5f, 0f));

            // Quality Presets

            Button qLow = CreateSettingsOptionBtn(container.transform, "QLow", new Vector2(0.05f, 0.08f), new Vector2(0.25f, 0.22f), "LOW", Color.gray);

            Button qMed = CreateSettingsOptionBtn(container.transform, "QMed", new Vector2(0.28f, 0.08f), new Vector2(0.48f, 0.22f), "MEDIUM", new Color(0f, 0.8f, 0.8f));

            Button qHigh = CreateSettingsOptionBtn(container.transform, "QHigh", new Vector2(0.52f, 0.08f), new Vector2(0.72f, 0.22f), "HIGH", new Color(0.5f, 0.2f, 1f));

            Button qUltra = CreateSettingsOptionBtn(container.transform, "QUltra", new Vector2(0.75f, 0.08f), new Vector2(0.95f, 0.22f), "ULTRA", new Color(1f, 0.1f, 0.5f));

            SettingsApp app = parent.gameObject.AddComponent<SettingsApp>();

            SetPrivateField(app, "_panelDisplay", container);

            SetPrivateField(app, "_themeCyberpunkBtn", btnCyber);

            SetPrivateField(app, "_themeDeepSpaceBtn", btnDeep);

            SetPrivateField(app, "_themeMatrixBtn", btnMatrix);

            SetPrivateField(app, "_themeSolarBtn", btnSolar);

            SetPrivateField(app, "_qualityLowBtn", qLow);

            SetPrivateField(app, "_qualityMedBtn", qMed);

            SetPrivateField(app, "_qualityHighBtn", qHigh);

            SetPrivateField(app, "_qualityUltraBtn", qUltra);
        }

        private Button CreateSettingsOptionBtn(Transform parent, string name, Vector2 min, Vector2 max, string text, Color color)

        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();

            rt.anchorMin = min;

            rt.anchorMax = max;

            rt.offsetMin = Vector2.zero;

            rt.offsetMax = Vector2.zero;

            Image img = btnObj.GetComponent<Image>();

            img.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.9f);

            Button btn = btnObj.GetComponent<Button>();

            ColorBlock cb = btn.colors;

            cb.normalColor = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.9f);

            cb.highlightedColor = color * 0.8f;

            cb.pressedColor = color;

            btn.colors = cb;

            GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            tObj.transform.SetParent(btnObj.transform, false);

            RectTransform trt = tObj.GetComponent<RectTransform>();

            trt.anchorMin = Vector2.zero;

            trt.anchorMax = Vector2.one;

            trt.offsetMin = Vector2.zero;

            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = tObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tmp.font = _defaultFont;

            tmp.text = text;

            tmp.fontSize = 16;

            tmp.fontStyle = FontStyles.Bold;

            tmp.alignment = TextAlignmentOptions.Center;

            tmp.color = Color.white;

            return btn;
        }

        private void BuildSysMonitorContent(RectTransform parent)

        {
            // System Monitor App Container

            GameObject container = new GameObject("SysMonContainer", typeof(RectTransform));

            container.transform.SetParent(parent, false);

            RectTransform cRt = container.GetComponent<RectTransform>();

            cRt.anchorMin = Vector2.zero;

            cRt.anchorMax = Vector2.one;

            cRt.offsetMin = Vector2.zero;

            cRt.offsetMax = Vector2.zero;

            // 1. Overview Gauges Panel (Top 36%)

            GameObject overview = new GameObject("GaugesBar", typeof(RectTransform), typeof(Image));

            overview.transform.SetParent(container.transform, false);

            RectTransform oRt = overview.GetComponent<RectTransform>();

            oRt.anchorMin = new Vector2(0.02f, 0.62f);

            oRt.anchorMax = new Vector2(0.98f, 0.98f);

            oRt.offsetMin = Vector2.zero;

            oRt.offsetMax = Vector2.zero;

            Image oImg = overview.GetComponent<Image>();

            oImg.color = new Color(0.025f, 0.04f, 0.075f, 0.98f);

            // 4 Telemetry Metric Cards

            CreateGaugeCard(overview.transform, "CPU_Metric", new Vector2(0f, 0.22f), new Vector2(0.25f, 1f), "CPU LOAD", "23%", new Color(0f, 1f, 1f), out TextMeshProUGUI cpuTmp, out Image cpuFill);

            CreateGaugeCard(overview.transform, "GPU_Metric", new Vector2(0.25f, 0.22f), new Vector2(0.50f, 1f), "GPU LOAD", "36%", new Color(0.65f, 0.35f, 1f), out TextMeshProUGUI gpuTmp, out Image gpuFill);

            CreateGaugeCard(overview.transform, "RAM_Metric", new Vector2(0.50f, 0.22f), new Vector2(0.75f, 1f), "MEMORY", "39% (6.4 / 16 GB)", new Color(1f, 0.20f, 0.60f), out TextMeshProUGUI ramTmp, out Image ramFill);

            CreateGaugeCard(overview.transform, "NET_Metric", new Vector2(0.75f, 0.22f), new Vector2(1f, 1f), "NETWORK", "DN: 531 MB/s", new Color(0.0f, 1f, 0.55f), out TextMeshProUGUI netTmp, out _);

            // Sub-telemetry strip across bottom of gauges card

            GameObject subInfoObj = new GameObject("SubInfoStrip", typeof(RectTransform), typeof(TextMeshProUGUI));

            subInfoObj.transform.SetParent(overview.transform, false);

            RectTransform subRt = subInfoObj.GetComponent<RectTransform>();

            subRt.anchorMin = new Vector2(0f, 0f);

            subRt.anchorMax = new Vector2(1f, 0.22f);

            subRt.offsetMin = new Vector2(12f, 0f);

            subRt.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI subTmp = subInfoObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) subTmp.font = _defaultFont;

            subTmp.text = "<b>FPS:</b> <color=#00ff88>60.0</color>   |   <b>GPU TEMP:</b> <color=#ffaa00>43 C</color>   |   <b>NET UP:</b> <color=#00ffff>UP: 28.4 MB/s</color>   |   <b>PROCESSES:</b> <color=#ffffff>6</color>";

            subTmp.fontSize = 15;

            subTmp.color = new Color(0.65f, 0.75f, 0.88f);

            subTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // 2. Process Table Area (Bottom 60%)

            GameObject procArea = new GameObject("ProcessTableArea", typeof(RectTransform), typeof(Image));

            procArea.transform.SetParent(container.transform, false);

            RectTransform pRt = procArea.GetComponent<RectTransform>();

            pRt.anchorMin = new Vector2(0.02f, 0.02f);

            pRt.anchorMax = new Vector2(0.98f, 0.60f);

            pRt.offsetMin = Vector2.zero;

            pRt.offsetMax = Vector2.zero;

            Image pImg = procArea.GetComponent<Image>();

            pImg.color = new Color(0.025f, 0.04f, 0.075f, 0.98f);

            // Table Title Bar

            GameObject tbBar = new GameObject("TableTitleBar", typeof(RectTransform), typeof(Image));

            tbBar.transform.SetParent(procArea.transform, false);

            RectTransform tbbRt = tbBar.GetComponent<RectTransform>();

            tbbRt.anchorMin = new Vector2(0f, 1f);

            tbbRt.anchorMax = new Vector2(1f, 1f);

            tbbRt.pivot = new Vector2(0.5f, 1f);

            tbbRt.sizeDelta = new Vector2(0f, 28f);

            tbbRt.anchoredPosition = Vector2.zero;

            Image tbbImg = tbBar.GetComponent<Image>();

            tbbImg.color = new Color(0.04f, 0.065f, 0.11f, 0.95f);

            GameObject tbTitle = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));

            tbTitle.transform.SetParent(tbBar.transform, false);

            RectTransform tbtRt = tbTitle.GetComponent<RectTransform>();

            tbtRt.anchorMin = Vector2.zero;

            tbtRt.anchorMax = Vector2.one;

            tbtRt.offsetMin = new Vector2(10f, 0f);

            tbtRt.offsetMax = new Vector2(-10f, 0f);

            TextMeshProUGUI tbtTmp = tbTitle.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tbtTmp.font = _defaultFont;

            tbtTmp.text = "<b><color=#00ffff>ACTIVE PROCESSES</color></b> <color=#507090>(6 RUNNING)</color>";

            tbtTmp.fontSize = 17;

            tbtTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Table Columns Header Row

            GameObject colHeader = new GameObject("ColHeader", typeof(RectTransform), typeof(Image));

            colHeader.transform.SetParent(procArea.transform, false);

            RectTransform chRt = colHeader.GetComponent<RectTransform>();

            chRt.anchorMin = new Vector2(0f, 1f);

            chRt.anchorMax = new Vector2(1f, 1f);

            chRt.pivot = new Vector2(0.5f, 1f);

            chRt.sizeDelta = new Vector2(0f, 24f);

            chRt.anchoredPosition = new Vector2(0f, -26f);

            Image chImg = colHeader.GetComponent<Image>();

            chImg.color = new Color(0.05f, 0.08f, 0.14f, 0.95f);

            CreateColLabel(colHeader.transform, "PROCESS NAME", new Vector2(0f, 0f), new Vector2(0.44f, 1f), new Vector2(28f, 0f), Vector2.zero, TextAlignmentOptions.MidlineLeft, new Color(0f, 0.9f, 1f));

            CreateColLabel(colHeader.transform, "PID", new Vector2(0.44f, 0f), new Vector2(0.58f, 1f), new Vector2(4f, 0f), Vector2.zero, TextAlignmentOptions.MidlineLeft, new Color(0.65f, 0.78f, 0.9f));

            CreateColLabel(colHeader.transform, "CPU LOAD", new Vector2(0.58f, 0f), new Vector2(0.70f, 1f), new Vector2(4f, 0f), Vector2.zero, TextAlignmentOptions.MidlineLeft, new Color(0.65f, 0.78f, 0.9f));

            CreateColLabel(colHeader.transform, "MEMORY", new Vector2(0.70f, 0f), new Vector2(0.84f, 1f), new Vector2(4f, 0f), Vector2.zero, TextAlignmentOptions.MidlineLeft, new Color(0.65f, 0.78f, 0.9f));

            CreateColLabel(colHeader.transform, "ACTION", new Vector2(0.85f, 0f), new Vector2(0.985f, 1f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, new Color(0.65f, 0.78f, 0.9f));

            // Scrollable Process Table List

            GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));

            scrollObj.transform.SetParent(procArea.transform, false);

            RectTransform sRt = scrollObj.GetComponent<RectTransform>();

            sRt.anchorMin = Vector2.zero;

            sRt.anchorMax = Vector2.one;

            sRt.offsetMin = new Vector2(2f, 2f);

            sRt.offsetMax = new Vector2(-2f, -52f);

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();

            GameObject listObj = new GameObject("ProcessList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

            listObj.transform.SetParent(scrollObj.transform, false);

            RectTransform lRt = listObj.GetComponent<RectTransform>();

            lRt.anchorMin = new Vector2(0f, 1f);

            lRt.anchorMax = new Vector2(1f, 1f);

            lRt.pivot = new Vector2(0.5f, 1f);

            lRt.sizeDelta = new Vector2(0f, 300f);

            lRt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup vlg = listObj.GetComponent<VerticalLayoutGroup>();

            vlg.spacing = 3f;

            vlg.childControlHeight = true;

            vlg.childForceExpandHeight = false;

            vlg.childControlWidth = true;

            vlg.childForceExpandWidth = true;

            ContentSizeFitter csf = listObj.GetComponent<ContentSizeFitter>();

            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            sr.viewport = sRt;

            sr.content = lRt;

            sr.vertical = true;

            sr.horizontal = false;

            sr.scrollSensitivity = 20f;

            // Attach & Setup SystemMonitorApp

            SystemMonitorApp app = parent.gameObject.GetComponent<SystemMonitorApp>();

            if (app == null) app = parent.gameObject.AddComponent<SystemMonitorApp>();

            app.Setup(cpuTmp, cpuFill, gpuTmp, gpuFill, ramTmp, ramFill, netTmp, subTmp, listObj.transform);
        }

        private void CreateGaugeCard(Transform parent, string name, Vector2 min, Vector2 max, string title, string initialVal, Color accent, out TextMeshProUGUI valTmp, out Image fillImg)

        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image));

            card.transform.SetParent(parent, false);

            RectTransform rt = card.GetComponent<RectTransform>();

            rt.anchorMin = min;

            rt.anchorMax = max;

            rt.offsetMin = new Vector2(3f, 4f);

            rt.offsetMax = new Vector2(-3f, -4f);

            Image img = card.GetComponent<Image>();

            img.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);

            // Title

            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));

            tObj.transform.SetParent(card.transform, false);

            RectTransform trt = tObj.GetComponent<RectTransform>();

            trt.anchorMin = new Vector2(0f, 0.62f);

            trt.anchorMax = new Vector2(1f, 1f);

            trt.offsetMin = new Vector2(10f, 0f);

            trt.offsetMax = new Vector2(-10f, 0f);

            TextMeshProUGUI tt = tObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tt.font = _defaultFont;

            tt.text = title;

            tt.fontSize = 15;

            tt.fontStyle = FontStyles.Bold;

            tt.color = new Color(0.65f, 0.78f, 0.92f);

            tt.alignment = TextAlignmentOptions.MidlineLeft;

            // Value

            GameObject vObj = new GameObject("Val", typeof(RectTransform), typeof(TextMeshProUGUI));

            vObj.transform.SetParent(card.transform, false);

            RectTransform vrt = vObj.GetComponent<RectTransform>();

            vrt.anchorMin = new Vector2(0f, 0.20f);

            vrt.anchorMax = new Vector2(1f, 0.62f);

            vrt.offsetMin = new Vector2(10f, 0f);

            vrt.offsetMax = new Vector2(-10f, 0f);

            valTmp = vObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) valTmp.font = _defaultFont;

            valTmp.text = initialVal;

            valTmp.fontSize = 20;

            valTmp.fontStyle = FontStyles.Bold;

            valTmp.color = accent;

            valTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Progress Bar Track

            GameObject trackObj = new GameObject("Track", typeof(RectTransform), typeof(Image));

            trackObj.transform.SetParent(card.transform, false);

            RectTransform trtTrack = trackObj.GetComponent<RectTransform>();

            trtTrack.anchorMin = new Vector2(0f, 0f);

            trtTrack.anchorMax = new Vector2(1f, 0f);

            trtTrack.pivot = new Vector2(0.5f, 0f);

            trtTrack.anchoredPosition = new Vector2(0f, 8f);

            trtTrack.sizeDelta = new Vector2(-20f, 4f);

            Image trackImg = trackObj.GetComponent<Image>();

            trackImg.color = new Color(0.08f, 0.12f, 0.20f, 1f);

            // Progress Bar Fill

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));

            fillObj.transform.SetParent(trackObj.transform, false);

            RectTransform frt = fillObj.GetComponent<RectTransform>();

            frt.anchorMin = Vector2.zero;

            frt.anchorMax = Vector2.one;

            frt.offsetMin = Vector2.zero;

            frt.offsetMax = Vector2.zero;

            fillImg = fillObj.GetComponent<Image>();

            fillImg.color = accent;

            fillImg.type = Image.Type.Filled;

            fillImg.fillMethod = Image.FillMethod.Horizontal;

            fillImg.fillAmount = 0.35f;
        }

        private void BuildConnectionContent(DesktopWindow win, RectTransform parent)

        {
            GameObject container = new GameObject("ConnectionContainer", typeof(RectTransform));

            container.transform.SetParent(parent, false);

            RectTransform cRt = container.GetComponent<RectTransform>();

            cRt.anchorMin = Vector2.zero;

            cRt.anchorMax = Vector2.one;

            cRt.offsetMin = Vector2.zero;

            cRt.offsetMax = Vector2.zero;

            // 1. Top Status Banner Card (Height: 88px)

            GameObject statusCard = new GameObject("StatusCard", typeof(RectTransform), typeof(Image));

            statusCard.transform.SetParent(container.transform, false);

            RectTransform scRt = statusCard.GetComponent<RectTransform>();

            scRt.anchorMin = new Vector2(0.025f, 0.78f);

            scRt.anchorMax = new Vector2(0.975f, 0.98f);

            scRt.offsetMin = Vector2.zero;

            scRt.offsetMax = Vector2.zero;

            Image scImg = statusCard.GetComponent<Image>();

            scImg.color = new Color(0.03f, 0.055f, 0.095f, 0.98f);

            // Status Indicator Dot

            GameObject dotObj = new GameObject("StatusDot", typeof(RectTransform), typeof(Image));

            dotObj.transform.SetParent(statusCard.transform, false);

            RectTransform dotRt = dotObj.GetComponent<RectTransform>();

            dotRt.anchorMin = new Vector2(0f, 0.5f);

            dotRt.anchorMax = new Vector2(0f, 0.5f);

            dotRt.pivot = new Vector2(0.5f, 0.5f);

            dotRt.anchoredPosition = new Vector2(24f, 10f);

            dotRt.sizeDelta = new Vector2(14f, 14f);

            Image dotImg = dotObj.GetComponent<Image>();

            dotImg.color = new Color(0f, 1f, 0.55f, 1f);

            // Status Text

            GameObject statusTextObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));

            statusTextObj.transform.SetParent(statusCard.transform, false);

            RectTransform stRt = statusTextObj.GetComponent<RectTransform>();

            stRt.anchorMin = new Vector2(0f, 0.45f);

            stRt.anchorMax = new Vector2(0.7f, 1f);

            stRt.offsetMin = new Vector2(42f, 0f);

            stRt.offsetMax = Vector2.zero;

            TextMeshProUGUI statusTmp = statusTextObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) statusTmp.font = _defaultFont;

            statusTmp.text = "<b>CONNECTED</b>";

            statusTmp.fontSize = 20;

            statusTmp.fontStyle = FontStyles.Bold;

            statusTmp.color = new Color(0f, 1f, 0.55f);

            statusTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Host Info Line

            GameObject hostObj = new GameObject("HostInfo", typeof(RectTransform), typeof(TextMeshProUGUI));

            hostObj.transform.SetParent(statusCard.transform, false);

            RectTransform hiRt = hostObj.GetComponent<RectTransform>();

            hiRt.anchorMin = new Vector2(0f, 0f);

            hiRt.anchorMax = new Vector2(1f, 0.45f);

            hiRt.offsetMin = new Vector2(24f, 0f);

            hiRt.offsetMax = new Vector2(-24f, 0f);

            TextMeshProUGUI hostTmp = hostObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) hostTmp.font = _defaultFont;

            hostTmp.text = "Host: <b>DESKTOP-VR-HOST</b>   |   IP: <b>192.168.1.145:8080</b>   |   Protocol: <b>WebRTC-H265 Ultra</b>   |   Cipher: <b>ChaCha20-Poly1305</b>";

            hostTmp.fontSize = 15;

            hostTmp.color = new Color(0.65f, 0.78f, 0.92f);

            hostTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // 2. Telemetry 4 Cards (Middle 38%)

            GameObject telemArea = new GameObject("TelemetryArea", typeof(RectTransform));

            telemArea.transform.SetParent(container.transform, false);

            RectTransform taRt = telemArea.GetComponent<RectTransform>();

            taRt.anchorMin = new Vector2(0.025f, 0.44f);

            taRt.anchorMax = new Vector2(0.975f, 0.75f);

            taRt.offsetMin = Vector2.zero;

            taRt.offsetMax = Vector2.zero;

            CreateGaugeCard(telemArea.transform, "Card_Lat", new Vector2(0f, 0f), new Vector2(0.25f, 1f), "STREAM LATENCY", "18.2 ms", new Color(0f, 1f, 0.55f), out TextMeshProUGUI latTmp, out _);

            CreateGaugeCard(telemArea.transform, "Card_Fps", new Vector2(0.25f, 0f), new Vector2(0.50f, 1f), "FRAME PACING", "60.0 FPS", new Color(0f, 0.9f, 1f), out TextMeshProUGUI fpsTmp, out _);

            CreateGaugeCard(telemArea.transform, "Card_Bit", new Vector2(0.50f, 0f), new Vector2(0.75f, 1f), "BANDWIDTH", "48.5 Mbps", new Color(1f, 0.75f, 0.2f), out TextMeshProUGUI bitTmp, out _);

            CreateGaugeCard(telemArea.transform, "Card_Loss", new Vector2(0.75f, 0f), new Vector2(1f, 1f), "PACKET LOSS", "0.01%", new Color(0.65f, 0.35f, 1f), out TextMeshProUGUI lossTmp, out _);

            // 3. Quality Presets Panel

            GameObject qCard = new GameObject("QualityCard", typeof(RectTransform), typeof(Image));

            qCard.transform.SetParent(container.transform, false);

            RectTransform qcRt = qCard.GetComponent<RectTransform>();

            qcRt.anchorMin = new Vector2(0.025f, 0.24f);

            qcRt.anchorMax = new Vector2(0.975f, 0.41f);

            qcRt.offsetMin = Vector2.zero;

            qcRt.offsetMax = Vector2.zero;

            Image qcImg = qCard.GetComponent<Image>();

            qcImg.color = new Color(0.03f, 0.055f, 0.095f, 0.98f);

            GameObject qTitleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));

            qTitleObj.transform.SetParent(qCard.transform, false);

            RectTransform qtRt = qTitleObj.GetComponent<RectTransform>();

            qtRt.anchorMin = new Vector2(0f, 0.5f);

            qtRt.anchorMax = new Vector2(0.32f, 0.5f);

            qtRt.pivot = new Vector2(0.5f, 0.5f);

            qtRt.anchoredPosition = new Vector2(100f, 0f);

            qtRt.sizeDelta = new Vector2(180f, 24f);

            TextMeshProUGUI qtTmp = qTitleObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) qtTmp.font = _defaultFont;

            qtTmp.text = "<b>QUALITY PRESET:</b>";

            qtTmp.fontSize = 16;

            qtTmp.color = Color.white;

            qtTmp.alignment = TextAlignmentOptions.MidlineLeft;

            Button lowBtn = CreatePresetBtn(qCard.transform, "LOW", new Vector2(0.32f, 0.5f), "720p 30fps");

            Button medBtn = CreatePresetBtn(qCard.transform, "MEDIUM", new Vector2(0.49f, 0.5f), "900p 60fps");

            Button highBtn = CreatePresetBtn(qCard.transform, "HIGH", new Vector2(0.66f, 0.5f), "1080p 60fps");

            Button ultraBtn = CreatePresetBtn(qCard.transform, "ULTRA", new Vector2(0.83f, 0.5f), "Ultra 1080p");

            // 4. Action Controls Toolbar (Bottom 20%)

            GameObject actCard = new GameObject("ActionCard", typeof(RectTransform), typeof(Image));

            actCard.transform.SetParent(container.transform, false);

            RectTransform acRt = actCard.GetComponent<RectTransform>();

            acRt.anchorMin = new Vector2(0.025f, 0.025f);

            acRt.anchorMax = new Vector2(0.975f, 0.21f);

            acRt.offsetMin = Vector2.zero;

            acRt.offsetMax = Vector2.zero;

            Image acImg = actCard.GetComponent<Image>();

            acImg.color = new Color(0.025f, 0.04f, 0.075f, 0.98f);

            Button connectBtn = CreateControlBtn(actCard.transform, "ConnectBtn", "> CONNECT", new Vector2(0.14f, 0.5f), new Color(0f, 0.75f, 0.45f));

            Button disconnectBtn = CreateControlBtn(actCard.transform, "DisconnectBtn", "[X] DISCONNECT", new Vector2(0.38f, 0.5f), new Color(0.65f, 0.20f, 0.30f));

            Button reconnectBtn = CreateControlBtn(actCard.transform, "ReconnectBtn", "RECONNECT", new Vector2(0.62f, 0.5f), new Color(0f, 0.55f, 0.85f));

            Button stressBtn = CreateControlBtn(actCard.transform, "StressBtn", "TEST LINK", new Vector2(0.86f, 0.5f), new Color(0.55f, 0.25f, 0.85f));

            // Wire ConnectionPanelApp

            ConnectionPanelApp app = parent.gameObject.GetComponent<ConnectionPanelApp>();

            if (app == null) app = parent.gameObject.AddComponent<ConnectionPanelApp>();

            app.Setup(dotImg, statusTmp, hostTmp, latTmp, fpsTmp, bitTmp, lossTmp,

                connectBtn, disconnectBtn, reconnectBtn, stressBtn,

                lowBtn, medBtn, highBtn, ultraBtn);
        }

        private Button CreatePresetBtn(Transform parent, string title, Vector2 anchorPos, string sub)

        {
            GameObject btnObj = new GameObject("Preset_" + title, typeof(RectTransform), typeof(Image), typeof(Button));

            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();

            rt.anchorMin = anchorPos;

            rt.anchorMax = anchorPos;

            rt.pivot = new Vector2(0.5f, 0.5f);

            rt.sizeDelta = new Vector2(110f, 34f);

            Image img = btnObj.GetComponent<Image>();

            img.color = new Color(0.06f, 0.10f, 0.16f, 0.85f);

            img.raycastTarget = true;

            Button btn = btnObj.GetComponent<Button>();

            ColorBlock cb = btn.colors;

            cb.normalColor = new Color(0.06f, 0.10f, 0.16f, 0.85f);

            cb.highlightedColor = new Color(0.12f, 0.22f, 0.36f, 0.95f);

            cb.pressedColor = new Color(0f, 0.65f, 0.85f, 1f);

            btn.colors = cb;

            GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            tObj.transform.SetParent(btnObj.transform, false);

            StretchFull(tObj.GetComponent<RectTransform>());

            TextMeshProUGUI tmp = tObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tmp.font = _defaultFont;

            tmp.text = $"<b>{title}</b>\n<size=11><color=#88a8c8>{sub}</color></size>";

            tmp.fontSize = 15;

            tmp.alignment = TextAlignmentOptions.Center;

            tmp.color = Color.white;

            tmp.raycastTarget = false;

            return btn;
        }

        private Button CreateControlBtn(Transform parent, string name, string label, Vector2 anchorPos, Color color)

        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();

            rt.anchorMin = anchorPos;

            rt.anchorMax = anchorPos;

            rt.pivot = new Vector2(0.5f, 0.5f);

            rt.sizeDelta = new Vector2(150f, 36f);

            Image img = btnObj.GetComponent<Image>();

            img.color = color;

            img.raycastTarget = true;

            Button btn = btnObj.GetComponent<Button>();

            ColorBlock cb = btn.colors;

            cb.normalColor = color;

            cb.highlightedColor = color * 1.2f;

            cb.pressedColor = color * 0.8f;

            btn.colors = cb;

            GameObject tObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            tObj.transform.SetParent(btnObj.transform, false);

            StretchFull(tObj.GetComponent<RectTransform>());

            TextMeshProUGUI tmp = tObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tmp.font = _defaultFont;

            tmp.text = label;

            tmp.fontSize = 15;

            tmp.fontStyle = FontStyles.Bold;

            tmp.alignment = TextAlignmentOptions.Center;

            tmp.color = Color.white;

            tmp.raycastTarget = false;

            return btn;
        }

        // =========================================================================

        // PROCEDURAL INPUT FIELD & NOTEPAD APP

        // =========================================================================

        public TMP_InputField CreateStyledInputField(

            Transform parent,

            string name,

            string placeholderText,

            int fontSize,

            bool isMultiLine = false)

        {
            // Root Object with Image for Raycasting

            GameObject rootObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));

            rootObj.transform.SetParent(parent, false);

            Image rootImg = rootObj.GetComponent<Image>();

            rootImg.color = new Color(0.04f, 0.07f, 0.13f, 0.98f); // dark navy -- clearly visible background

            rootImg.raycastTarget = true;

            // Border

            GameObject borderObj = new GameObject("FieldBorder", typeof(RectTransform), typeof(Image));

            borderObj.transform.SetParent(rootObj.transform, false);

            RectTransform bRt = borderObj.GetComponent<RectTransform>();

            StretchFull(bRt);

            bRt.offsetMin = new Vector2(-1, -1);

            bRt.offsetMax = new Vector2(1, 1);

            Image bImg = borderObj.GetComponent<Image>();

            bImg.color = new Color(0f, 0.8f, 1f, 0.45f);

            bImg.raycastTarget = false;

            bRt.SetAsFirstSibling();

            // Text Area (Viewport with RectMask2D)

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));

            textArea.transform.SetParent(rootObj.transform, false);

            RectTransform taRt = textArea.GetComponent<RectTransform>();

            StretchFull(taRt);

            taRt.offsetMin = new Vector2(10, 4);

            taRt.offsetMax = new Vector2(-10, -4);

            // Text Component

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            textObj.transform.SetParent(textArea.transform, false);

            RectTransform tRt = textObj.GetComponent<RectTransform>();

            StretchFull(tRt);

            TextMeshProUGUI textTmp = textObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) textTmp.font = _defaultFont;

            textTmp.fontSize = fontSize;

            // Crisp bright white: highly readable on dark bg

            textTmp.color = new Color(0.95f, 0.98f, 1f, 1f);

            textTmp.alignment = isMultiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;

            textTmp.raycastTarget = false;

            textTmp.richText = true;

            textTmp.textWrappingMode = isMultiLine ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;

            textTmp.overflowMode = isMultiLine ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;

            // Placeholder Component

            GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));

            phObj.transform.SetParent(textArea.transform, false);

            RectTransform phRt = phObj.GetComponent<RectTransform>();

            StretchFull(phRt);

            TextMeshProUGUI phTmp = phObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) phTmp.font = _defaultFont;

            phTmp.fontSize = fontSize;

            phTmp.color = new Color(0.6f, 0.72f, 0.88f, 0.85f); // clear readable blue-gray placeholder

            phTmp.fontStyle = FontStyles.Italic;

            phTmp.text = placeholderText;

            phTmp.alignment = isMultiLine ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;

            phTmp.raycastTarget = false;

            // TMP_InputField Setup

            TMP_InputField inputField = rootObj.GetComponent<TMP_InputField>();

            inputField.targetGraphic = rootImg;

            inputField.textViewport = taRt;

            inputField.textComponent = textTmp;

            inputField.placeholder = phTmp;

            if (_defaultFont != null) inputField.fontAsset = _defaultFont;

            inputField.pointSize = fontSize;

            inputField.lineType = isMultiLine ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;

            inputField.caretWidth = 2;

            inputField.customCaretColor = true;

            inputField.caretColor = Color.cyan;

            inputField.selectionColor = new Color(0f, 0.8f, 1f, 0.4f);

            inputField.interactable = true;

            inputField.readOnly = false;

            inputField.onFocusSelectAll = false; // Prevent selecting entire document on focus

            return inputField;
        }

        private void BuildNotepadContent(DesktopWindow win, RectTransform parent)

        {
            // Background

            GameObject bgObj = new GameObject("NotepadBg", typeof(RectTransform), typeof(Image));

            bgObj.transform.SetParent(parent, false);

            RectTransform bgRt = bgObj.GetComponent<RectTransform>();

            StretchFull(bgRt);

            Image bgImg = bgObj.GetComponent<Image>();

            bgImg.color = new Color(0.02f, 0.035f, 0.06f, 0.98f);

            // 1. Top Toolbar (Actions: NEW, OPEN, SAVE, CLEAR, KEYBOARD)

            GameObject toolbar = new GameObject("Toolbar", typeof(RectTransform), typeof(Image));

            toolbar.transform.SetParent(parent, false);

            RectTransform tbRt = toolbar.GetComponent<RectTransform>();

            tbRt.anchorMin = new Vector2(0, 1);

            tbRt.anchorMax = new Vector2(1, 1);

            tbRt.pivot = new Vector2(0.5f, 1);

            tbRt.anchoredPosition = Vector2.zero;

            tbRt.sizeDelta = new Vector2(0, 36);

            Image tbImg = toolbar.GetComponent<Image>();

            tbImg.color = new Color(0.06f, 0.09f, 0.15f, 0.98f);

            // Toolbar buttons container

            GameObject btnCont = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));

            btnCont.transform.SetParent(toolbar.transform, false);

            RectTransform bcRt = btnCont.GetComponent<RectTransform>();

            bcRt.anchorMin = new Vector2(0, 0);

            bcRt.anchorMax = new Vector2(0.8f, 1);

            bcRt.offsetMin = new Vector2(8, 4);

            bcRt.offsetMax = new Vector2(0, -4);

            HorizontalLayoutGroup bhlg = btnCont.GetComponent<HorizontalLayoutGroup>();

            bhlg.spacing = 6;

            bhlg.childForceExpandWidth = false;

            bhlg.childForceExpandHeight = true;

            Button newBtn = CreateNotepadToolbarButton(btnCont.transform, "NewBtn", "+ NEW", new Color(0f, 0.85f, 1f));

            Button openBtn = CreateNotepadToolbarButton(btnCont.transform, "OpenBtn", "OPEN", new Color(0.2f, 0.8f, 0.95f));

            Button saveBtn = CreateNotepadToolbarButton(btnCont.transform, "SaveBtn", "SAVE", new Color(0.0f, 1f, 0.6f));

            Button clearBtn = CreateNotepadToolbarButton(btnCont.transform, "ClearBtn", "CLEAR", new Color(1f, 0.35f, 0.45f));

            // Keyboard Toggle Button on Right

            GameObject kbBtnObj = new GameObject("KbBtn", typeof(RectTransform), typeof(Image), typeof(Button));

            kbBtnObj.transform.SetParent(toolbar.transform, false);

            RectTransform kbRt = kbBtnObj.GetComponent<RectTransform>();

            kbRt.anchorMin = new Vector2(1, 0.5f);

            kbRt.anchorMax = new Vector2(1, 0.5f);

            kbRt.pivot = new Vector2(1, 0.5f);

            kbRt.anchoredPosition = new Vector2(-10, 0);

            kbRt.sizeDelta = new Vector2(110, 26);

            Image kbImg = kbBtnObj.GetComponent<Image>();

            kbImg.color = new Color(0.6f, 0.2f, 0.9f, 0.35f);

            Button kbBtn = kbBtnObj.GetComponent<Button>();

            GameObject kbTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            kbTextObj.transform.SetParent(kbBtnObj.transform, false);

            StretchFull(kbTextObj.GetComponent<RectTransform>());

            TextMeshProUGUI kbTmp = kbTextObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) kbTmp.font = _defaultFont;

            kbTmp.text = "KEYBOARD";

            kbTmp.fontSize = 14;

            kbTmp.fontStyle = FontStyles.Bold;

            kbTmp.alignment = TextAlignmentOptions.Center;

            kbTmp.color = new Color(0.9f, 0.5f, 1f);

            kbTmp.raycastTarget = false;

            // 2. Document Title Sub-bar

            GameObject docSubBar = new GameObject("DocSubBar", typeof(RectTransform), typeof(Image));

            docSubBar.transform.SetParent(parent, false);

            RectTransform dsbRt = docSubBar.GetComponent<RectTransform>();

            dsbRt.anchorMin = new Vector2(0, 1);

            dsbRt.anchorMax = new Vector2(1, 1);

            dsbRt.pivot = new Vector2(0.5f, 1);

            dsbRt.anchoredPosition = new Vector2(0, -36);

            dsbRt.sizeDelta = new Vector2(0, 26);

            Image dsbImg = docSubBar.GetComponent<Image>();

            dsbImg.color = new Color(0.04f, 0.06f, 0.10f, 0.95f);

            GameObject docNameObj = new GameObject("DocNameText", typeof(RectTransform), typeof(TextMeshProUGUI));

            docNameObj.transform.SetParent(docSubBar.transform, false);

            RectTransform dnRt = docNameObj.GetComponent<RectTransform>();

            StretchFull(dnRt);

            dnRt.offsetMin = new Vector2(14, 0);

            dnRt.offsetMax = new Vector2(-14, 0);

            TextMeshProUGUI docNameTmp = docNameObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) docNameTmp.font = _defaultFont;

            docNameTmp.fontSize = 16;

            docNameTmp.alignment = TextAlignmentOptions.MidlineLeft;

            docNameTmp.color = Color.white;

            docNameTmp.text = "<color=#00ffff><b>notes.txt</b></color> <color=#888888>[UTF-8]</color> <color=#00ff88>[SAVED]</color>";

            // 4. Bottom Status Bar (28px fixed height at bottom -- created BEFORE editor so Z-order is correct)

            GameObject statusBar = new GameObject("StatusBar", typeof(RectTransform), typeof(Image));

            statusBar.transform.SetParent(parent, false);

            RectTransform sbRt = statusBar.GetComponent<RectTransform>();

            // Fixed 28px bar anchored to bottom edge -- no percentage, pixel-perfect

            sbRt.anchorMin = new Vector2(0, 0);

            sbRt.anchorMax = new Vector2(1, 0);

            sbRt.pivot = new Vector2(0.5f, 0);

            sbRt.anchoredPosition = new Vector2(0, 4);

            sbRt.sizeDelta = new Vector2(-16, 28);

            Image sbImg = statusBar.GetComponent<Image>();

            sbImg.color = new Color(0.04f, 0.06f, 0.11f, 0.96f);

            GameObject sbTextObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));

            sbTextObj.transform.SetParent(statusBar.transform, false);

            RectTransform sbtRt = sbTextObj.GetComponent<RectTransform>();

            StretchFull(sbtRt);

            sbtRt.offsetMin = new Vector2(12, 0);

            sbtRt.offsetMax = new Vector2(-12, 0);

            TextMeshProUGUI sbTmp = sbTextObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) sbTmp.font = _defaultFont;

            sbTmp.fontSize = 15;

            sbTmp.alignment = TextAlignmentOptions.MidlineLeft;

            sbTmp.color = new Color(0.8f, 0.88f, 0.95f, 1f); // brighter status bar text

            sbTmp.text = "<color=#00ffff>Lines:</color> 1  |  <color=#00ffff>Words:</color> 0  |  <color=#00ffff>Chars:</color> 0  |  <color=#00ff88>READY</color>";

            // 3. Main Multi-line Text Editor

            // Stretches between toolbar+subbar (top 62px) and status bar (bottom 32px)

            // Uses anchor-stretch + fixed offsets -- NO percentage anchors that change with window size

            TMP_InputField editorInputField = CreateStyledInputField(parent, "NotepadEditor", "Start typing here... (use toolbar to load samples)", 18, true);

            RectTransform edRt = editorInputField.GetComponent<RectTransform>();

            edRt.anchorMin = Vector2.zero;           // stretch full width

            edRt.anchorMax = Vector2.one;            // stretch full height

            edRt.pivot = new Vector2(0.5f, 0.5f);

            edRt.offsetMin = new Vector2(8, 34);    // 8px left margin, 34px above status bar

            edRt.offsetMax = new Vector2(-8, -66);  // 8px right margin, 66px below top (toolbar36+subbar26+4gap)

            // Attach NotepadApp

            NotepadApp notepad = parent.gameObject.GetComponent<NotepadApp>();

            if (notepad == null) notepad = parent.gameObject.AddComponent<NotepadApp>();

            notepad.Setup(editorInputField, sbTmp, docNameTmp, newBtn, openBtn, saveBtn, clearBtn, kbBtn, win);
        }

        private Button CreateNotepadToolbarButton(Transform parent, string name, string label, Color color)

        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));

            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();

            rt.sizeDelta = new Vector2(82, 26);

            Image img = btnObj.GetComponent<Image>();

            img.color = new Color(color.r, color.g, color.b, 0.2f);

            img.raycastTarget = true;

            Button btn = btnObj.GetComponent<Button>();

            ColorBlock cb = btn.colors;

            cb.normalColor = new Color(color.r, color.g, color.b, 0.2f);

            cb.highlightedColor = new Color(color.r, color.g, color.b, 0.5f);

            cb.pressedColor = new Color(color.r, color.g, color.b, 0.8f);

            btn.colors = cb;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));

            textObj.transform.SetParent(btnObj.transform, false);

            StretchFull(textObj.GetComponent<RectTransform>());

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tmp.font = _defaultFont;

            tmp.text = label;

            tmp.fontSize = 14;

            tmp.fontStyle = FontStyles.Bold;

            tmp.alignment = TextAlignmentOptions.Center;

            tmp.color = color;

            tmp.raycastTarget = false;

            return btn;
        }

        private void BuildLiveCamContent(DesktopWindow win, RectTransform parent)

        {
            // 1. Top Control Bar (38px high)

            GameObject toolbar = new GameObject("LiveCamToolbar", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));

            toolbar.transform.SetParent(parent, false);

            RectTransform tbRt = toolbar.GetComponent<RectTransform>();

            tbRt.anchorMin = new Vector2(0, 1);

            tbRt.anchorMax = new Vector2(1, 1);

            tbRt.pivot = new Vector2(0.5f, 1);

            tbRt.anchoredPosition = Vector2.zero;

            tbRt.sizeDelta = new Vector2(0, 38);

            Image tbImg = toolbar.GetComponent<Image>();

            tbImg.color = new Color(0.04f, 0.08f, 0.14f, 0.96f);

            HorizontalLayoutGroup tbHlg = toolbar.GetComponent<HorizontalLayoutGroup>();

            tbHlg.padding = new RectOffset(10, 10, 4, 4);

            tbHlg.spacing = 8f;

            tbHlg.childAlignment = TextAnchor.MiddleLeft;

            tbHlg.childControlWidth = false;

            tbHlg.childControlHeight = true;

            tbHlg.childForceExpandWidth = false;

            tbHlg.childForceExpandHeight = true;

            // Rec status badge

            GameObject recObj = new GameObject("RecBadge", typeof(RectTransform), typeof(TextMeshProUGUI));

            recObj.transform.SetParent(toolbar.transform, false);

            RectTransform recRt = recObj.GetComponent<RectTransform>();

            recRt.sizeDelta = new Vector2(130, 28);

            TextMeshProUGUI recTmp = recObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) recTmp.font = _defaultFont;

            recTmp.fontSize = 14;

            recTmp.alignment = TextAlignmentOptions.MidlineLeft;

            recTmp.text = "<color=#00ff88><b>| REC [LIVE]</b></color>";

            // Angle Switcher Buttons

            Button btnCctv = CreateNotepadToolbarButton(toolbar.transform, "Btn_CCTV", "[ROOM] ROOM CCTV", new Color(0f, 0.9f, 1f));

            btnCctv.GetComponent<RectTransform>().sizeDelta = new Vector2(125, 28);

            Button btnWebcam = CreateNotepadToolbarButton(toolbar.transform, "Btn_Webcam", "[WEBCAM] WEBCAM", new Color(0.2f, 1f, 0.6f));

            btnWebcam.GetComponent<RectTransform>().sizeDelta = new Vector2(110, 28);

            Button btnTp = CreateNotepadToolbarButton(toolbar.transform, "Btn_ThirdPerson", "[USER] 3RD PERSON", new Color(1f, 0.75f, 0.2f));

            btnTp.GetComponent<RectTransform>().sizeDelta = new Vector2(125, 28);

            // 2. Subbar for Camera Details & Dynamic Clock (26px high)

            GameObject subBar = new GameObject("LiveCamSubBar", typeof(RectTransform), typeof(Image));

            subBar.transform.SetParent(parent, false);

            RectTransform sbRt = subBar.GetComponent<RectTransform>();

            sbRt.anchorMin = new Vector2(0, 1);

            sbRt.anchorMax = new Vector2(1, 1);

            sbRt.pivot = new Vector2(0.5f, 1);

            sbRt.anchoredPosition = new Vector2(0, -38);

            sbRt.sizeDelta = new Vector2(0, 26);

            Image sbImg = subBar.GetComponent<Image>();

            sbImg.color = new Color(0.02f, 0.04f, 0.08f, 0.95f);

            GameObject camLabelObj = new GameObject("CamLabel", typeof(RectTransform), typeof(TextMeshProUGUI));

            camLabelObj.transform.SetParent(subBar.transform, false);

            RectTransform clRt = camLabelObj.GetComponent<RectTransform>();

            clRt.anchorMin = new Vector2(0, 0);

            clRt.anchorMax = new Vector2(0.6f, 1);

            clRt.offsetMin = new Vector2(12, 0);

            clRt.offsetMax = Vector2.zero;

            TextMeshProUGUI clTmp = camLabelObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) clTmp.font = _defaultFont;

            clTmp.fontSize = 13;

            clTmp.alignment = TextAlignmentOptions.MidlineLeft;

            clTmp.color = new Color(0.4f, 0.85f, 1f, 1f);

            clTmp.text = "CAM 01 // OVERHEAD CCTV - FULL ROOM COVERAGE";

            GameObject clockObj = new GameObject("ClockFps", typeof(RectTransform), typeof(TextMeshProUGUI));

            clockObj.transform.SetParent(subBar.transform, false);

            RectTransform clockRt = clockObj.GetComponent<RectTransform>();

            clockRt.anchorMin = new Vector2(0.6f, 0);

            clockRt.anchorMax = new Vector2(1, 1);

            clockRt.offsetMin = Vector2.zero;

            clockRt.offsetMax = new Vector2(-12, 0);

            TextMeshProUGUI clockTmp = clockObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) clockTmp.font = _defaultFont;

            clockTmp.fontSize = 13;

            clockTmp.alignment = TextAlignmentOptions.MidlineRight;

            clockTmp.color = new Color(0.8f, 0.9f, 1f, 0.8f);

            clockTmp.text = "2026-10-19 [14:35:00] // 60 FPS";

            // 3. Bottom Status Bar (28px high)

            GameObject botBar = new GameObject("LiveCamBottomBar", typeof(RectTransform), typeof(Image));

            botBar.transform.SetParent(parent, false);

            RectTransform bbRt = botBar.GetComponent<RectTransform>();

            bbRt.anchorMin = new Vector2(0, 0);

            bbRt.anchorMax = new Vector2(1, 0);

            bbRt.pivot = new Vector2(0.5f, 0);

            bbRt.anchoredPosition = new Vector2(0, 4);

            bbRt.sizeDelta = new Vector2(-16, 28);

            Image bbImg = botBar.GetComponent<Image>();

            bbImg.color = new Color(0.03f, 0.05f, 0.09f, 0.95f);

            GameObject targetStatusObj = new GameObject("TargetStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));

            targetStatusObj.transform.SetParent(botBar.transform, false);

            RectTransform tsRt = targetStatusObj.GetComponent<RectTransform>();

            StretchFull(tsRt);

            tsRt.offsetMin = new Vector2(12, 0);

            tsRt.offsetMax = new Vector2(-12, 0);

            TextMeshProUGUI tsTmp = targetStatusObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) tsTmp.font = _defaultFont;

            tsTmp.fontSize = 13;

            tsTmp.alignment = TextAlignmentOptions.MidlineLeft;

            tsTmp.color = new Color(0.7f, 0.85f, 0.95f, 1f);

            tsTmp.text = "TARGET: <color=#00ffff>PLAYER AVATAR // CYBER SUIT</color> | STATUS: <color=#00ff88>TRACKING LOCK</color>";

            // 4. Center Viewport with RawImage Feed (occupies remaining height)

            GameObject feedObj = new GameObject("CCTV_FeedDisplay", typeof(RectTransform), typeof(RawImage));

            feedObj.transform.SetParent(parent, false);

            RectTransform feedRt = feedObj.GetComponent<RectTransform>();

            feedRt.anchorMin = Vector2.zero;

            feedRt.anchorMax = Vector2.one;

            feedRt.pivot = new Vector2(0.5f, 0.5f);

            feedRt.offsetMin = new Vector2(8, 36);   // Above bottom bar

            feedRt.offsetMax = new Vector2(-8, -68); // Below top bars

            RawImage rawImg = feedObj.GetComponent<RawImage>();

            rawImg.color = Color.white;

            if (VRRemoteDesktop.Environment.RoomSurveillanceCamera.Instance != null)

            {
                rawImg.texture = VRRemoteDesktop.Environment.RoomSurveillanceCamera.Instance.FeedTexture;
            }

            // Subtle Cyber Crosshair Reticle in center of feed

            GameObject reticle = new GameObject("ReticleOverlay", typeof(RectTransform), typeof(TextMeshProUGUI));

            reticle.transform.SetParent(feedObj.transform, false);

            RectTransform retRt = reticle.GetComponent<RectTransform>();

            retRt.anchorMin = new Vector2(0.5f, 0.5f);

            retRt.anchorMax = new Vector2(0.5f, 0.5f);

            retRt.sizeDelta = new Vector2(120, 120);

            TextMeshProUGUI retTmp = reticle.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) retTmp.font = _defaultFont;

            retTmp.fontSize = 32;

            retTmp.alignment = TextAlignmentOptions.Center;

            retTmp.color = new Color(0f, 0.9f, 1f, 0.25f);

            retTmp.text = "+";

            retTmp.raycastTarget = false;

            // Attach LiveCamApp controller

            LiveCamApp app = parent.gameObject.GetComponent<LiveCamApp>();

            if (app == null) app = parent.gameObject.AddComponent<LiveCamApp>();

            app.Setup(rawImg, recTmp, clTmp, clockTmp, tsTmp, btnCctv, btnWebcam, btnTp);
        }

        // =========================================================================

        // DYNAMIC SHORTCUTS & HELPERS

        // =========================================================================

        private void EnsureDesktopShortcuts()

        {
            if (_canvasRect == null) return;

            Transform gridTr = _canvasRect.Find("DesktopGrid");

            if (gridTr == null) gridTr = _canvasRect.Find("AppGrid");

            if (gridTr == null)

            {
                var icon = _canvasRect.GetComponentInChildren<DesktopIcon>();

                if (icon != null && icon.transform.parent != null)

                {
                    gridTr = icon.transform.parent;
                }
            }

            if (gridTr != null)

            {
                Transform notepadIconTr = gridTr.Find("Icon_Notepad");

                if (notepadIconTr == null) notepadIconTr = gridTr.Find("Icon_Notes");

                if (notepadIconTr == null)

                {
                    notepadIconTr = CreateDesktopShortcutIcon(gridTr, "Notepad", "Cyber Notes", "NOTE", new Color(0.85f, 0.35f, 1f));
                }

                // Place Notepad icon right after Browser

                Transform browserTr = gridTr.Find("Icon_Browser");

                if (browserTr != null && notepadIconTr != null)

                {
                    notepadIconTr.SetSiblingIndex(browserTr.GetSiblingIndex() + 1);
                }

                Transform connIconTr = gridTr.Find("Icon_Connection");

                if (connIconTr == null) connIconTr = gridTr.Find("Icon_RemoteLink");

                if (connIconTr == null)

                {
                    connIconTr = CreateDesktopShortcutIcon(gridTr, "Connection", "Remote Host", "LINK", new Color(0f, 0.9f, 0.6f));
                }

                Transform liveCamIconTr = gridTr.Find("Icon_LiveCam");

                if (liveCamIconTr == null) liveCamIconTr = gridTr.Find("Icon_Camera");

                if (liveCamIconTr == null)

                {
                    liveCamIconTr = CreateDesktopShortcutIcon(gridTr, "LiveCam", "CCTV Cam", "CAM", new Color(0.2f, 0.85f, 1f));
                }

                RearrangeDesktopGrid(gridTr as RectTransform);
            }
        }

        private void RearrangeDesktopGrid(RectTransform gridRt)

        {
            if (gridRt == null) return;

            // Disable single-column VerticalLayoutGroup so icons can form a clean 2-column grid

            var vlg = gridRt.GetComponent<VerticalLayoutGroup>();

            if (vlg != null)

            {
                vlg.enabled = false;
            }

            float itemWidth = 130f;

            float itemHeight = 104f;

            float colSpacing = 16f;

            float rowSpacing = 12f;

            int maxPerCol = 4; // 4 rows max per column -> fits 8 icons in 2 clean columns in upper-left desktop!

            gridRt.sizeDelta = new Vector2((itemWidth * 2f) + colSpacing + 20f, (itemHeight * maxPerCol) + (rowSpacing * maxPerCol));

            int activeIndex = 0;

            for (int i = 0; i < gridRt.childCount; i++)

            {
                RectTransform child = gridRt.GetChild(i) as RectTransform;

                if (child == null || !child.gameObject.activeSelf) continue;

                int col = activeIndex / maxPerCol;

                int row = activeIndex % maxPerCol;

                float xPos = col * (itemWidth + colSpacing);

                float yPos = row * (itemHeight + rowSpacing);

                child.anchorMin = new Vector2(0f, 1f);

                child.anchorMax = new Vector2(0f, 1f);

                child.pivot = new Vector2(0f, 1f);

                child.sizeDelta = new Vector2(itemWidth, itemHeight);

                child.anchoredPosition = new Vector2(xPos, -yPos);

                LayoutElement le = child.GetComponent<LayoutElement>();

                if (le == null) le = child.gameObject.AddComponent<LayoutElement>();

                le.preferredHeight = itemHeight;

                le.minHeight = itemHeight;

                le.preferredWidth = itemWidth;

                activeIndex++;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRt);
        }

        private Transform CreateDesktopShortcutIcon(Transform parent, string title, string sub, string symbol, Color accentColor)

        {
            GameObject item = new GameObject($"Icon_{title}", typeof(RectTransform), typeof(Button), typeof(DesktopIcon));

            item.transform.SetParent(parent, false);

            RectTransform itemRt = item.GetComponent<RectTransform>();

            itemRt.anchorMin = new Vector2(0.5f, 1f);

            itemRt.anchorMax = new Vector2(0.5f, 1f);

            itemRt.pivot = new Vector2(0.5f, 1f);

            itemRt.sizeDelta = new Vector2(130f, 114f);

            // Icon Box

            GameObject box = new GameObject("IconBox", typeof(RectTransform), typeof(Image));

            box.transform.SetParent(item.transform, false);

            Image boxImg = box.GetComponent<Image>();

            boxImg.color = new Color(0.12f, 0.05f, 0.18f, 0.9f);

            boxImg.raycastTarget = true;

            RectTransform boxRt = box.GetComponent<RectTransform>();

            boxRt.anchorMin = new Vector2(0.5f, 1f);

            boxRt.anchorMax = new Vector2(0.5f, 1f);

            boxRt.pivot = new Vector2(0.5f, 1f);

            boxRt.anchoredPosition = Vector2.zero;

            boxRt.sizeDelta = new Vector2(66f, 66f);

            // Border Line

            GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));

            border.transform.SetParent(box.transform, false);

            Image borderImg = border.GetComponent<Image>();

            borderImg.color = accentColor;

            borderImg.raycastTarget = false;

            RectTransform borderRt = border.GetComponent<RectTransform>();

            borderRt.anchorMin = new Vector2(0f, 0f);

            borderRt.anchorMax = new Vector2(1f, 0f);

            borderRt.pivot = new Vector2(0.5f, 0f);

            borderRt.anchoredPosition = Vector2.zero;

            borderRt.sizeDelta = new Vector2(0f, 2.5f);

            // Symbol

            GameObject symObj = new GameObject("Symbol", typeof(RectTransform), typeof(TextMeshProUGUI));

            symObj.transform.SetParent(box.transform, false);

            RectTransform symRt = symObj.GetComponent<RectTransform>();

            StretchFull(symRt);

            TextMeshProUGUI symText = symObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) symText.font = _defaultFont;

            symText.text = symbol;

            symText.fontSize = 24;

            symText.fontStyle = FontStyles.Bold;

            symText.alignment = TextAlignmentOptions.Center;

            symText.color = accentColor;

            symText.raycastTarget = false;

            // Title Label

            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));

            titleObj.transform.SetParent(item.transform, false);

            RectTransform tRt = titleObj.GetComponent<RectTransform>();

            tRt.anchorMin = new Vector2(0f, 0f);

            tRt.anchorMax = new Vector2(1f, 0f);

            tRt.pivot = new Vector2(0.5f, 0f);

            tRt.anchoredPosition = new Vector2(0f, 24f);

            tRt.sizeDelta = new Vector2(0f, 24f);

            TextMeshProUGUI titleLabel = titleObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) titleLabel.font = _defaultFont;

            titleLabel.text = title;

            titleLabel.fontSize = 20;

            titleLabel.fontStyle = FontStyles.Bold;

            titleLabel.alignment = TextAlignmentOptions.Center;

            titleLabel.color = Color.white;

            titleLabel.raycastTarget = false;

            // Subtitle Label

            GameObject subObj = new GameObject("Sub", typeof(RectTransform), typeof(TextMeshProUGUI));

            subObj.transform.SetParent(item.transform, false);

            RectTransform sRt = subObj.GetComponent<RectTransform>();

            sRt.anchorMin = new Vector2(0f, 0f);

            sRt.anchorMax = new Vector2(1f, 0f);

            sRt.pivot = new Vector2(0.5f, 0f);

            sRt.anchoredPosition = new Vector2(0f, 3f);

            sRt.sizeDelta = new Vector2(0f, 20f);

            TextMeshProUGUI subLabel = subObj.GetComponent<TextMeshProUGUI>();

            if (_defaultFont != null) subLabel.font = _defaultFont;

            subLabel.text = sub;

            subLabel.fontSize = 16;

            subLabel.alignment = TextAlignmentOptions.Center;

            subLabel.color = new Color(0.78f, 0.85f, 0.95f, 1f);

            subLabel.raycastTarget = false;

            // Setup DesktopIcon

            DesktopIcon dIcon = item.GetComponent<DesktopIcon>();

            dIcon.Setup(title, sub, accentColor);

            SetPrivateField(dIcon, "_iconBackground", boxImg);

            SetPrivateField(dIcon, "_iconBorder", borderImg);

            SetPrivateField(dIcon, "_titleText", titleLabel);

            SetPrivateField(dIcon, "_subText", subLabel);

            Button btn = item.GetComponent<Button>();

            btn.onClick.AddListener(() => OpenWindow(title));

            return item.transform;
        }

        private static void StretchFull(RectTransform rt)

        {
            if (rt == null) return;

            rt.anchorMin = Vector2.zero;

            rt.anchorMax = Vector2.one;

            rt.offsetMin = Vector2.zero;

            rt.offsetMax = Vector2.zero;
        }

        private static void SetPrivateField(object target, string fieldName, object value)

        {
            if (target == null) return;

            var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (f != null)

            {
                f.SetValue(target, value);
            }
        }
    }
}
