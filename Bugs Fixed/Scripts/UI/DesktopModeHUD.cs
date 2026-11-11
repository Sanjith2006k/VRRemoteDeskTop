using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.Computer;

namespace VRRemoteDesktop.UI
{
    /// <summary>
    /// Screen-Space Overlay HUD displayed while in Computer Interaction Mode.
    /// Provides clear clickable buttons to slide between Left, Center, and Right monitors,
    /// a Lean-In (Zoom) toggle, Stand Up button, and keyboard navigation shortcuts guide.
    /// </summary>
    public class DesktopModeHUD : MonoBehaviour
    {
        private static DesktopModeHUD _instance;
        public static DesktopModeHUD Instance
        {
            get
            {
                if (_instance == null) _instance = EnsureInstance();
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Components")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _leftMonitorButton;
        [SerializeField] private Button _centerMonitorButton;
        [SerializeField] private Button _rightMonitorButton;
        [SerializeField] private Button _zoomButton;

        [Header("Animation")]
        [SerializeField] private float _fadeSpeed = 8.0f;

        private float _targetAlpha = 0f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            if (ComputerInteractionManager.Instance != null)
            {
                ComputerInteractionManager.Instance.OnModeChanged += HandleModeChanged;
            }
        }

        private void OnDestroy()
        {
            if (ComputerInteractionManager.Instance != null)
            {
                ComputerInteractionManager.Instance.OnModeChanged -= HandleModeChanged;
            }
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.deltaTime);
                _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0.5f;
            }
        }

        private void HandleModeChanged(WorkstationMode mode)
        {
            Show(mode == WorkstationMode.ComputerInteraction);
        }

        public void Show(bool show)
        {
            _targetAlpha = show ? 1.0f : 0.0f;
        }

        private void OnExitClicked()
        {
            ComputerInteractionManager.Instance?.ExitComputerMode();
        }

        private void OnLeftClicked()
        {
            ComputerInteractionManager.Instance?.SlideToMonitor(ActiveDisplayIndex.Left);
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayKeyClick();
        }

        private void OnCenterClicked()
        {
            ComputerInteractionManager.Instance?.SlideToMonitor(ActiveDisplayIndex.Center);
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayKeyClick();
        }

        private void OnRightClicked()
        {
            ComputerInteractionManager.Instance?.SlideToMonitor(ActiveDisplayIndex.Right);
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayKeyClick();
        }

        private void OnZoomClicked()
        {
            ComputerInteractionManager.Instance?.ToggleZoom();
            VRRemoteDesktop.Audio.AudioManager.Instance?.PlayKeyClick();
        }

        public static DesktopModeHUD EnsureInstance()
        {
            if (_instance != null) return _instance;
            _instance = Object.FindAnyObjectByType<DesktopModeHUD>();
            if (_instance != null) return _instance;

            // Procedurally construct the HUD ScreenSpaceOverlay Canvas
            GameObject hudObj = new GameObject("DesktopModeHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(DesktopModeHUD));
            Canvas canvas = hudObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = hudObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            CanvasGroup cg = hudObj.GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            // 1. Top Navigation Dock Bar (Centered at y = -24, width = 1100, height = 48)
            GameObject topBarObj = new GameObject("TopNavDock", typeof(RectTransform), typeof(Image));
            topBarObj.transform.SetParent(hudObj.transform, false);
            RectTransform trt = topBarObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 1f);
            trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, -14f);
            trt.sizeDelta = new Vector2(1150f, 50f);
            topBarObj.GetComponent<Image>().color = new Color(0.035f, 0.06f, 0.12f, 0.94f);

            // Top bar cyan border
            GameObject tBorder = new GameObject("Border", typeof(RectTransform), typeof(Image));
            tBorder.transform.SetParent(topBarObj.transform, false);
            RectTransform brt = tBorder.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.anchoredPosition = new Vector2(0f, 0f);
            brt.sizeDelta = new Vector2(0f, 2f);
            tBorder.GetComponent<Image>().color = new Color(0f, 1f, 1f, 0.8f);

