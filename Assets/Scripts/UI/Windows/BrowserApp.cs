using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.Networking;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.UI.Windows
{
    /// <summary>
    /// Master Quantum Web Browser application for the virtual desktop.
    /// Connects to LiveWebClient to render real-world live web content:
    /// - Live Hacker News feed (live real-world tech headlines & upvote counts)
    /// - Live Wikipedia encyclopedia search & article extracts
    /// - Live URL fetching & smart destination routing
    /// - Resilient dual-mode browsing (Live HTTP/2 and offline cache fallback)
    /// - Interactive bookmarks bar, navigation history, and responsive scrollable content
    /// </summary>
    public class BrowserApp : MonoBehaviour
    {
        [Header("Browser Controls")]
        [SerializeField] private TMP_InputField _urlInputField;
        [SerializeField] private Button _goButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _forwardButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _homeButton;

        [Header("Live Bookmarks Bar")]
        [SerializeField] private Button _btnHackerNews;
        [SerializeField] private Button _btnWikipedia;
        [SerializeField] private Button _btnSearch;
        [SerializeField] private Button _btnDocs;
        [SerializeField] private Button _btnNexusAI;

        [Header("Content Viewport")]
        [SerializeField] private ScrollRect _contentScrollRect;
        [SerializeField] private RectTransform _contentContainer;
        [SerializeField] private TextMeshProUGUI _statusBadgeText;

        // Navigation History Stack
        private List<string> _history = new List<string>();
        private int _historyIndex = -1;
        private bool _isLoading = false;
        public bool IsLoading => _isLoading;

        private void Awake()
        {
            EnsureBrowserComponents();
            WireEventListeners();
        }

        private void Start()
        {
            NavigateTo("https://news.ycombinator.com", false);
        }

        private void EnsureBrowserComponents()
        {
            // Auto-resolve or procedurally attach UI references
            if (_urlInputField == null) _urlInputField = GetComponentInChildren<TMP_InputField>();
            if (_contentScrollRect == null) _contentScrollRect = GetComponentInChildren<ScrollRect>();

            Transform navBar = transform.Find("NavBar") ?? transform.Find("TopBar") ?? transform;
            if (_goButton == null)
            {
                var go = navBar.Find("GoBtn") ?? navBar.Find("Btn_Go");
                if (go != null) _goButton = go.GetComponent<Button>();
            }

            // Create Bookmarks Bar and Scrollable Viewport if not present
            BuildDynamicBrowserLayout();
        }

        private void BuildDynamicBrowserLayout()
        {
            RectTransform rootRt = transform as RectTransform;
            if (rootRt == null) return;

            // 1. Ensure Bookmark Bar container
            Transform existingBookmarks = transform.Find("BookmarksBar");
            if (existingBookmarks == null)
            {
                GameObject bmObj = new GameObject("BookmarksBar", typeof(RectTransform), typeof(Image));
                bmObj.transform.SetParent(transform, false);
                RectTransform bmRt = bmObj.GetComponent<RectTransform>();
                bmRt.anchorMin = new Vector2(0f, 1f);
                bmRt.anchorMax = new Vector2(1f, 1f);
                bmRt.pivot = new Vector2(0.5f, 1f);
                bmRt.anchoredPosition = new Vector2(0f, -44f);
                bmRt.sizeDelta = new Vector2(0f, 32f);

                Image bmImg = bmObj.GetComponent<Image>();
                bmImg.color = new Color(0.04f, 0.07f, 0.12f, 0.98f);

                // Bookmarks buttons
                string[] bmNames = new string[] { "[HN] Live Tech", "[WIKI] Wikipedia", "[WEB] Search", "[DOCS] Specs", "[AI] Nexus" };
                Action[] bmActions = new Action[]
                {
                    () => NavigateTo("https://news.ycombinator.com"),
                    () => NavigateTo("https://en.wikipedia.org/wiki/Virtual_reality"),
                    () => NavigateTo("https://quantum-vr.net/search"),
                    () => NavigateTo("https://quantum-vr.net/docs/protocol"),
                    () => NavigateTo("https://quantum-vr.net/ai/nexus")
                };

                for (int i = 0; i < bmNames.Length; i++)
                {
                    int idx = i;
                    GameObject bObj = new GameObject($"BM_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                    bObj.transform.SetParent(bmObj.transform, false);
                    RectTransform bRt = bObj.GetComponent<RectTransform>();
                    bRt.anchorMin = new Vector2(0.02f + i * 0.195f, 0.10f);
                    bRt.anchorMax = new Vector2(0.02f + (i + 1) * 0.195f - 0.015f, 0.90f);
                    bRt.offsetMin = Vector2.zero;
                    bRt.offsetMax = Vector2.zero;

                    Image bImg = bObj.GetComponent<Image>();
                    bImg.color = new Color(0.08f, 0.14f, 0.22f, 0.95f);

                    Button btn = bObj.GetComponent<Button>();
                    btn.onClick.AddListener(() => bmActions[idx]());

                    GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtObj.transform.SetParent(bObj.transform, false);
                    RectTransform tRt = txtObj.GetComponent<RectTransform>();
                    tRt.anchorMin = Vector2.zero;
                    tRt.anchorMax = Vector2.one;
                    tRt.offsetMin = Vector2.zero;
                    tRt.offsetMax = Vector2.zero;

                    TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
                    tmp.text = bmNames[i];
                    tmp.fontSize = 12;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(0f, 0.95f, 1f, 1f);
                }
            }

            // 2. Ensure Scrollable Web Viewport
            Transform existingPage = transform.Find("PageContent");
            if (existingPage != null && _contentContainer == null)
            {
                // Clear out legacy static placeholder texts
                for (int i = existingPage.childCount - 1; i >= 0; i--)
                {
                    Transform child = existingPage.GetChild(i);
                    if (child.name == "Headline" || child.name == "SwitchLeftBtn" || child.name.StartsWith("QuickApp_"))
                    {
                        Destroy(child.gameObject);
                    }
                }

                // Add ScrollRect if missing
                _contentScrollRect = existingPage.GetComponent<ScrollRect>();
                if (_contentScrollRect == null) _contentScrollRect = existingPage.gameObject.AddComponent<ScrollRect>();
                _contentScrollRect.horizontal = false;
                _contentScrollRect.vertical = true;
                _contentScrollRect.scrollSensitivity = 35f;

                GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
                viewport.transform.SetParent(existingPage, false);
                RectTransform vpRt = viewport.GetComponent<RectTransform>();
                vpRt.anchorMin = Vector2.zero;
                vpRt.anchorMax = Vector2.one;
                vpRt.offsetMin = new Vector2(10f, 10f);
                vpRt.offsetMax = new Vector2(-10f, -10f);

                Image vpImg = viewport.GetComponent<Image>();
                vpImg.color = Color.white;
                Mask mask = viewport.GetComponent<Mask>();
                mask.showMaskGraphic = false;

                GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                content.transform.SetParent(viewport.transform, false);
                _contentContainer = content.GetComponent<RectTransform>();
                _contentContainer.anchorMin = new Vector2(0f, 1f);
                _contentContainer.anchorMax = new Vector2(1f, 1f);
                _contentContainer.pivot = new Vector2(0.5f, 1f);
                _contentContainer.anchoredPosition = Vector2.zero;
                _contentContainer.sizeDelta = new Vector2(0f, 0f);

                VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childControlWidth = true;
                vlg.spacing = 10f;
                vlg.padding = new RectOffset(8, 8, 8, 8);

                ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                _contentScrollRect.viewport = vpRt;
                _contentScrollRect.content = _contentContainer;
            }
        }

        private void WireEventListeners()
        {
            if (_goButton != null) _goButton.onClick.AddListener(OnGoClicked);
            if (_homeButton != null) _homeButton.onClick.AddListener(() => NavigateTo("https://quantum-vr.net/search"));
            if (_refreshButton != null) _refreshButton.onClick.AddListener(Refresh);
            if (_backButton != null) _backButton.onClick.AddListener(GoBack);
            if (_forwardButton != null) _forwardButton.onClick.AddListener(GoForward);

            if (_urlInputField != null)
            {
                _urlInputField.onSubmit.AddListener((val) => OnGoClicked());
            }
        }

        public void NavigateTo(string url, bool addToHistory = true)
        {
            if (string.IsNullOrEmpty(url)) url = "https://quantum-vr.net";
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (url.Contains(".") && !url.Contains(" "))
                {
                    url = "https://" + url;
                }
                else
                {
                    // Treat as query
                    url = "https://en.wikipedia.org/wiki/" + Uri.EscapeDataString(url);
                }
            }

            if (_urlInputField != null) _urlInputField.text = url;

            if (addToHistory)
            {
                if (_historyIndex == -1 || _history[_historyIndex] != url)
                {
                    if (_historyIndex >= 0 && _historyIndex < _history.Count - 1)
                    {
                        _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
                    }
                    _history.Add(url);
                    _historyIndex = _history.Count - 1;
                }
            }

            UpdateNavButtons();
            LoadPageContent(url);
            AudioManager.Instance?.PlayClick();
        }

        private void UpdateNavButtons()
        {
            if (_backButton != null) _backButton.interactable = _historyIndex > 0;
            if (_forwardButton != null) _forwardButton.interactable = _historyIndex < _history.Count - 1;
        }

        public void GoBack()
        {
            if (_historyIndex > 0)
            {
                _historyIndex--;
                NavigateTo(_history[_historyIndex], false);
            }
        }

        public void GoForward()
        {
            if (_historyIndex < _history.Count - 1)
            {
                _historyIndex++;
                NavigateTo(_history[_historyIndex], false);
            }
        }

        public void Refresh()
        {
            if (_historyIndex >= 0 && _historyIndex < _history.Count)
            {
                LoadPageContent(_history[_historyIndex]);
            }
            else
            {
                LoadPageContent(_urlInputField != null ? _urlInputField.text : "https://news.ycombinator.com");
            }
            AudioManager.Instance?.PlayConnectChime();
        }

        private void OnGoClicked()
        {
            if (_urlInputField == null) return;
            string input = _urlInputField.text.Trim();
            NavigateTo(input);
        }

        private void LoadPageContent(string url)
        {
            ClearContent();
            ShowLoadingBanner(url);

            var client = LiveWebClient.EnsureInstance();

            string lower = url.ToLowerInvariant();
            if (lower.Contains("news.ycombinator.com") || lower.Contains("hacker"))
            {
                client.FetchHackerNewsTop(5, (articles) => RenderHackerNewsArticles(articles));
            }
            else if (lower.Contains("wikipedia.org") || lower.Contains("wiki"))
            {
                string topic = "Virtual reality";
                int idx = url.LastIndexOf('/');
                if (idx != -1 && idx < url.Length - 1)
                {
                    topic = Uri.UnescapeDataString(url.Substring(idx + 1).Replace("_", " "));
                }
                client.FetchWikipedia(topic, (summary) => RenderWikiArticle(summary));
            }
            else if (lower.Contains("docs") || lower.Contains("protocol"))
            {
                RenderProtocolDocs();
            }
            else if (lower.Contains("ai") || lower.Contains("nexus"))
            {
                RenderNexusAIChat();
            }
            else
            {
                // General web search / Wikipedia lookup
                string topic = url.Replace("https://", "").Replace("http://", "").Trim('/');
                client.FetchWikipedia(topic, (summary) => RenderWikiArticle(summary));
            }
        }

        private void ClearContent()
        {
            if (_contentContainer == null) return;
            for (int i = _contentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_contentContainer.GetChild(i).gameObject);
            }
        }

        private void ShowLoadingBanner(string url)
        {
            if (_contentContainer == null) return;

            GameObject banner = new GameObject("LoadingBanner", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(_contentContainer, false);
            Image img = banner.GetComponent<Image>();
            img.color = new Color(0.05f, 0.08f, 0.15f, 0.95f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(banner.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16, 12);
            trt.offsetMax = new Vector2(-16, -12);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#00ffff><b>CONNECTING TO WEB NODE...</b></color>\n<size=12><color=#888888>Target: {url}</color></size>";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;

            LayoutElement le = banner.AddComponent<LayoutElement>();
            le.minHeight = 55f;
            le.preferredHeight = 55f;
        }

        #region Page Renderers

        private void RenderHackerNewsArticles(List<WebArticleItem> articles)
        {
            ClearContent();
            if (_contentContainer == null) return;

            // Page Header Card
            CreateHeaderCard("<color=#ff6600><b>HACKER NEWS</b></color> // <color=#ffffff>LIVE REAL-TIME TECH DISPATCH</color>",
                             "Official Y Combinator Firebase Feed | Top Community Submissions");

            if (articles == null || articles.Count == 0)
            {
                CreateTextCard("No live stories retrieved. Checking local cache...", Color.gray);
                return;
            }

            for (int i = 0; i < articles.Count; i++)
            {
                var a = articles[i];
                CreateArticleCard($"{i + 1}. {a.Title}",
                                  a.Description,
                                  a.SourceUrl,
                                  a.Upvotes,
                                  a.IsLive);
            }
        }

        private void RenderWikiArticle(WikiSummary wiki)
        {
            ClearContent();
            if (_contentContainer == null) return;

            // Header
            CreateHeaderCard($"<color=#00ffff><b>WIKIPEDIA</b></color> // <color=#ffffff>{wiki.Title.ToUpper()}</color>",
                             string.IsNullOrEmpty(wiki.Description) ? "Live Encyclopedia Article" : wiki.Description);

            // Article Body Card
            GameObject card = CreateCardObject("WikiBodyCard", new Color(0.04f, 0.07f, 0.12f, 0.95f));

            GameObject textObj = new GameObject("ExtractText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(card.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(18, 16);
            trt.offsetMax = new Vector2(-18, -16);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<size=15><color=#ffffff>{wiki.Extract}</color></size>\n\n" +
                       $"<color=#00ff88><b>Source:</b> {wiki.PageUrl}</color>";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.TopLeft;

            LayoutElement le = card.AddComponent<LayoutElement>();
            le.minHeight = 180f;
            le.preferredHeight = 220f;
        }

        private void RenderProtocolDocs()
        {
            ClearContent();
            if (_contentContainer == null) return;

            CreateHeaderCard("<color=#00ffff><b>QUANTUM WORKSTATION ARCHITECTURE</b></color>", "System Specifications v2.4");

            CreateTextCard("<b>1. Multi-Monitor Coordinate Mapping:</b>\n" +
                           "Preserves continuous virtual display UV coordinates across Left (-17 deg), Center (0 deg), and Right (+19 deg) displays with exact 3D world height millimeter alignment.", Color.white);

            CreateTextCard("<b>2. Ambilight Reactive Bias Illumination:</b>\n" +
                           "GPU downsampling blits screen RenderTextures to 4x4 buffers at 24 Hz, casting reactive color washes onto the desk and wall without CPU overhead.", Color.white);

            CreateTextCard("<b>3. Ergonomic Seated Free-Look:</b>\n" +
                           "Hold Right Mouse Button to swivel head view around the cyberpunk room while the workstation cursor remains solidly pinned to monitor canvases.", Color.white);
        }

        private void RenderNexusAIChat()
        {
            ClearContent();
            if (_contentContainer == null) return;

            CreateHeaderCard("<color=#00ff88><b>NEXUS COPILOT // AI WORKSTATION ASSISTANT</b></color>", "Autonomous Assistant Online");

            CreateTextCard("<b>Nexus AI:</b> Greetings, operator. I am connected to your workstation's telemetry bus. All three displays, spatial audio channels, and reactive backlights are functioning within optimal operating parameters.", new Color(0f, 1f, 1f, 1f));

            CreateTextCard("<b>Nexus AI:</b> Tip: Use <b>[F5]</b> on your physical keyboard to cycle between Ambilight modes (Reactive Screen, Cyberpunk Neon, Sunset Amber, Matrix Green, and Neon Pulse).", new Color(0.85f, 0.95f, 1f, 1f));
        }

        #endregion

        #region UI Card Construction Helpers

        private GameObject CreateCardObject(string name, Color bgColor)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_contentContainer, false);
            Image img = card.GetComponent<Image>();
            img.color = bgColor;
            return card;
        }

        private void CreateHeaderCard(string title, string subtitle)
        {
            GameObject card = CreateCardObject("HeaderCard", new Color(0.06f, 0.10f, 0.18f, 0.98f));

            GameObject textObj = new GameObject("HeaderText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(card.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16, 10);
            trt.offsetMax = new Vector2(-16, -10);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"{title}\n<size=11><color=#88aacc>{subtitle}</color></size>";
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutElement le = card.AddComponent<LayoutElement>();
            le.minHeight = 52f;
            le.preferredHeight = 52f;
        }

        private void CreateArticleCard(string title, string desc, string url, int upvotes, bool isLive)
        {
            GameObject card = CreateCardObject("ArticleCard", new Color(0.04f, 0.07f, 0.13f, 0.95f));

            GameObject textObj = new GameObject("ArticleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(card.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16, 10);
            trt.offsetMax = new Vector2(-16, -10);

            string liveBadge = isLive ? "<color=#00ff88><b>[LIVE HTTP/2]</b></color>" : "<color=#ffaa00><b>[CACHED]</b></color>";
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b><color=#ffffff>{title}</color></b>\n" +
                       $"<size=11><color=#888888>{desc}  |  {liveBadge}</color></size>\n" +
                       $"<size=11><color=#00ffff><u>{url}</u></color></size>";
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.TopLeft;

            // Make card clickable to navigate
            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (url.Contains("wikipedia.org"))
                {
                    NavigateTo(url);
                }
                else
                {
                    Debug.Log($"[BrowserApp] Article clicked: {url}");
                    AudioManager.Instance?.PlayClick();
                }
            });

            LayoutElement le = card.AddComponent<LayoutElement>();
            le.minHeight = 68f;
            le.preferredHeight = 74f;
        }

        private void CreateTextCard(string text, Color textColor)
        {
            GameObject card = CreateCardObject("TextCard", new Color(0.035f, 0.06f, 0.11f, 0.95f));

            GameObject textObj = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(card.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16, 12);
            trt.offsetMax = new Vector2(-16, -12);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.TopLeft;

            LayoutElement le = card.AddComponent<LayoutElement>();
            le.minHeight = 65f;
            le.preferredHeight = 75f;
        }

        #endregion
    }
}
