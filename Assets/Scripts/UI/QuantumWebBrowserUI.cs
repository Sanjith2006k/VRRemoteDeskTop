using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.Audio;
using VRRemoteDesktop.Networking;

namespace VRRemoteDesktop.UI
{
    /// <summary>
    /// Quantum Web Browser application rendered natively on the Left Auxiliary Display (L-01).
    /// Provides 1920x1080 high-resolution visuals, multi-tab browsing, interactive search engine,
    /// live tech news feed with voting, conversational AI assistant (Nexus AI), embedded CCTV security hub,
    /// real-time cloud CDN telemetry, theme customizer, and a playable Neon Snake cyber arcade game.
    /// </summary>
    public class QuantumWebBrowserUI : MonoBehaviour
    {
        public static QuantumWebBrowserUI Instance { get; private set; }

        public enum BrowserTab
        {
            QuantumSearch = 0,
            HackerFeed = 1,
            NexusAI = 2,
            CCTVSecurity = 3,
            CloudCDN = 4,
            CyberArcade = 5,
            Themes = 6
        }

        [Header("State")]
        [SerializeField] private BrowserTab _currentTab = BrowserTab.QuantumSearch;
        private List<string> _history = new List<string>();
        private int _historyIndex = -1;

        // UI Roots
        private RectTransform _browserRoot;
        private RectTransform _tabBarContainer;
        private RectTransform _viewportContainer;
        private Image _loadingProgressBar;

        // Navigation Bar Controls
        private Button _backBtn;
        private Button _forwardBtn;
        private Button _refreshBtn;
        private Button _homeBtn;
        private TMP_InputField _urlInput;
        private Button _goBtn;
        private Button[] _tabButtons = new Button[7];
        private TextMeshProUGUI[] _tabTexts = new TextMeshProUGUI[7];
        private Image[] _tabBgs = new Image[7];

        // Page Containers
        private GameObject[] _pages = new GameObject[7];

        // --- Tab 0: Quantum Search References ---
        private TMP_InputField _searchInput;
        private TextMeshProUGUI _searchResultsText;
        private GameObject _searchResultsBox;

        // --- Tab 1: HackerFeed References ---
        private int[] _articleUpvotes = new int[] { 342, 289, 215, 178 };
        private TextMeshProUGUI[] _upvoteTexts = new TextMeshProUGUI[4];
        private TextMeshProUGUI[] _articleHeadlineTexts = new TextMeshProUGUI[4];
        private TextMeshProUGUI[] _articleMetaTexts = new TextMeshProUGUI[4];
        private TextMeshProUGUI[] _articleDescTexts = new TextMeshProUGUI[4];

        // --- Tab 2: Nexus AI References ---
        private TextMeshProUGUI _aiChatLogText;
        private TMP_InputField _aiChatInput;
        private ScrollRect _aiScrollRect;
        private System.Text.StringBuilder _chatHistory = new System.Text.StringBuilder();

        // --- Tab 3: CCTV Hub References ---
        private TextMeshProUGUI _cctvStatusText;
        private TextMeshProUGUI _cctvFeedLabel;
        private TextMeshProUGUI _cctvLogStream;
        private int _selectedCameraIndex = 0;

        // --- Tab 4: Cloud CDN References ---
        private TextMeshProUGUI _pingSummaryText;
        private Slider _pingTestSlider;
        private bool _isPingTesting = false;

        // --- Tab 5: Neon Snake Arcade References ---
        private const int GRID_SIZE = 16;
        private Image[,] _gridCells = new Image[GRID_SIZE, GRID_SIZE];
        private List<Vector2Int> _snakeBody = new List<Vector2Int>();
        private Vector2Int _snakeDir = Vector2Int.right;
        private Vector2Int _foodPos = new Vector2Int(10, 8);
        private int _snakeScore = 0;
        private int _snakeHighScore = 0;
        private bool _gameRunning = false;
        private Coroutine _snakeCoroutine;
        private TextMeshProUGUI _scoreText;
        private TextMeshProUGUI _gameStatusText;

        // Accent Colors
        private Color _primaryAccent = new Color(0f, 1f, 1f, 1f); // Cyan
        private Color _bgDark = new Color(0.025f, 0.045f, 0.08f, 0.98f);
        private Color _cardDark = new Color(0.04f, 0.07f, 0.13f, 0.95f);

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
            BuildBrowserUI();
            NavigateTo(BrowserTab.QuantumSearch, "https://quantum-vr.net/search");
            FetchLiveHackerFeed();
        }

        private void OnDisable()
        {
            ResetSnakeGame();
        }

