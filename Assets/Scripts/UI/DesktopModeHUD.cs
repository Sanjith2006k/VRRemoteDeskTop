using System.Collections;
using UnityEngine;

using UnityEngine.UI;

using TMPro;

using VRRemoteDesktop.Computer;

using VRRemoteDesktop.XR;
using VRRemoteDesktop.Remote;



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



        [Header("Master Workstation Cursor")]

        [SerializeField] private RectTransform _masterCursorRt;



        [SerializeField] private Image _masterCursorImage;



        [SerializeField] private Color _cursorNormalColor = new Color(0f, 0.95f, 1f, 1f);      // Bright Neon Cyan



        [SerializeField] private Color _cursorHoverColor = new Color(0.25f, 1.0f, 0.65f, 1f);   // Vivid Neon Green



        [SerializeField] private Color _cursorClickColor = new Color(1.0f, 0.30f, 0.55f, 1f);   // High-Contrast Magenta



        [SerializeField] private float _cursorSize = 56f;



        private float _targetAlpha = 0f;

        [Header("Dynamic HUD & Notification Banner")]
        [SerializeField] private TextMeshProUGUI _bottomHelpText;
        private GameObject _notificationBanner;
        private TextMeshProUGUI _notificationBannerText;
        private CanvasGroup _notificationBannerCg;
        private Coroutine _bannerRoutine;



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
                if (ComputerInteractionManager.Instance.IsInteractingAtDesktop)
                {
                    Show(true);
                }
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
            var cim = ComputerInteractionManager.Instance;
            bool inComputerMode = cim != null && cim.IsInteractingAtDesktop;
            if (inComputerMode && _targetAlpha < 0.5f)
            {
                Show(true);
            }
            else if (!inComputerMode && _targetAlpha > 0.5f)
            {
                Show(false);
            }


            if (_canvasGroup != null)



            {

                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.deltaTime);



                _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0.5f;

            }



            UpdateMasterCursor();

        }



        private void HandleModeChanged(WorkstationMode mode)



        {

            Show(mode == WorkstationMode.ComputerInteraction);

        }



        public void Show(bool show)



        {

            _targetAlpha = show ? 1.0f : 0.0f;



            if (show && _canvasGroup != null)



            {

                _canvasGroup.alpha = 1.0f; // Instant availability upon sitting down

            }

        }



        private void EnsureMasterCursor()



        {

            if (_masterCursorRt != null) return;



            Transform existing = transform.Find("MasterWorkstationCursor");



            if (existing != null)



            {

                _masterCursorRt = existing.GetComponent<RectTransform>();



                _masterCursorImage = existing.GetComponent<Image>();



                return;

            }



            GameObject cursorObj = new GameObject("MasterWorkstationCursor", typeof(RectTransform), typeof(Image));



            cursorObj.transform.SetParent(transform, false);



            _masterCursorRt = cursorObj.GetComponent<RectTransform>();



            _masterCursorRt.anchorMin = new Vector2(0.5f, 0.5f);



            _masterCursorRt.anchorMax = new Vector2(0.5f, 0.5f);



            _masterCursorRt.pivot = new Vector2(0.08f, 0.92f);



            _masterCursorRt.sizeDelta = new Vector2(_cursorSize, _cursorSize);
            CanvasGroup cursorCg = cursorObj.AddComponent<CanvasGroup>();
            cursorCg.ignoreParentGroups = true;
            cursorCg.alpha = 1f;



            Sprite arrowSprite = Resources.Load<Sprite>("Tex_CursorArrow");



            if (arrowSprite == null)



            {

#if UNITY_EDITOR



                arrowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tex_CursorArrow.png");



#endif

            }



            // Drop shadow child for razor-sharp visibility across both light and dark screens



            GameObject shadowObj = new GameObject("Shadow", typeof(RectTransform), typeof(Image));



            shadowObj.transform.SetParent(_masterCursorRt, false);



            RectTransform srt = shadowObj.GetComponent<RectTransform>();



            srt.anchorMin = Vector2.zero;



            srt.anchorMax = Vector2.one;



            srt.offsetMin = new Vector2(2.5f, -2.5f);



            srt.offsetMax = new Vector2(2.5f, -2.5f);



            srt.pivot = new Vector2(0.08f, 0.92f);



            Image simg = shadowObj.GetComponent<Image>();



            simg.sprite = arrowSprite;



            simg.color = new Color(0f, 0f, 0f, 0.85f);



            simg.raycastTarget = false;



            _masterCursorImage = cursorObj.GetComponent<Image>();



            _masterCursorImage.sprite = arrowSprite;



            _masterCursorImage.color = _cursorNormalColor;



            _masterCursorImage.raycastTarget = false;



            _masterCursorRt.SetAsLastSibling();



            cursorObj.SetActive(false);

        }



        private void UpdateMasterCursor()



        {

            EnsureMasterCursor();



            if (_masterCursorRt == null) return;



            var cim = ComputerInteractionManager.Instance;



            bool inComputerMode = cim != null && cim.IsInteractingAtDesktop;



            if (!inComputerMode)



            {

                if (_masterCursorRt.gameObject.activeSelf)



                    _masterCursorRt.gameObject.SetActive(false);



                return;

            }



            var mouse = UnityEngine.InputSystem.Mouse.current;



            if (mouse == null) return;



            bool isRmbLooking = (cim != null && cim.IsRightClickLooking) || mouse.rightButton.isPressed;



            try



            {

                if (!isRmbLooking && UnityEngine.Input.GetMouseButton(1)) isRmbLooking = true;

            }



            catch {}



            if (isRmbLooking)
            {
                if (_masterCursorRt.gameObject.activeSelf)
                    _masterCursorRt.gameObject.SetActive(false);
                return;
            }

            // Ensure OS mouse arrow stays completely hidden & confined
            if (Cursor.visible) Cursor.visible = false;
            if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;

            Vector2 mousePos = mouse.position.ReadValue();

            // The master HUD overlay cursor is ONLY active when hovering HUD controls (dock / help strip).
            // When interacting with workstation monitors or the empty space around them, this cursor is hidden
            // so it never floats in the air; each monitor renders its own surface cursor.
            bool isOverHUD = IsMouseOverHUD(mousePos);
            if (!isOverHUD)
            {
                if (_masterCursorRt.gameObject.activeSelf)
                    _masterCursorRt.gameObject.SetActive(false);
                return;
            }

            if (!_masterCursorRt.gameObject.activeSelf)
            {
                _masterCursorRt.gameObject.SetActive(true);
                _masterCursorRt.SetAsLastSibling();
            }

            // Map mouse screen position into HUD canvas space for HUD buttons
            RectTransform canvasRt = transform as RectTransform;
            if (canvasRt != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, mousePos, null, out Vector2 localPoint))
            {
                float halfW = canvasRt.rect.width * 0.5f;
                float halfH = canvasRt.rect.height * 0.5f;
                localPoint.x = Mathf.Clamp(localPoint.x, -halfW + 10f, halfW - 10f);
                localPoint.y = Mathf.Clamp(localPoint.y, -halfH + 10f, halfH - 10f);
                _masterCursorRt.anchoredPosition = localPoint;
            }

            // Dynamic interaction feedback (Hover, Click/Drag, Normal)
            bool isClicking = mouse.leftButton.isPressed;



            try



            {

                if (!isClicking && UnityEngine.Input.GetMouseButton(0)) isClicking = true;

            }



            catch {}



            bool isHovering = IsAnyInteractiveHovered();



            Color targetColor = isClicking ? _cursorClickColor : (isHovering ? _cursorHoverColor : _cursorNormalColor);



            Vector3 targetScale = isClicking ? new Vector3(0.92f, 0.92f, 1f) : (isHovering ? new Vector3(1.10f, 1.10f, 1f) : Vector3.one);



            if (_masterCursorImage != null)



            {

                _masterCursorImage.color = Color.Lerp(_masterCursorImage.color, targetColor, Time.deltaTime * 25f);

            }



            _masterCursorRt.localScale = Vector3.Lerp(_masterCursorRt.localScale, targetScale, Time.deltaTime * 25f);

        }



        private bool IsMouseOverHUD(Vector2 mousePos)



        {

            if (_exitButton != null && RectTransformUtility.RectangleContainsScreenPoint(_exitButton.transform as RectTransform, mousePos, null)) return true;



            if (_leftMonitorButton != null && RectTransformUtility.RectangleContainsScreenPoint(_leftMonitorButton.transform as RectTransform, mousePos, null)) return true;



            if (_centerMonitorButton != null && RectTransformUtility.RectangleContainsScreenPoint(_centerMonitorButton.transform as RectTransform, mousePos, null)) return true;



            if (_rightMonitorButton != null && RectTransformUtility.RectangleContainsScreenPoint(_rightMonitorButton.transform as RectTransform, mousePos, null)) return true;



            if (_zoomButton != null && RectTransformUtility.RectangleContainsScreenPoint(_zoomButton.transform as RectTransform, mousePos, null)) return true;



            Transform topNav = transform.Find("TopNavDock");



            if (topNav != null && RectTransformUtility.RectangleContainsScreenPoint(topNav as RectTransform, mousePos, null)) return true;



            Transform botStrip = transform.Find("BottomHelpStrip");



            if (botStrip != null && RectTransformUtility.RectangleContainsScreenPoint(botStrip as RectTransform, mousePos, null)) return true;



            return false;

        }



        private bool IsAnyInteractiveHovered()



        {

            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())



                return true;



            if (VRDesktopInteractable.Instance != null && VRDesktopInteractable.Instance.IsHoveringInteractive)



                return true;



            if (AuxiliaryScreenInteractable.IsAnyHoveringInteractive)



                return true;



            return false;

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



            canvas.sortingOrder = 999;



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



                                            "< L-01 TELEMETRY (1)", new Color(0.05f, 0.35f, 0.45f));



            // Button 2: Center Monitor Focus



            Button btnCenter = CreateNavButton(topBarObj.transform, new Vector2(232f, 0f), new Vector2(210f, 36f),



                                              "| DESKTOP (2)", new Color(0.1f, 0.25f, 0.5f));



            // Button 3: Right Monitor Focus



            Button btnRight = CreateNavButton(topBarObj.transform, new Vector2(452f, 0f), new Vector2(210f, 36f),



                                             "R-01 NOC (3) >", new Color(0.35f, 0.15f, 0.5f));



            // Button 4: Lean In (Zoom)



            Button btnZoom = CreateNavButton(topBarObj.transform, new Vector2(672f, 0f), new Vector2(180f, 36f),



                                            "[ZOOM] LEAN IN (W/S)", new Color(0.1f, 0.35f, 0.25f));



            // Button 5: Stand Up (Exit)



            Button btnExit = CreateNavButton(topBarObj.transform, new Vector2(862f, 0f), new Vector2(265f, 36f),



                                            "[EXIT] STAND UP (E / ESC)", new Color(0.55f, 0.1f, 0.25f));



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



            hTmp.text = "<color=#00ffff><b>[A/D or 1,2,3]</b> Slide</color>  |  <color=#00ff88><b>[W/S]</b> Zoom</color>  |  <color=#ffcc00><b>[RMB]</b> Look</color>  |  <color=#ff00ff><b>[F5]</b> Ambilight</color>  |  <color=#00e5ff><b>[F6]</b> Theme</color>  |  <color=#00ff88><b>[F7]</b> Host PC</color>  |  <color=#ff5588><b>[E / ESC]</b> Stand Up</color>";



            hTmp.fontSize = 20;



            hTmp.fontStyle = FontStyles.Bold;



            hTmp.alignment = TextAlignmentOptions.Center;



            hTmp.color = Color.white;



            DesktopModeHUD hud = hudObj.GetComponent<DesktopModeHUD>();
            hud._bottomHelpText = hTmp;



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



            hud.EnsureMasterCursor();



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

        #region Phase 3: Dynamic Bottom Help & Notification Banner

        public void UpdateBottomHelp(string ambilightMode, string themeName)
        {
            if (_bottomHelpText != null)
            {
                bool isHost = HostDesktopStreamManager.Instance != null && HostDesktopStreamManager.Instance.IsHostStreamActive;
                string hostColor = isHost ? "#00ff88" : "#888888";
                string hostStatus = isHost ? "STREAMING" : "OFF";

                bool isImax = VRRemoteDesktop.Environment.ScreenShareToImaxManager.Instance != null && VRRemoteDesktop.Environment.ScreenShareToImaxManager.Instance.IsScreenSharing;
                string imaxColor = isImax ? "#00ffff" : "#888888";
                string imaxStatus = isImax ? "PROJECTING" : "OFF";

                _bottomHelpText.text = $"<color=#00ffff><b>[A/D or 1,2,3]</b> Slide</color>  |  " +
                                       $"<color=#00ff88><b>[W/S]</b> Zoom</color>  |  " +
                                       $"<color=#ffcc00><b>[RMB]</b> Look</color>  |  " +
                                       $"<color=#ff00ff><b>[F5]</b> Ambilight: {ambilightMode}</color>  |  " +
                                       $"<color=#00e5ff><b>[F6]</b> Theme: {themeName}</color>  |  " +
                                       $"<color={hostColor}><b>[F7]</b> Host: {hostStatus}</color>  |  " +
                                       $"<color={imaxColor}><b>[F8]</b> IMAX: {imaxStatus}</color>  |  " +
                                       $"<color=#ff5588><b>[E / ESC]</b> Stand Up</color>";
            }
        }

        public void ShowNotificationBanner(string title, string subtitle, Color accentColor, float duration = 2.5f)
        {
            if (!gameObject.activeInHierarchy) return;

            if (_notificationBanner == null)
            {
                BuildNotificationBanner();
            }

            if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
            _bannerRoutine = StartCoroutine(BannerDisplayRoutine(title, subtitle, accentColor, duration));
        }

        private void BuildNotificationBanner()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            _notificationBanner = new GameObject("HUD_NotificationBanner", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _notificationBanner.transform.SetParent(parent, false);

            RectTransform rt = _notificationBanner.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -85f);
            rt.sizeDelta = new Vector2(580f, 62f);

            Image bg = _notificationBanner.GetComponent<Image>();
            bg.color = new Color(0.03f, 0.06f, 0.12f, 0.95f);

            _notificationBannerCg = _notificationBanner.GetComponent<CanvasGroup>();
            _notificationBannerCg.alpha = 0f;
            _notificationBannerCg.blocksRaycasts = false;

            // Top accent border
            GameObject topBorder = new GameObject("TopBorder", typeof(RectTransform), typeof(Image));
            topBorder.transform.SetParent(_notificationBanner.transform, false);
            RectTransform trt = topBorder.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.sizeDelta = new Vector2(0f, 2.5f);
            trt.anchoredPosition = new Vector2(0f, -1.25f);
            topBorder.GetComponent<Image>().color = new Color(0f, 1f, 1f, 1f);

            // Bottom accent border
            GameObject btmBorder = new GameObject("BottomBorder", typeof(RectTransform), typeof(Image));
            btmBorder.transform.SetParent(_notificationBanner.transform, false);
            RectTransform brt = btmBorder.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.sizeDelta = new Vector2(0f, 1.5f);
            brt.anchoredPosition = new Vector2(0f, 0.75f);
            btmBorder.GetComponent<Image>().color = new Color(0f, 1f, 1f, 0.4f);

            // Banner text
            GameObject txtObj = new GameObject("BannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(_notificationBanner.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            txtRt.offsetMin = new Vector2(16f, 6f);
            txtRt.offsetMax = new Vector2(-16f, -6f);

            _notificationBannerText = txtObj.GetComponent<TextMeshProUGUI>();
            _notificationBannerText.alignment = TextAlignmentOptions.Center;
            _notificationBannerText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private IEnumerator BannerDisplayRoutine(string title, string subtitle, Color accentColor, float duration)
        {
            if (_notificationBannerText != null)
            {
                string hex = ColorUtility.ToHtmlStringRGB(accentColor);
                _notificationBannerText.text = $"<size=20><color=#{hex}><b>* {title} *</b></color></size>\n" +
                                               $"<size=15><color=#bbddee>{subtitle}</color></size>";
            }

            if (_notificationBanner != null)
            {
                _notificationBanner.SetActive(true);
            }

            // Fade In
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                if (_notificationBannerCg != null)
                    _notificationBannerCg.alpha = Mathf.Lerp(0f, 1f, elapsed / 0.2f);
                yield return null;
            }
            if (_notificationBannerCg != null) _notificationBannerCg.alpha = 1f;

            yield return new WaitForSeconds(duration);

            // Fade Out
            elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                if (_notificationBannerCg != null)
                    _notificationBannerCg.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.4f);
                yield return null;
            }
            if (_notificationBannerCg != null) _notificationBannerCg.alpha = 0f;
            if (_notificationBanner != null) _notificationBanner.SetActive(false);
        }

        #endregion


    }

}