            // Button 1: Left Monitor Focus
            Button btnLeft = CreateNavButton(topBarObj.transform, new Vector2(12f, 0f), new Vector2(210f, 36f),
                                            "◀ L-01 TELEMETRY (1)", new Color(0.05f, 0.35f, 0.45f));

            // Button 2: Center Monitor Focus
            Button btnCenter = CreateNavButton(topBarObj.transform, new Vector2(232f, 0f), new Vector2(210f, 36f),
                                              "● DESKTOP (2)", new Color(0.1f, 0.25f, 0.5f));

            // Button 3: Right Monitor Focus
            Button btnRight = CreateNavButton(topBarObj.transform, new Vector2(452f, 0f), new Vector2(210f, 36f),
                                             "R-01 NOC (3) ▶", new Color(0.35f, 0.15f, 0.5f));

            // Button 4: Lean In (Zoom)
            Button btnZoom = CreateNavButton(topBarObj.transform, new Vector2(672f, 0f), new Vector2(180f, 36f),
                                            "🔍 LEAN IN (W/S)", new Color(0.1f, 0.35f, 0.25f));

            // Button 5: Stand Up (Exit)
            Button btnExit = CreateNavButton(topBarObj.transform, new Vector2(862f, 0f), new Vector2(265f, 36f),
                                            "🚶 STAND UP (E / ESC)", new Color(0.55f, 0.1f, 0.25f));

            // 2. Bottom Keyboard Navigation Help Strip
            GameObject bottomBarObj = new GameObject("BottomHelpStrip", typeof(RectTransform), typeof(Image));
            bottomBarObj.transform.SetParent(hudObj.transform, false);
            RectTransform brtBottom = bottomBarObj.GetComponent<RectTransform>();
            brtBottom.anchorMin = new Vector2(0.5f, 0f);
            brtBottom.anchorMax = new Vector2(0.5f, 0f);
            brtBottom.pivot = new Vector2(0.5f, 0f);
            brtBottom.anchoredPosition = new Vector2(0f, 14f);
            brtBottom.sizeDelta = new Vector2(1200f, 40f);
            bottomBarObj.GetComponent<Image>().color = new Color(0.03f, 0.05f, 0.10f, 0.92f);

            GameObject helpTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            helpTextObj.transform.SetParent(bottomBarObj.transform, false);
            RectTransform hrt = helpTextObj.GetComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero;
            hrt.anchorMax = Vector2.one;
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;
            var hTmp = helpTextObj.GetComponent<TextMeshProUGUI>();
            hTmp.text = "<color=#00ffff><b>[A/D or 1,2,3]</b> Slide Between PCs</color>  ●  <color=#00ff88><b>[W/S]</b> Lean In (Zoom)</color>  ●  <color=#ffcc00><b>[Hold RMB]</b> Swivel Head Look</color>  ●  <color=#ff5588><b>[E / ESC]</b> Stand Up</color>";
            hTmp.fontSize = 20;
            hTmp.fontStyle = FontStyles.Bold;
            hTmp.alignment = TextAlignmentOptions.Center;
            hTmp.color = Color.white;

            DesktopModeHUD hud = hudObj.GetComponent<DesktopModeHUD>();
            hud._canvasGroup = cg;
            hud._exitButton = btnExit;
            hud._leftMonitorButton = btnLeft;
            hud._centerMonitorButton = btnCenter;
            hud._rightMonitorButton = btnRight;
            hud._zoomButton = btnZoom;

            btnExit.onClick.AddListener(hud.OnExitClicked);
            btnLeft.onClick.AddListener(hud.OnLeftClicked);
            btnCenter.onClick.AddListener(hud.OnCenterClicked);
            btnRight.onClick.AddListener(hud.OnRightClicked);
            btnZoom.onClick.AddListener(hud.OnZoomClicked);

            _instance = hud;
            return hud;
        }

        private static Button CreateNavButton(Transform parent, Vector2 anchoredPos, Vector2 size, string label, Color bgColor)
        {
            GameObject btnObj = new GameObject($"Btn_{label.Substring(0, Mathf.Min(6, label.Length))}", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = bgColor * 1.35f;
            cb.pressedColor = bgColor * 0.75f;
            cb.selectedColor = bgColor * 1.2f;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }
    }
}