        private void Update()
        {
            // Keyboard controls for Cyber Arcade game when active
            if (_currentTab == BrowserTab.CyberArcade && _gameRunning)
            {
                bool up = false, down = false, left = false, right = false;

                // Check New Input System Keyboard safely
                try
                {
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb != null)
                    {
                        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) up = true;
                        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) down = true;
                        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) left = true;
                        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) right = true;
                    }
                }
                catch { }

                // Fallback for Legacy Input with try-catch to prevent crash
                try
                {
                    if (!up && (UnityEngine.Input.GetKeyDown(KeyCode.W) || UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))) up = true;
                    if (!down && (UnityEngine.Input.GetKeyDown(KeyCode.S) || UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))) down = true;
                    if (!left && (UnityEngine.Input.GetKeyDown(KeyCode.A) || UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))) left = true;
                    if (!right && (UnityEngine.Input.GetKeyDown(KeyCode.D) || UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))) right = true;
                }
                catch { }

                if (up && _snakeDir != Vector2Int.down) _snakeDir = Vector2Int.up;
                else if (down && _snakeDir != Vector2Int.up) _snakeDir = Vector2Int.down;
                else if (left && _snakeDir != Vector2Int.right) _snakeDir = Vector2Int.left;
                else if (right && _snakeDir != Vector2Int.left) _snakeDir = Vector2Int.right;
            }
        }

        // =========================================================================
        // PUBLIC API & NAVIGATION
        // =========================================================================

        public void OpenBrowser()
        {
            gameObject.SetActive(true);
            if (LeftDashboardUI.Instance != null)
            {
                LeftDashboardUI.Instance.SetMode(LeftDashboardUI.LeftScreenMode.Browser);
            }
            TriggerLoadingSweep();
            AudioManager.Instance?.PlayConnectChime();
        }

        public void SwitchTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= 7) return;
            BrowserTab tab = (BrowserTab)tabIndex;
            string url = GetTabUrl(tab);
            NavigateTo(tab, url);
        }

        private string GetTabUrl(BrowserTab tab)
        {
            switch (tab)
            {
                case BrowserTab.QuantumSearch: return "https://quantum-vr.net/search";
                case BrowserTab.HackerFeed: return "https://quantum-vr.net/news/hackerfeed";
                case BrowserTab.NexusAI: return "https://quantum-vr.net/ai/nexus-copilot";
                case BrowserTab.CCTVSecurity: return "https://quantum-vr.net/security/cctv-stream";
                case BrowserTab.CloudCDN: return "https://quantum-vr.net/network/cloud-telemetry";
                case BrowserTab.CyberArcade: return "https://quantum-vr.net/arcade/neon-snake";
                case BrowserTab.Themes: return "https://quantum-vr.net/settings/themes";
                default: return "https://quantum-vr.net";
            }
        }

        public void NavigateTo(BrowserTab tab, string url)
        {
            _currentTab = tab;
            if (_urlInput != null) _urlInput.text = url;

            // Update History
            if (_historyIndex == -1 || _history[_historyIndex] != url)
            {
                // Truncate future history if navigated from past
                if (_historyIndex >= 0 && _historyIndex < _history.Count - 1)
                {
                    _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
                }
                _history.Add(url);
                _historyIndex = _history.Count - 1;
            }

            UpdateNavButtons();
            UpdateTabHeaders();
            ShowPage((int)tab);
            TriggerLoadingSweep();
            AudioManager.Instance?.PlayClick();

            if (tab == BrowserTab.HackerFeed)
            {
                FetchLiveHackerFeed();
            }

            if (tab == BrowserTab.CyberArcade)
            {
                ResetSnakeGame();
            }
        }

        private void ShowPage(int index)
        {
            for (int i = 0; i < _pages.Length; i++)
            {
                if (_pages[i] != null) _pages[i].SetActive(i == index);
            }
        }

        private void UpdateTabHeaders()
        {
            for (int i = 0; i < 7; i++)
            {
                bool active = (i == (int)_currentTab);
                if (_tabBgs[i] != null)
                {
                    _tabBgs[i].color = active ? new Color(0.08f, 0.14f, 0.24f, 0.98f) : new Color(0.04f, 0.06f, 0.10f, 0.8f);
                }
                if (_tabTexts[i] != null)
                {
                    _tabTexts[i].color = active ? _primaryAccent : new Color(0.65f, 0.75f, 0.85f);
                    _tabTexts[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }

        private void UpdateNavButtons()
        {
            if (_backBtn != null) _backBtn.interactable = (_historyIndex > 0);
            if (_forwardBtn != null) _forwardBtn.interactable = (_historyIndex < _history.Count - 1);
        }

        public void GoBack()
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                string targetUrl = _history[_historyIndex];
                BrowserTab tab = ParseUrlToTab(targetUrl);
                _currentTab = tab;
                if (_urlInput != null) _urlInput.text = targetUrl;
                UpdateNavButtons();
                UpdateTabHeaders();
                ShowPage((int)tab);
                TriggerLoadingSweep();
                AudioManager.Instance?.PlayClick();
            }
        }

        public void GoForward()
        {
            if (_historyIndex < _history.Count - 1)
            {
                _historyIndex++;
                string targetUrl = _history[_historyIndex];
                BrowserTab tab = ParseUrlToTab(targetUrl);
                _currentTab = tab;
                if (_urlInput != null) _urlInput.text = targetUrl;
                UpdateNavButtons();
                UpdateTabHeaders();
                ShowPage((int)tab);
                TriggerLoadingSweep();
                AudioManager.Instance?.PlayClick();
            }
        }

        public void RefreshPage()
        {
            TriggerLoadingSweep();
            AudioManager.Instance?.PlayConnectChime();
            if (_currentTab == BrowserTab.CyberArcade)
            {
                ResetSnakeGame();
            }
        }

        private BrowserTab ParseUrlToTab(string url)
        {
            string lower = url.ToLowerInvariant();
            if (lower.Contains("news") || lower.Contains("hacker")) return BrowserTab.HackerFeed;
            if (lower.Contains("ai") || lower.Contains("nexus")) return BrowserTab.NexusAI;
            if (lower.Contains("cctv") || lower.Contains("cam") || lower.Contains("security")) return BrowserTab.CCTVSecurity;
            if (lower.Contains("network") || lower.Contains("cdn") || lower.Contains("telemetry")) return BrowserTab.CloudCDN;
            if (lower.Contains("arcade") || lower.Contains("game") || lower.Contains("snake")) return BrowserTab.CyberArcade;
            if (lower.Contains("theme") || lower.Contains("setting")) return BrowserTab.Themes;
            return BrowserTab.QuantumSearch;
        }

        private void OnGoClicked()
        {
            if (_urlInput == null) return;
            string input = _urlInput.text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Smart search or destination match
                BrowserTab target = ParseUrlToTab(input);
                if (target == BrowserTab.QuantumSearch && !input.Contains("search"))
                {
                    // Treat as query
                    NavigateTo(BrowserTab.QuantumSearch, $"https://quantum-vr.net/search?q={Uri.EscapeDataString(input)}");
                    ExecuteSearchQuery(input);
                    return;
                }
                NavigateTo(target, GetTabUrl(target));
            }
            else
            {
                BrowserTab target = ParseUrlToTab(input);
                NavigateTo(target, input);
            }
        }

        private void TriggerLoadingSweep()
        {
            if (_loadingProgressBar == null) return;
            StopCoroutine(nameof(AnimateLoadingSweep));
            StartCoroutine(nameof(AnimateLoadingSweep));
        }

        private IEnumerator AnimateLoadingSweep()
        {
            if (_loadingProgressBar == null) yield break;
            _loadingProgressBar.gameObject.SetActive(true);
            _loadingProgressBar.fillAmount = 0.1f;
            float elapsed = 0f;
            float dur = 0.35f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                _loadingProgressBar.fillAmount = Mathf.Lerp(0.1f, 0.95f, elapsed / dur);
                yield return null;
            }

            _loadingProgressBar.fillAmount = 1f;
            yield return new WaitForSeconds(0.08f);
            _loadingProgressBar.gameObject.SetActive(false);
        }

        // =========================================================================
        // UI CONSTRUCTION (1920x1080 DISPLAY RESOLUTION)
        // =========================================================================

        public void BuildBrowserUI()
        {
            // Clear existing children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Base Root container (1920 x 1080)
            GameObject rootObj = CreateUIObject("QuantumBrowserRoot", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _bgDark);
            _browserRoot = rootObj.GetComponent<RectTransform>();

            // 1. Tab Bar (Height: 46px at top: y = 1034 to 1080)
            BuildTopTabBar(_browserRoot);

            // 2. Navigation & Address Bar (Height: 64px: y = 970 to 1034)
            BuildAddressAndNavBar(_browserRoot);

            // 3. Bookmarks Quick Bar (Height: 36px: y = 934 to 970)
            BuildBookmarksBar(_browserRoot);

            // 4. Main Web Viewport (y = 0 to 934)
            GameObject vpObj = CreateUIObject("Viewport", _browserRoot, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              new Vector2(0f, 0f), new Vector2(0f, -146f), new Color(0.02f, 0.035f, 0.06f, 1f));
            _viewportContainer = vpObj.GetComponent<RectTransform>();

            // Build individual web pages
            _pages[0] = BuildQuantumSearchPage(_viewportContainer);
            _pages[1] = BuildHackerFeedPage(_viewportContainer);
            _pages[2] = BuildNexusAIPage(_viewportContainer);
            _pages[3] = BuildCCTVPage(_viewportContainer);
            _pages[4] = BuildCloudCDNPage(_viewportContainer);
            _pages[5] = BuildCyberArcadePage(_viewportContainer);
            _pages[6] = BuildThemesPage(_viewportContainer);

            ShowPage(0);
            UpdateTabHeaders();
        }

        // --- 1. TOP TAB BAR ---
        private void BuildTopTabBar(RectTransform parent)
        {
            GameObject tabBar = CreateUIObject("TabBar", parent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                               new Vector2(0f, -46f), new Vector2(0f, 0f), new Color(0.03f, 0.05f, 0.09f, 0.98f));
            _tabBarContainer = tabBar.GetComponent<RectTransform>();

            // Top Cyan Highlight line
            CreateUIObject("TopAccent", tabBar.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                           new Vector2(0f, -2f), new Vector2(0f, 0f), _primaryAccent);

            string[] tabLabels = new string[]
            {
                "[WEB] Quantum Search",
                "[NEWS] HackerFeed News",
                "[AI] Nexus AI Assistant",
                "[CAM] CCTV Surveillance",
                "[STATS] Cloud CDN Status",
                "[GAME] Cyber Arcade",
                "[THEME] Neon Themes"
            };

            float tabWidth = 210f;
            float startX = 14f;

            for (int i = 0; i < 7; i++)
            {
                int index = i;
                GameObject tObj = CreateUIObject($"Tab_{i}", tabBar.transform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                                                 new Vector2(startX + i * (tabWidth + 4f), 2f),
                                                 new Vector2(startX + (i + 1) * tabWidth + i * 4f, -2f),
                                                 new Color(0.04f, 0.06f, 0.10f, 0.8f));

                _tabBgs[i] = tObj.GetComponent<Image>();
                Button btn = tObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SwitchTab(index));
                _tabButtons[i] = btn;

                // Tab text
                GameObject lbl = CreateTextObject("Label", tObj.transform, Vector2.zero, Vector2.one,
                                                  new Vector2(10f, 0f), new Vector2(-10f, 0f),
                                                  tabLabels[i], 16, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);
                _tabTexts[i] = lbl.GetComponent<TextMeshProUGUI>();
            }

            // Right side Monitor Switcher: [[STATS] HARDWARE TELEMETRY]
            Button switchBtn = CreateButton("SwitchToTelemetryBtn", tabBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                            new Vector2(-280f, -17f), new Vector2(-16f, 17f),
                                            "[STATS] SWITCH TO HARDWARE TELEMETRY", new Color(0.12f, 0.28f, 0.44f, 0.95f),
                                            () => {
                                                if (LeftDashboardUI.Instance != null)
                                                    LeftDashboardUI.Instance.ShowTelemetry();
                                            });
        }

        // --- 2. ADDRESS & NAVIGATION BAR ---
        private void BuildAddressAndNavBar(RectTransform parent)
        {
            GameObject navBar = CreateUIObject("NavBar", parent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                               new Vector2(0f, -110f), new Vector2(0f, -46f), new Color(0.04f, 0.07f, 0.12f, 0.98f));

            // Back Button
            _backBtn = CreateNavIconButton("BackBtn", navBar.transform, new Vector2(14f, 12f), new Vector2(54f, 52f), "<", GoBack);

            // Forward Button
            _forwardBtn = CreateNavIconButton("ForwardBtn", navBar.transform, new Vector2(62f, 12f), new Vector2(102f, 52f), ">", GoForward);

            // Refresh Button
            _refreshBtn = CreateNavIconButton("RefreshBtn", navBar.transform, new Vector2(110f, 12f), new Vector2(150f, 52f), "[SYNC]", RefreshPage);

            // Home Button
            _homeBtn = CreateNavIconButton("HomeBtn", navBar.transform, new Vector2(158f, 12f), new Vector2(198f, 52f), "[HOME]", () => NavigateTo(BrowserTab.QuantumSearch, "https://quantum-vr.net/search"));

            // SSL Lock Indicator
            GameObject lockObj = CreateUIObject("SSLLock", navBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                                new Vector2(212f, -18f), new Vector2(248f, 18f), new Color(0.06f, 0.22f, 0.16f, 0.9f));
            CreateTextObject("LockIcon", lockObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                             "[SECURE]", 16, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0f, 1f, 0.6f));

            // Address Input Field
            GameObject urlBox = CreateUIObject("URLFieldBox", navBar.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                                               new Vector2(256f, -19f), new Vector2(-140f, 19f), new Color(0.02f, 0.035f, 0.06f, 0.95f));

            // URL Box Border
            GameObject urlBorder = CreateUIObject("Border", urlBox.transform, Vector2.zero, Vector2.one,
                                                 new Vector2(-1f, -1f), new Vector2(1f, 1f), new Color(0f, 1f, 1f, 0.35f));
            urlBorder.transform.SetAsFirstSibling();

            GameObject utObj = CreateTextObject("Text", urlBox.transform, Vector2.zero, Vector2.one,
                                                new Vector2(16f, 0f), new Vector2(-16f, 0f),
                                                "https://quantum-vr.net/search", 20, FontStyles.Normal,
                                                TextAlignmentOptions.MidlineLeft, new Color(0.2f, 0.9f, 1f));

            _urlInput = urlBox.AddComponent<TMP_InputField>();
            _urlInput.textComponent = utObj.GetComponent<TextMeshProUGUI>();
            _urlInput.text = "https://quantum-vr.net/search";
            _urlInput.onSubmit.AddListener((s) => OnGoClicked());

            // Go Button
            _goBtn = CreateButton("GoBtn", navBar.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                  new Vector2(-124f, -18f), new Vector2(-14f, 18f), "-> GO",
                                  new Color(0.0f, 0.6f, 0.7f, 1f), OnGoClicked, 18f);

            // Loading Progress Bar across bottom
            GameObject pbObj = CreateUIObject("LoadingProgressBar", navBar.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                                              new Vector2(0f, 0f), new Vector2(0f, 3f), _primaryAccent);
            _loadingProgressBar = pbObj.GetComponent<Image>();
            _loadingProgressBar.type = Image.Type.Filled;
            _loadingProgressBar.fillMethod = Image.FillMethod.Horizontal;
            _loadingProgressBar.fillAmount = 1f;
            _loadingProgressBar.gameObject.SetActive(false);
        }

        // --- 3. BOOKMARKS BAR ---
        private void BuildBookmarksBar(RectTransform parent)
        {
            GameObject bmBar = CreateUIObject("BookmarksBar", parent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                              new Vector2(0f, -146f), new Vector2(0f, -110f), new Color(0.035f, 0.055f, 0.09f, 0.95f));

            var bookmarks = new (string title, BrowserTab tab)[]
            {
                ("* Quantum Search", BrowserTab.QuantumSearch),
                ("* HackerFeed News", BrowserTab.HackerFeed),
                ("* Nexus AI Chat", BrowserTab.NexusAI),
                ("* CCTV Security", BrowserTab.CCTVSecurity),
                ("* Cloud CDN Status", BrowserTab.CloudCDN),
                ("* Cyber Arcade (Snake)", BrowserTab.CyberArcade),
                ("* Neon Themes", BrowserTab.Themes)
            };

            float x = 14f;
            for (int i = 0; i < bookmarks.Length; i++)
            {
                int index = i;
                float w = 175f;
                CreateButton($"BM_{i}", bmBar.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                             new Vector2(x, -14f), new Vector2(x + w, 14f),
                             bookmarks[i].title, new Color(0.06f, 0.10f, 0.16f, 0.9f),
                             () => SwitchTab((int)bookmarks[index].tab), 14);
                x += w + 8f;
            }
        }

        // =========================================================================
        // WEBPAGES IMPLEMENTATION
        // =========================================================================

        // --- PAGE 0: QUANTUM SEARCH ---
        private GameObject BuildQuantumSearchPage(RectTransform parent)
        {
            GameObject page = CreateUIObject("Page_Search", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _bgDark);

            // Large Quantum Search Holographic Title
            CreateTextObject("MainLogo", page.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(-450f, -170f), new Vector2(450f, -60f),
                             "<size=54><color=#00ffff><b>QUANTUM</b></color> <color=#ffffff>SEARCH</color></size>\n<size=18><color=#88bbdd>Next-Gen Spatial Web Index | Ultra-Low Latency Navigation</color></size>",
                             54, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            // Search Bar Container
            GameObject sBox = CreateUIObject("SearchBox", page.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                             new Vector2(-440f, -240f), new Vector2(300f, -180f), new Color(0.04f, 0.08f, 0.14f, 0.98f));
            CreateUIObject("BoxBorder", sBox.transform, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f), _primaryAccent).transform.SetAsFirstSibling();

            GameObject stObj = CreateTextObject("Text", sBox.transform, Vector2.zero, Vector2.one,
                                                new Vector2(20f, 0f), new Vector2(-20f, 0f),
                                                "Search quantum web, docs, or enter query...", 22, FontStyles.Normal,
                                                TextAlignmentOptions.MidlineLeft, new Color(0.5f, 0.8f, 1f));

            _searchInput = sBox.AddComponent<TMP_InputField>();
            _searchInput.textComponent = stObj.GetComponent<TextMeshProUGUI>();
            _searchInput.onSubmit.AddListener(ExecuteSearchQuery);

            // Search Action Button
            CreateButton("SearchBtn", page.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                         new Vector2(310f, -240f), new Vector2(440f, -180f),
                         "[ZOOM] SEARCH", new Color(0f, 0.7f, 0.8f, 1f),
                         () => ExecuteSearchQuery(_searchInput != null ? _searchInput.text : ""));

            // Trending Tags
            float tagX = -440f;
            string[] tags = new string[] { "#WebRTC10Gbps", "#OrbitalRelay", "#NexusAI", "#CCTVSecurity", "#TheaterCinema", "#RetroArcade" };
            for (int i = 0; i < tags.Length; i++)
            {
                string tag = tags[i];
                float tagW = 140f;
                CreateButton($"Tag_{i}", page.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                             new Vector2(tagX, -280f), new Vector2(tagX + tagW, -252f),
                             tag, new Color(0.06f, 0.12f, 0.20f, 0.85f),
                             () => {
                                 if (_searchInput != null) _searchInput.text = tag;
                                 ExecuteSearchQuery(tag);
                             }, 13);
                tagX += tagW + 10f;
            }

            // Results Container
            _searchResultsBox = CreateUIObject("SearchResults", page.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                                               new Vector2(-540f, 40f), new Vector2(540f, -300f), _cardDark);
            CreateUIObject("ResBorder", _searchResultsBox.transform, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f), new Color(0f, 1f, 1f, 0.2f)).transform.SetAsFirstSibling();

            GameObject resText = CreateTextObject("ResultsText", _searchResultsBox.transform, Vector2.zero, Vector2.one,
                                                  new Vector2(24f, 20f), new Vector2(-24f, -20f),
                                                  GetDefaultSearchFeed(), 20, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);
            _searchResultsText = resText.GetComponent<TextMeshProUGUI>();

            return page;
        }

        private string GetDefaultSearchFeed()
        {
            return "<color=#00ffff><b>* FEATURED DESTINATIONS & POPULAR ARTICLES</b></color>\n\n" +
                   "<size=22><color=#ffffff><b>1. HackerFeed: Orbital Quantum Entanglement Test Achieves 100 Tbps</b></color></size>\n" +
                   "<color=#00ff88>https://quantum-vr.net/news/orbital-link</color> -- Experimental low-orbit satellite cluster demonstrates zero packet-loss optical routing across 4,200 km.\n\n" +
                   "<size=22><color=#ffffff><b>2. Nexus AI: Quantum Workstation Intelligence Companion v4.5</b></color></size>\n" +
                   "<color=#00ff88>https://quantum-vr.net/ai/nexus-copilot</color> -- Interactive natural language system diagnostics, hardware monitoring, and smart office room management.\n\n" +
                   "<size=22><color=#ffffff><b>3. Live CCTV Surveillance: Perimeter & Theater Projection Feeds</b></color></size>\n" +
                   "<color=#00ff88>https://quantum-vr.net/security/cctv-stream</color> -- Real-time security stream connecting workstation cameras, nature lounge, and IMAX 70mm auditorium.\n\n" +
                   "<size=22><color=#ffffff><b>4. Retro Neon Snake Arcade: WebGL 60 FPS VR Challenge</b></color></size>\n" +
                   "<color=#00ff88>https://quantum-vr.net/arcade/neon-snake</color> -- Playable responsive neon snake mini-game with global leaderboard integration.";
        }

        private void ExecuteSearchQuery(string query)
        {
            if (string.IsNullOrEmpty(query)) query = "trending";
            TriggerLoadingSweep();
            AudioManager.Instance?.PlayConnectChime();

            if (_searchResultsText != null)
            {
                _searchResultsText.text = $"<color=#00ffff><b>SEARCHING LIVE WEB: \"{query}\"...</b></color>\n<size=18><color=#88bbdd>Querying Wikimedia REST API & live tech relay...</color></size>";
            }

            string cleanQuery = query.Trim();
            LiveWebClient.EnsureInstance().FetchWikipedia(cleanQuery, (summary) =>
            {
                if (_searchResultsText == null) return;
                if (summary != null && !string.IsNullOrEmpty(summary.Extract))
                {
                    string desc = string.IsNullOrEmpty(summary.Description) ? "Live Encyclopedia Article" : summary.Description;
                    _searchResultsText.text = $"<color=#00ffff><b>LIVE ENCYCLOPEDIA RESULT: \"{summary.Title}\"</b></color>\n\n" +
                                              $"<size=24><color=#ffffff><b>{summary.Title}</b></color></size>\n" +
                                              $"<color=#00ff88>{summary.PageUrl}</color>  --  <color=#ffaa00><i>{desc}</i></color>\n\n" +
                                              $"<color=#e6f2ff>{summary.Extract}</color>\n\n" +
                                              $"<size=16><color=#88bbdd>[LIVE DATA: Wikipedia REST v1 API | HTTPS Encrypted | Zero Cache Lag]</color></size>";
                }
                else
                {
                    _searchResultsText.text = $"<color=#00ffff><b>SEARCH RESULTS FOR: \"{cleanQuery}\"</b></color> <color=#888888>(Local Cache Index)</color>\n\n" +
                                              $"<size=22><color=#ffffff><b>1. Top Match: {cleanQuery.ToUpper()} Documentation & Specifications</b></color></size>\n" +
                                              $"<color=#00ff88>https://quantum-vr.net/docs/{Uri.EscapeDataString(cleanQuery.ToLower())}</color> -- Verified technical protocol reference and architectural guides.\n\n" +
                                              $"<size=22><color=#ffffff><b>2. Global Network Nodes for {cleanQuery}</b></color></size>\n" +
                                              $"<color=#00ff88>https://quantum-vr.net/network/{Uri.EscapeDataString(cleanQuery.ToLower())}</color> -- 100% uptime across 12 distributed edge data centers with sub-10ms latency.\n\n" +
                                              $"<size=22><color=#ffffff><b>3. Community Discussion: Advancements in {cleanQuery}</b></color></size>\n" +
                                              $"<color=#00ff88>https://quantum-vr.net/news/forum/{Uri.EscapeDataString(cleanQuery.ToLower())}</color> -- High-voter threads, benchmarks, and performance analysis.";
                }
            });
        }

        // --- PAGE 1: HACKERFEED (NEWS) ---
        private GameObject BuildHackerFeedPage(RectTransform parent)
        {
            GameObject page = CreateUIObject("Page_HackerFeed", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _bgDark);

            // Banner
            CreateTextObject("NewsHeader", page.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(40f, -60f), new Vector2(-40f, -10f),
                             "<color=#00ffff><b>HACKERFEED // DAILY CYBERNETICS & VR TECH DISPATCH</b></color>", 28, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            var articles = new (string title, string source, string desc)[]
            {
                ("Quantum Entanglement Optical Relay Reaches 100 Terabits/sec in Orbital Test", "orbital-relay.net | 14 min ago", "Researchers deployed an experimental constellation of 6 micro-satellites achieving unprecedented photonic multiplexing with near-zero latency degradation."),
                ("Neural Interface Standard 4.2 Approved for Commercial Spatial Computing Desktops", "neural-consortium.org | 42 min ago", "The ISO working group standardized haptic impulse decoding, paving the way for seamless multi-monitor gaze and neural intention cursor tracking."),
                ("Global WebRTC Mesh Optimization Slashes Average Workstation Jitter Below 1.2ms", "webrtc-labs.io | 2 hours ago", "A newly rolled out adaptive forward error correction algorithm dynamically re-routes dropped audio/video packets across secondary fiber hops."),
                ("Deep Space Array Detects Repeating High-Frequency Harmonic Pulse from Proxima Centauri", "astro-telemetry.org | 5 hours ago", "The Atacama Submillimeter Array picked up artificial-like phase-modulated cycles on the 1420 MHz hydrogen resonance band.")
            };

            float y = -80f;
            float cardHeight = 160f;

            for (int i = 0; i < articles.Length; i++)
            {
                int idx = i;
                GameObject card = CreateUIObject($"Article_{i}", page.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                 new Vector2(40f, y - cardHeight), new Vector2(-40f, y), _cardDark);
                CreateUIObject("CardAccent", card.transform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                               new Vector2(0f, 0f), new Vector2(4f, 0f), _primaryAccent);

                // Upvote button
                Button upvoteBtn = CreateButton($"Upvote_{i}", card.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                                new Vector2(20f, -32f), new Vector2(100f, 32f),
                                                $"^ {_articleUpvotes[i]}", new Color(0.08f, 0.18f, 0.28f, 0.95f),
                                                () => {
                                                    _articleUpvotes[idx]++;
                                                    if (_upvoteTexts[idx] != null) _upvoteTexts[idx].text = $"^ {_articleUpvotes[idx]}";
                                                    AudioManager.Instance?.PlayConnectChime();
                                                }, 18);
                _upvoteTexts[i] = upvoteBtn.GetComponentInChildren<TextMeshProUGUI>();

                // Article Content
                GameObject hlObj = CreateTextObject("Headline", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                 new Vector2(120f, -44f), new Vector2(-20f, -10f),
                                 $"<b>{articles[i].title}</b>", 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);
                _articleHeadlineTexts[i] = hlObj.GetComponent<TextMeshProUGUI>();

                GameObject metaObj = CreateTextObject("Meta", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                 new Vector2(120f, -74f), new Vector2(-20f, -44f),
                                 $"<color=#00ff88>{articles[i].source}</color>", 16, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.green);
                _articleMetaTexts[i] = metaObj.GetComponent<TextMeshProUGUI>();

                GameObject descObj = CreateTextObject("Desc", card.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                 new Vector2(120f, 12f), new Vector2(-20f, -76f),
                                 $"<color=#bbccdd>{articles[i].desc}</color>", 17, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);
                _articleDescTexts[i] = descObj.GetComponent<TextMeshProUGUI>();

                y -= cardHeight + 20f;
            }

            return page;
        }

        public void FetchLiveHackerFeed()
        {
            var client = LiveWebClient.EnsureInstance();
            client.FetchHackerNewsTop(4, (liveArticles) =>
            {
                if (liveArticles == null || liveArticles.Count == 0) return;
                for (int i = 0; i < liveArticles.Count && i < 4; i++)
                {
                    if (_articleHeadlineTexts[i] != null)
                        _articleHeadlineTexts[i].text = $"<b>{liveArticles[i].Title}</b>";

                    if (_articleMetaTexts[i] != null)
                    {
                        string badge = liveArticles[i].IsLive ? "<color=#00ff88>[LIVE HN]</color>" : "<color=#ffaa00>[CACHE]</color>";
                        _articleMetaTexts[i].text = $"{badge} <color=#00ffcc>{liveArticles[i].SourceName}</color> | <color=#ffffff>{liveArticles[i].Upvotes} points</color>";
                    }

                    if (_articleDescTexts[i] != null)
                        _articleDescTexts[i].text = $"<color=#bbccdd>{liveArticles[i].Description}</color>";

                    _articleUpvotes[i] = liveArticles[i].Upvotes;
                    if (_upvoteTexts[i] != null)
                        _upvoteTexts[i].text = $"^ {_articleUpvotes[i]}";
                }
            });
        }

        // --- PAGE 2: NEXUS AI (CHAT & ASSISTANT) ---
        private GameObject BuildNexusAIPage(RectTransform parent)
        {
            GameObject page = CreateUIObject("Page_NexusAI", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _bgDark);

            // Header Banner
            CreateTextObject("AIHeader", page.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(40f, -50f), new Vector2(-40f, -10f),
                             "<color=#00ffff><b>NEXUS COGNITIVE COPILOT // WORKSTATION AI ASSISTANT</b></color>  |  <color=#00ff88>LLM v4.5 ONLINE</color>",
                             26, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            // Chat Log Scroll Area
            GameObject logBox = CreateUIObject("ChatLogBox", page.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                               new Vector2(40f, 140f), new Vector2(-40f, -60f), _cardDark);
            CreateUIObject("BoxBorder", logBox.transform, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f), new Color(0f, 1f, 1f, 0.25f)).transform.SetAsFirstSibling();

            GameObject logTextObj = CreateTextObject("LogText", logBox.transform, Vector2.zero, Vector2.one,
                                                     new Vector2(24f, 20f), new Vector2(-24f, -20f), "",
                                                     19, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);
            _aiChatLogText = logTextObj.GetComponent<TextMeshProUGUI>();

            // Initial AI Greeting
            _chatHistory.AppendLine("<color=#00ffff><b>[NEXUS AI]</b></color>: Welcome to Quantum OS Spatial Workstation. I am your cognitive ambient copilot. I have real-time telemetric access to your room lighting, multi-monitor configuration, WebRTC connection, and IMAX cinema projector. How can I assist you today?");
            _aiChatLogText.text = _chatHistory.ToString();

            // Quick Prompt Chips
            string[] quickPrompts = new string[]
            {
                "[LIGHTS] Room Lighting Status",
                "[CINEMA] IMAX Cinema Setup",
                "[CAM] Surveillance Cameras",
                "[PWR] Hardware Overclocking",
                "[WEB] WebRTC Network Stats"
            };

            float qx = 40f;
            for (int i = 0; i < quickPrompts.Length; i++)
            {
                string prompt = quickPrompts[i];
                float qw = 220f;
                CreateButton($"Prompt_{i}", page.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                             new Vector2(qx, 85f), new Vector2(qx + qw, 125f),
                             prompt, new Color(0.08f, 0.16f, 0.25f, 0.95f),
                             () => SendAIMessage(prompt), 14);
                qx += qw + 12f;
            }

            // Input Box
            GameObject chatInputBox = CreateUIObject("InputBox", page.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                                                     new Vector2(40f, 20f), new Vector2(-180f, 75f), new Color(0.03f, 0.05f, 0.08f, 0.98f));
            CreateUIObject("InBorder", chatInputBox.transform, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f), _primaryAccent).transform.SetAsFirstSibling();

            GameObject ciText = CreateTextObject("Text", chatInputBox.transform, Vector2.zero, Vector2.one,
                                                 new Vector2(16f, 0f), new Vector2(-16f, 0f),
                                                 "Ask Nexus AI a question or command...", 20, FontStyles.Normal,
                                                 TextAlignmentOptions.MidlineLeft, new Color(0.4f, 0.8f, 1f));

            _aiChatInput = chatInputBox.AddComponent<TMP_InputField>();
            _aiChatInput.textComponent = ciText.GetComponent<TextMeshProUGUI>();
            _aiChatInput.onSubmit.AddListener(SendAIMessage);

            // Send Button
            CreateButton("SendBtn", page.transform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                         new Vector2(-165f, 20f), new Vector2(-40f, 75f),
                         "SEND ->", new Color(0f, 0.7f, 0.8f, 1f),
                         () => SendAIMessage(_aiChatInput != null ? _aiChatInput.text : ""));

            return page;
        }

        private void SendAIMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;
            if (_aiChatInput != null) _aiChatInput.text = "";

            _chatHistory.AppendLine($"\n<color=#ffffff><b>[USER]</b></color>: {msg}");
            AudioManager.Instance?.PlayKeyboardTap();

            string reply = GenerateAIReply(msg);
            _chatHistory.AppendLine($"<color=#00ffff><b>[NEXUS AI]</b></color>: {reply}");

            if (_aiChatLogText != null)
            {
                _aiChatLogText.text = _chatHistory.ToString();
            }
            AudioManager.Instance?.PlayConnectChime();
        }

        private string GenerateAIReply(string query)
        {
            string lower = query.ToLowerInvariant();
            if (lower.Contains("light") || lower.Contains("room"))
                return "Ambient lighting is synchronized in Cyber Blue Neon theme with 10 ceiling spot fixtures active. You can toggle daylight/night mood from the Right Dashboard NOC panel.";
            if (lower.Contains("imax") || lower.Contains("theater") || lower.Contains("screen share"))
                return "The 70mm VIP Theater screen is online! Click the [[FEED] SHARE MIDDLE MONITOR TO IMAX] button on the Right Dashboard to project your live workstation and player webcam directly onto the massive screen.";
            if (lower.Contains("cctv") || lower.Contains("camera") || lower.Contains("webcam"))
                return "Surveillance systems report 4 active HD feeds: Workstation Wide, VIP Theater, Nature Lounge, and Entryway. AI facial recognition verifies admin authorization with zero alerts.";
            if (lower.Contains("overclock") || lower.Contains("hardware") || lower.Contains("cpu"))
                return "The host CPU is locked at 4.85 GHz with all 8 performance cores running under 54 degC. Turbo Overclock profile is ready on the Left Display Hardware Tuner.";
            if (lower.Contains("network") || lower.Contains("ping") || lower.Contains("webrtc"))
                return "WebRTC peer connection operates over 10 Gbps encrypted link. Measured round-trip latency to Virginia node is 12ms with 0.01% jitter.";
            return $"Understood. I have logged your query regarding \"{query}\" into the Quantum OS knowledge cache. All workstation sub-systems are operating at 100% nominal capacity.";
        }

        // --- PAGE 3: CCTV SURVEILLANCE & SECURITY HUB ---
        private GameObject BuildCCTVPage(RectTransform parent)
        {
            GameObject page = CreateUIObject("Page_CCTV", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _bgDark);

            // Left side: Camera Viewports / Selector
            GameObject camPanel = CreateUIObject("CamPanel", page.transform, new Vector2(0f, 0f), new Vector2(0.65f, 1f),
                                                 new Vector2(40f, 40f), new Vector2(-20f, -40f), _cardDark);
            CreateUIObject("CPBorder", camPanel.transform, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f), _primaryAccent).transform.SetAsFirstSibling();

            // Main simulated CCTV feed monitor
            GameObject feedDisplay = CreateUIObject("CCTVDisplay", camPanel.transform, new Vector2(0f, 0.25f), new Vector2(1f, 1f),
                                                    new Vector2(20f, 20f), new Vector2(-20f, -20f), new Color(0.015f, 0.025f, 0.04f, 1f));

            // Grid scanlines overlay effect
            CreateTextObject("CCTVScanlines", feedDisplay.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                             "==================================================================================\n" +
                             "REC | [4K UHD 60FPS]  |  NIGHT VISION: PASSIVE  |  AI OBJECT TRACKING: ON\n" +
                             "==================================================================================",
                             16, FontStyles.Bold, TextAlignmentOptions.TopLeft, new Color(0f, 1f, 0.5f, 0.8f));

            GameObject feedLbl = CreateTextObject("FeedLabel", feedDisplay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                                  new Vector2(-300f, -40f), new Vector2(300f, 40f),
                                                  "[CAM] CAMERA 01: WORKSTATION DESK & WEBCAM",
                                                  28, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            _cctvFeedLabel = feedLbl.GetComponent<TextMeshProUGUI>();

            // 4 Camera Selector Buttons
            string[] camNames = new string[] { "CAM 01: Workstation Desk", "CAM 02: IMAX VIP Theater", "CAM 03: Nature Lounge", "CAM 04: Cyber Entry" };
            float cx = 20f;
            for (int i = 0; i < 4; i++)
            {
                int cIdx = i;
                float cw = 260f;
                CreateButton($"CamBtn_{i}", camPanel.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                             new Vector2(cx, 20f), new Vector2(cx + cw, 75f),
                             camNames[i], new Color(0.08f, 0.16f, 0.24f, 0.95f),
                             () => SelectCCTVCamera(cIdx), 15);
                cx += cw + 14f;
            }

            // Right side: Security Telemetry & Log Stream
            GameObject logPanel = CreateUIObject("LogPanel", page.transform, new Vector2(0.65f, 0f), new Vector2(1f, 1f),
                                                 new Vector2(20f, 40f), new Vector2(-40f, -40f), _cardDark);
            CreateUIObject("LPBorder", logPanel.transform, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f), new Color(0f, 1f, 0.6f, 0.4f)).transform.SetAsFirstSibling();

            CreateTextObject("SecTitle", logPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(20f, -60f), new Vector2(-20f, -15f),
                             "<color=#00ff88><b>FACILITY SECURITY TELEMETRY</b></color>", 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            GameObject statusTextObj = CreateTextObject("SecStatus", logPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                        new Vector2(20f, 20f), new Vector2(-20f, -70f),
                                                        "SECURITY LEVEL: DEFCON 5 (NORMAL)\n" +
                                                        "INTRUSION DETECTORS: 8/8 CLEAR\n" +
                                                        "BIOMETRIC RECOGNITION: ADMIN VERIFIED\n" +
                                                        "WEBCAM MIRRORING: READY TO IMAX\n\n" +
                                                        "<color=#00ffff><b>SURVEILLANCE EVENT LOG:</b></color>\n" +
                                                        "[15:40:12] Motion detected at Gaming Chair.\n" +
                                                        "[15:40:14] Player seated -- Avatar sync locked.\n" +
                                                        "[15:41:02] UI Left Display activated.\n" +
                                                        "[15:42:30] Screen share link to IMAX ready.\n" +
                                                        "[15:45:10] AudioListener initialized 100% vol.",
                                                        17, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);
            _cctvStatusText = statusTextObj.GetComponent<TextMeshProUGUI>();

            return page;
        }

        private void SelectCCTVCamera(int index)
        {
            _selectedCameraIndex = index;
            string[] names = new string[]
            {
                "[CAM] CAMERA 01: WORKSTATION DESK & WEBCAM",
                "[CINEMA] CAMERA 02: IMAX VIP THEATER 70MM",
                "[LOUNGE] CAMERA 03: NATURE PANORAMA LOUNGE",
                "[ENTRY] CAMERA 04: CYBER ENTRYWAY & CORRIDOR"
            };

            if (_cctvFeedLabel != null)
            {
                _cctvFeedLabel.text = names[index];
            }
            TriggerLoadingSweep();
            AudioManager.Instance?.PlayConnectChime();
        }

        // --- PAGE 4: CLOUD CDN & NETWORK STATUS ---
        private GameObject BuildCloudCDNPage(RectTransform parent)
        {
            GameObject page = CreateUIObject("Page_CloudCDN", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _bgDark);

            CreateTextObject("CDNTitle", page.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(40f, -60f), new Vector2(-40f, -10f),
                             "<color=#00ffff><b>GLOBAL CLOUD CDN & WEBRTC RELAY DIAGNOSTICS</b></color>", 28, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            var nodes = new (string city, string ping, string load, string status)[]
            {
                ("US-East (Virginia)", "12 ms", "34% Load", "ONLINE | OPTIMAL"),
                ("EU-Central (Frankfurt)", "24 ms", "48% Load", "ONLINE | OPTIMAL"),
                ("AP-East (Tokyo)", "48 ms", "62% Load", "ONLINE | OPTIMAL"),
                ("OC-South (Sydney)", "94 ms", "28% Load", "ONLINE | STABLE")
            };

            float x = 40f;
            float nodeW = 430f;

            for (int i = 0; i < nodes.Length; i++)
            {
                GameObject nodeCard = CreateUIObject($"Node_{i}", page.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                                     new Vector2(x, -280f), new Vector2(x + nodeW, -80f), _cardDark);
                CreateUIObject("TopAccent", nodeCard.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                               new Vector2(0f, -4f), new Vector2(0f, 0f), _primaryAccent);

                CreateTextObject("City", nodeCard.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                 new Vector2(20f, -45f), new Vector2(-20f, -10f),
                                 $"<b>{nodes[i].city}</b>", 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

                CreateTextObject("Ping", nodeCard.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                                 new Vector2(20f, -20f), new Vector2(-20f, 20f),
                                 $"<size=34><color=#00ff88><b>{nodes[i].ping}</b></color></size>", 34, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.green);

                CreateTextObject("Details", nodeCard.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                                 new Vector2(20f, 15f), new Vector2(-20f, 50f),
                                 $"{nodes[i].load}  |  <color=#00ffff>{nodes[i].status}</color>", 16, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.cyan);

                x += nodeW + 20f;
            }

            // Bottom Diagnostics & Interactive Ping Test
            GameObject diagBox = CreateUIObject("DiagBox", page.transform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                new Vector2(40f, 40f), new Vector2(-40f, -310f), _cardDark);
            CreateUIObject("DBBorder", diagBox.transform, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f), new Color(0f, 1f, 1f, 0.3f)).transform.SetAsFirstSibling();

            GameObject pingSumObj = CreateTextObject("PingSummary", diagBox.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                     new Vector2(30f, -140f), new Vector2(-30f, -20f),
                                                     "<color=#00ffff><b>LIVE METRICS:</b></color> Jitter: 0.8ms  |  Packet Loss: 0.00%  |  Bandwidth: 10 Gbps Symmetrical\n" +
                                                     "TLS Protocol: TLS 1.3  |  Cipher: AES-256-GCM  |  Spatial WebRTC Session: CONNECTED",
                                                     22, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, Color.white);
            _pingSummaryText = pingSumObj.GetComponent<TextMeshProUGUI>();

            // Ping Test Button
            CreateButton("RunPingBtn", diagBox.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                         new Vector2(30f, 30f), new Vector2(340f, 90f),
                         "[PWR] RUN LIVE NETWORK BENCHMARK", new Color(0.1f, 0.6f, 0.7f, 1f),
                         RunNetworkPingTest, 18);

            return page;
        }

        private void RunNetworkPingTest()
        {
            if (_isPingTesting) return;
            StartCoroutine(AnimatePingTest());
        }

        private IEnumerator AnimatePingTest()
        {
            _isPingTesting = true;
            TriggerLoadingSweep();
            AudioManager.Instance?.PlayConnectChime();

            string[] testPhases = new string[]
            {
                "Measuring ICMP RTT to Edge Node...",
                "Testing WebRTC UDP socket throughput...",
                "Verifying frame buffer jitter...",
                "Calculating QoS Packet Health score..."
            };

            for (int i = 0; i < testPhases.Length; i++)
            {
                if (_pingSummaryText != null)
                {
                    _pingSummaryText.text = $"<color=#00ffff><b>BENCHMARK RUNNING:</b></color> {testPhases[i]}\n" +
                                            $"Current Jitter: {UnityEngine.Random.Range(0.6f, 1.1f):F2}ms  |  Latency: {UnityEngine.Random.Range(10, 14)}ms";
                }
                yield return new WaitForSeconds(0.4f);
            }

            if (_pingSummaryText != null)
            {
                _pingSummaryText.text = "<color=#00ff88><b>[OK] BENCHMARK COMPLETED: 100% HEALTH SCORE</b></color>\n" +
                                        "Average Latency: 11.4ms  |  Jitter: 0.72ms  |  Packet Loss: 0.000% (Lossless Link)\n" +
                                        "Spatial Video Pipeline: 4K 60FPS Uncompressed Stream Certified.";
            }

            AudioManager.Instance?.PlayConnectChime();
            _isPingTesting = false;
        }

        // --- PAGE 5: CYBER ARCADE (NEON SNAKE MINI-GAME) ---
        private GameObject BuildCyberArcadePage(RectTransform parent)
        {
            GameObject page = CreateUIObject("Page_Arcade", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _bgDark);

            _gridCells = new Image[GRID_SIZE, GRID_SIZE];

            // Left Side: 16x16 Game Board
            GameObject board = CreateUIObject("SnakeBoard", page.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                              new Vector2(-380f, -380f), new Vector2(380f, 380f), new Color(0.015f, 0.03f, 0.05f, 1f));
            CreateUIObject("BoardBorder", board.transform, Vector2.zero, Vector2.one, new Vector2(-2f, -2f), new Vector2(2f, 2f), _primaryAccent).transform.SetAsFirstSibling();

            float cellSize = 760f / GRID_SIZE;
            for (int r = 0; r < GRID_SIZE; r++)
            {
                for (int c = 0; c < GRID_SIZE; c++)
                {
                    GameObject cell = CreateUIObject($"Cell_{c}_{r}", board.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                                                     new Vector2(c * cellSize + 1f, r * cellSize + 1f),
                                                     new Vector2((c + 1) * cellSize - 1f, (r + 1) * cellSize - 1f),
                                                     new Color(0.03f, 0.06f, 0.10f, 0.4f));
                    _gridCells[c, r] = cell.GetComponent<Image>();
                }
            }

            // Right Side: Score, Status & Controls
            GameObject sidePanel = CreateUIObject("ArcadeSidePanel", page.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                                  new Vector2(-460f, -380f), new Vector2(-40f, 380f), _cardDark);
            CreateUIObject("SPBorder", sidePanel.transform, Vector2.zero, Vector2.one, new Vector2(-1f, -1f), new Vector2(1f, 1f), new Color(0f, 1f, 1f, 0.4f)).transform.SetAsFirstSibling();

            CreateTextObject("GameTitle", sidePanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(20f, -60f), new Vector2(-20f, -15f),
                             "<color=#00ffff><b>NEON SNAKE // RETRO ARCADE</b></color>", 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            GameObject scoreObj = CreateTextObject("ScoreText", sidePanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                   new Vector2(20f, -140f), new Vector2(-20f, -70f),
                                                   "<size=32><color=#00ff88><b>SCORE: 0</b></color></size>\nHIGH SCORE: 0", 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            _scoreText = scoreObj.GetComponent<TextMeshProUGUI>();

            GameObject statObj = CreateTextObject("StatusText", sidePanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                  new Vector2(20f, -200f), new Vector2(-20f, -150f),
                                                  "<color=#00ffff>PRESS START OR WASD TO PLAY</color>", 18, FontStyles.Normal, TextAlignmentOptions.Center, Color.cyan);
            _gameStatusText = statObj.GetComponent<TextMeshProUGUI>();

            // Start / Restart Button
            CreateButton("StartGameBtn", sidePanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                         new Vector2(-160f, -270f), new Vector2(160f, -215f),
                         "> START / RESTART", new Color(0f, 0.7f, 0.6f, 1f),
                         StartSnakeGame, 20);

            // On-Screen D-Pad for VR or mouse raycast clicking
            CreateButton("UpBtn", sidePanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                         new Vector2(-40f, 180f), new Vector2(40f, 250f), "^", new Color(0.1f, 0.2f, 0.35f, 0.95f),
                         () => { if (_snakeDir != Vector2Int.down) _snakeDir = Vector2Int.up; }, 26);

            CreateButton("LeftBtn", sidePanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                         new Vector2(-120f, 100f), new Vector2(-45f, 170f), "<", new Color(0.1f, 0.2f, 0.35f, 0.95f),
                         () => { if (_snakeDir != Vector2Int.right) _snakeDir = Vector2Int.left; }, 26);

            CreateButton("RightBtn", sidePanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                         new Vector2(45f, 100f), new Vector2(120f, 170f), ">", new Color(0.1f, 0.2f, 0.35f, 0.95f),
                         () => { if (_snakeDir != Vector2Int.left) _snakeDir = Vector2Int.right; }, 26);

            CreateButton("DownBtn", sidePanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                         new Vector2(-40f, 20f), new Vector2(40f, 90f), "v", new Color(0.1f, 0.2f, 0.35f, 0.95f),
                         () => { if (_snakeDir != Vector2Int.up) _snakeDir = Vector2Int.down; }, 26);

            return page;
        }

        private void ResetSnakeGame()
        {
            _gameRunning = false;
            if (_snakeCoroutine != null)
            {
                StopCoroutine(_snakeCoroutine);
                _snakeCoroutine = null;
            }

            if (_snakeBody == null) _snakeBody = new List<Vector2Int>();
            _snakeBody.Clear();
            _snakeBody.Add(new Vector2Int(5, 8));
            _snakeBody.Add(new Vector2Int(4, 8));
            _snakeBody.Add(new Vector2Int(3, 8));
            _snakeDir = Vector2Int.right;
            _foodPos = new Vector2Int(11, 8);
            _snakeScore = 0;

            UpdateScoreUI();
            if (_gameStatusText != null) _gameStatusText.text = "<color=#00ffff>READY - USE WASD OR D-PAD</color>";
            RedrawSnakeGrid();
        }

        private void StartSnakeGame()
        {
            ResetSnakeGame();
            _gameRunning = true;
            if (_gameStatusText != null) _gameStatusText.text = "<color=#00ff88>GAME IN PROGRESS</color>";
            if (gameObject.activeInHierarchy)
            {
                _snakeCoroutine = StartCoroutine(SnakeGameLoop());
            }
            AudioManager.Instance?.PlayConnectChime();
        }

        private IEnumerator SnakeGameLoop()
        {
            while (_gameRunning)
            {
                yield return new WaitForSeconds(0.14f);

                if (!_gameRunning || _snakeBody == null || _snakeBody.Count == 0) yield break;

                try
                {
                    Vector2Int head = _snakeBody[0] + _snakeDir;

                    // Wall Collision wrap or game over
                    if (head.x < 0 || head.x >= GRID_SIZE || head.y < 0 || head.y >= GRID_SIZE)
                    {
                        GameOver();
                        yield break;
                    }

                    // Self Collision
                    if (_snakeBody.Contains(head))
                    {
                        GameOver();
                        yield break;
                    }

                    _snakeBody.Insert(0, head);

                    // Check Food
                    if (head == _foodPos)
                    {
                        _snakeScore += 10;
                        if (_snakeScore > _snakeHighScore) _snakeHighScore = _snakeScore;
                        UpdateScoreUI();
                        AudioManager.Instance?.PlayConnectChime();
                        SpawnFood();
                    }
                    else
                    {
                        if (_snakeBody.Count > 0)
                        {
                            _snakeBody.RemoveAt(_snakeBody.Count - 1);
                        }
                    }

                    RedrawSnakeGrid();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[QuantumBrowser] Snake game handled error: {ex.Message}");
                    GameOver();
                    yield break;
                }
            }
        }

        private void SpawnFood()
        {
            int safety = 0;
            do
            {
                _foodPos = new Vector2Int(UnityEngine.Random.Range(0, GRID_SIZE), UnityEngine.Random.Range(0, GRID_SIZE));
                safety++;
            } while (_snakeBody != null && _snakeBody.Contains(_foodPos) && safety < 100);
        }

        private void GameOver()
        {
            _gameRunning = false;
            if (_gameStatusText != null) _gameStatusText.text = "<color=#ff4444><b>GAME OVER!</b></color>";
            AudioManager.Instance?.PlayDisconnect();
        }

        private void UpdateScoreUI()
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"<size=32><color=#00ff88><b>SCORE: {_snakeScore}</b></color></size>\nHIGH SCORE: {_snakeHighScore}";
            }
        }

        private void RedrawSnakeGrid()
        {
            if (_gridCells == null) return;

            Color emptyColor = new Color(0.03f, 0.06f, 0.10f, 0.4f);
            Color headColor = _primaryAccent;
            Color bodyColor = new Color(0f, 0.7f, 0.9f, 0.9f);
            Color foodColor = new Color(1f, 0.2f, 0.6f, 1f);

            for (int r = 0; r < GRID_SIZE; r++)
            {
                for (int c = 0; c < GRID_SIZE; c++)
                {
                    if (_gridCells[c, r] == null) continue;
                    Vector2Int pos = new Vector2Int(c, r);

                    if (pos == _foodPos)
                    {
                        _gridCells[c, r].color = foodColor;
                    }
                    else if (_snakeBody != null && _snakeBody.Count > 0 && pos == _snakeBody[0])
                    {
                        _gridCells[c, r].color = headColor;
                    }
                    else if (_snakeBody != null && _snakeBody.Contains(pos))
                    {
                        _gridCells[c, r].color = bodyColor;
                    }
                    else
                    {
                        _gridCells[c, r].color = emptyColor;
                    }
                }
            }
        }

        // --- PAGE 6: NEON THEMES ---
        private GameObject BuildThemesPage(RectTransform parent)
        {
            GameObject page = CreateUIObject("Page_Themes", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, _bgDark);

            CreateTextObject("ThemeHeader", page.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(40f, -60f), new Vector2(-40f, -10f),
                             "<color=#00ffff><b>QUANTUM BROWSER & WORKSTATION THEME CUSTOMIZER</b></color>", 28, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);

            var themes = new (string name, Color accent, string desc)[]
            {
                ("CYBER CYAN (DEFAULT)", new Color(0f, 1f, 1f), "Signature Quantum OS aesthetic with crisp luminescent cyan accents and dark navy backdrop."),
                ("SYNTHWAVE NEON PINK", new Color(1f, 0.2f, 0.75f), "Retro 80s outrun palette with vibrant magenta glows and synthwave harmonics."),
                ("MATRIX OBSIDIAN GREEN", new Color(0f, 1f, 0.4f), "Terminal hacker green with ultra-deep black contrast and terminal typography."),
                ("SOLAR FLARE AMBER", new Color(1f, 0.75f, 0.1f), "Warm energetic gold styling reminiscent of orbital satellite solar arrays.")
            };

            float y = -90f;
            float cardH = 150f;

            for (int i = 0; i < themes.Length; i++)
            {
                int tIdx = i;
                Color col = themes[i].accent;
                GameObject card = CreateUIObject($"ThemeCard_{i}", page.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                                                 new Vector2(40f, y - cardH), new Vector2(-40f, y), _cardDark);

                CreateUIObject("TopAccent", card.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                               new Vector2(0f, -4f), new Vector2(0f, 0f), col);

                CreateTextObject("Title", card.transform, new Vector2(0f, 1f), new Vector2(0.7f, 1f),
                                 new Vector2(24f, -45f), new Vector2(0f, -10f),
                                 $"<b>{themes[i].name}</b>", 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, col);

                CreateTextObject("Desc", card.transform, new Vector2(0f, 0f), new Vector2(0.7f, 1f),
                                 new Vector2(24f, 15f), new Vector2(0f, -50f),
                                 themes[i].desc, 18, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);

                CreateButton($"ApplyTheme_{i}", card.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                             new Vector2(-240f, -28f), new Vector2(-24f, 28f),
                             "APPLY THEME", col,
                             () => ApplyBrowserTheme(col), 18);

                y -= cardH + 20f;
            }

            return page;
        }

        private void ApplyBrowserTheme(Color accent)
        {
            _primaryAccent = accent;
            TriggerLoadingSweep();
            UpdateTabHeaders();
            AudioManager.Instance?.PlayConnectChime();
        }

        // =========================================================================
        // UI BUILDER HELPERS
        // =========================================================================

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

        private Button CreateButton(string name, Transform parent, Vector2 aMin, Vector2 aMax,
                                   Vector2 offMin, Vector2 offMax, string label, Color bgColor,
                                   UnityEngine.Events.UnityAction onClick, float fontSize = 18f)
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
                             label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

            return btn;
        }

        private Button CreateNavIconButton(string name, Transform parent, Vector2 posMin, Vector2 posMax,
                                          string icon, UnityEngine.Events.UnityAction onClick, Color? customBg = null)
        {
            Color bg = customBg ?? new Color(0.08f, 0.14f, 0.22f, 0.95f);
            return CreateButton(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                posMin - new Vector2(0f, 32f), posMax - new Vector2(0f, 32f),
                                icon, bg, onClick, 20f);
        }
    }
}
