using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using VRRemoteDesktop.Audio;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.UI;
using VRRemoteDesktop.Environment;

namespace VRRemoteDesktop.Remote
{
    /// <summary>
    /// Master Coordinator for Real Host Desktop Streaming & Bi-Directional Input Forwarding.
    /// Manages the real-time stream of the physical Windows PC into the Unity virtual workstation:
    /// - Displays the live Windows host OS on DesktopCanvas via a dedicated HostStreamSurface
    /// - Preserves the neon cyan virtual cursor (VRDesktopCursor) on top of the host desktop
    /// - Center Screen mesh renders the live desktop + cursor via CenterRenderTexture
    /// - VIP Theater IMAX screen projects the live upright desktop feed in real-time
    /// - Operates Windows seamlessly through Unity: mouse move, clicks, double-clicks, drag, scroll, and typing
    /// - Full alphanumeric and special keys forwarding (Enter, Backspace, Tab, Arrows, Win Key, Ctrl, Alt, Shift)
    /// - Hotkey [F7] for toggling host desktop streaming
    /// - Hotkey [F8] for toggling projection to 70mm VIP Theater IMAX Screen
    /// </summary>
    public class HostDesktopStreamManager : MonoBehaviour
    {
        public static HostDesktopStreamManager Instance { get; private set; }

        [Header("Host Streaming State")]
        [SerializeField] private bool _isHostStreamActive = true;
        [SerializeField] private bool _autoEnableInputForwarding = true;
        [SerializeField] private bool _autoStreamToImax = true;

        [Header("Components")]
        [SerializeField] private WindowsHostScreenCapture _screenCapture;
        [SerializeField] private WindowsHostInputForwarder _inputForwarder;

        private RemoteDesktopScreen _centerScreen;
        private RenderTexture _simulatedRenderTexture;
        private RectTransform _desktopCanvasRect;
        private RawImage _hostStreamDisplay;

        private float _lastClickTime = -1f;
        private Vector2 _lastClickUV = new Vector2(-1f, -1f);
        private const float DoubleClickThreshold = 0.45f;
        private const float DoubleClickUvTolerance = 0.02f;

        public bool IsHostStreamActive => _isHostStreamActive;
        public WindowsHostScreenCapture ScreenCapture => _screenCapture;
        public WindowsHostInputForwarder InputForwarder => _inputForwarder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureInstance();
        }

        public static HostDesktopStreamManager EnsureInstance()
        {
            if (Instance != null) return Instance;
            Instance = FindAnyObjectByType<HostDesktopStreamManager>();
            if (Instance != null) return Instance;

            GameObject go = new GameObject("HostDesktopStream_Manager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<HostDesktopStreamManager>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Application.runInBackground = true;
            EnsureComponents();
        }

        private void Start()
        {
            EnsureComponents();
            CacheScreenReferences();
            EnsureHostStreamSurface();

            StartCoroutine(AutoStartHostStreamRoutine());
        }

        private void OnEnable()
        {
            HookTextInput();
        }

        private void OnDisable()
        {
            UnhookTextInput();
        }

        private void OnDestroy()
        {
            UnhookTextInput();
        }

        private void HookTextInput()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                kb.onTextInput -= HandleTextInput;
                kb.onTextInput += HandleTextInput;
            }
        }

        private void UnhookTextInput()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                kb.onTextInput -= HandleTextInput;
            }
        }

        private void HandleTextInput(char c)
        {
            if (!_isHostStreamActive || _inputForwarder == null || !_inputForwarder.IsForwardingActive) return;

            // Only forward text input when interacting with the computer or focused on center screen
            if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.ActiveDisplay != ActiveDisplayIndex.Center) return;

            // Don't forward raw control characters that are handled via virtual keys
            if (c == '\r' || c == '\n' || c == '\b' || c == '\t' || c == '\x1b') return;

            _inputForwarder.ForwardCharInput(c);
        }

        private IEnumerator AutoStartHostStreamRoutine()
        {
            yield return null; // Wait 1 frame for all scene objects to initialize

            SetHostStreamActive(true);

            if (_autoStreamToImax)
            {
                ScreenShareToImaxManager.Instance?.SetScreenSharing(true);
            }
        }

        private void EnsureComponents()
        {
            if (_screenCapture == null)
            {
                _screenCapture = GetComponent<WindowsHostScreenCapture>();
                if (_screenCapture == null) _screenCapture = gameObject.AddComponent<WindowsHostScreenCapture>();
            }

            if (_inputForwarder == null)
            {
                _inputForwarder = GetComponent<WindowsHostInputForwarder>();
                if (_inputForwarder == null) _inputForwarder = gameObject.AddComponent<WindowsHostInputForwarder>();
            }
        }

        private void CacheScreenReferences()
        {
            if (MultiMonitorManager.Instance != null)
            {
                _centerScreen = MultiMonitorManager.Instance.CenterScreen;
                _simulatedRenderTexture = MultiMonitorManager.Instance.CenterRenderTexture;
            }

            if (_centerScreen == null)
            {
                var screens = FindObjectsByType<RemoteDesktopScreen>(FindObjectsInactive.Include);
                foreach (var s in screens)
                {
                    if (s.name.Contains("Center") || s.name.Contains("VirtualScreen") || s.name == "Screen")
                    {
                        _centerScreen = s;
                        break;
                    }
                }
            }
        }

        private void EnsureHostStreamSurface()
        {
            if (_desktopCanvasRect == null)
            {
                var canvasObj = GameObject.Find("DesktopCanvas");
                if (canvasObj != null) _desktopCanvasRect = canvasObj.GetComponent<RectTransform>();
            }

            if (_desktopCanvasRect == null) return;

            Transform existing = _desktopCanvasRect.Find("HostStreamDisplay");
            if (existing != null)
            {
                _hostStreamDisplay = existing.GetComponent<RawImage>();
            }

            if (_hostStreamDisplay == null)
            {
                GameObject go = new GameObject("HostStreamDisplay", typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(_desktopCanvasRect, false);

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                _hostStreamDisplay = go.GetComponent<RawImage>();
                _hostStreamDisplay.color = Color.white;
                _hostStreamDisplay.raycastTarget = false;

                // Position right before cursor so the cursor stays on top
                Transform cursorTr = _desktopCanvasRect.Find("VRCursor");
                if (cursorTr != null)
                {
                    go.transform.SetSiblingIndex(cursorTr.GetSiblingIndex());
                }
                else
                {
                    go.transform.SetAsLastSibling();
                }

                go.SetActive(false);
            }

            if (_screenCapture != null && _screenCapture.HostTexture != null)
            {
                _hostStreamDisplay.texture = _screenCapture.HostTexture;
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                // [F7] toggle Host PC Streaming
                if (kb.f7Key.wasPressedThisFrame)
                {
                    ToggleHostStream();
                }

                // [F8] toggle VIP Theater IMAX Projection
                if (kb.f8Key.wasPressedThisFrame)
                {
                    ScreenShareToImaxManager.Instance?.ToggleScreenShare();
                }
            }

            // Bridge cursor UV when host stream is active and cursor is on center screen
            if (_isHostStreamActive && _inputForwarder != null && _inputForwarder.IsForwardingActive)
            {
                bool isCenterActive = MultiMonitorManager.Instance == null || 
                                     MultiMonitorManager.Instance.ActiveDisplay == ActiveDisplayIndex.Center;

                if (isCenterActive)
                {
                    Vector2 uv = MultiMonitorManager.Instance != null ? MultiMonitorManager.Instance.CurrentUV : new Vector2(0.5f, 0.5f);
                    _inputForwarder.ForwardCursorPosition(uv);

                    var mouse = Mouse.current;
                    if (mouse != null)
                    {
                        // Left click & double-click detection
                        if (mouse.leftButton.wasPressedThisFrame)
                        {
                            float timeDelta = Time.unscaledTime - _lastClickTime;
                            float uvDist = Vector2.Distance(uv, _lastClickUV);

                            if (timeDelta <= DoubleClickThreshold && uvDist <= DoubleClickUvTolerance)
                            {
                                _inputForwarder.ForwardDoubleClick(0);
                                _lastClickTime = -1f;
                            }
                            else
                            {
                                _inputForwarder.ForwardMouseDown(0);
                                _lastClickTime = Time.unscaledTime;
                                _lastClickUV = uv;
                            }
                        }

                        if (mouse.leftButton.wasReleasedThisFrame)
                        {
                            _inputForwarder.ForwardMouseUp(0);
                        }

                        // Right click (context menus)
                        if (mouse.rightButton.wasPressedThisFrame) _inputForwarder.ForwardMouseDown(1);
                        if (mouse.rightButton.wasReleasedThisFrame) _inputForwarder.ForwardMouseUp(1);

                        // Middle click
                        if (mouse.middleButton.wasPressedThisFrame) _inputForwarder.ForwardMouseDown(2);
                        if (mouse.middleButton.wasReleasedThisFrame) _inputForwarder.ForwardMouseUp(2);

                        // Scroll wheel
                        float scroll = mouse.scroll.ReadValue().y;
                        if (Mathf.Abs(scroll) > 0.01f)
                        {
                            _inputForwarder.ForwardMouseScroll(scroll);
                        }
                    }

                    // Forward special navigation, editing and system keys
                    if (kb != null)
                    {
                        ForwardSpecialKeys(kb);
                    }
                }
            }
        }

        private void ForwardSpecialKeys(Keyboard kb)
        {
            // Enter key
            if (kb.enterKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x0D, true);
            if (kb.enterKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x0D, false);

            // Backspace key
            if (kb.backspaceKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x08, true);
            if (kb.backspaceKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x08, false);

            // Tab key
            if (kb.tabKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x09, true);
            if (kb.tabKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x09, false);

            // Space key
            if (kb.spaceKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x20, true);
            if (kb.spaceKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x20, false);

            // Delete key
            if (kb.deleteKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x2E, true);
            if (kb.deleteKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x2E, false);

            // Arrow keys
            if (kb.leftArrowKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x25, true);
            if (kb.leftArrowKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x25, false);

            if (kb.upArrowKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x26, true);
            if (kb.upArrowKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x26, false);

            if (kb.rightArrowKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x27, true);
            if (kb.rightArrowKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x27, false);

            if (kb.downArrowKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x28, true);
            if (kb.downArrowKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x28, false);

            // Windows Start Key
            if (kb.leftWindowsKey.wasPressedThisFrame || kb.rightWindowsKey.wasPressedThisFrame)
                _inputForwarder.ForwardVirtualKey(0x5B, true);
            if (kb.leftWindowsKey.wasReleasedThisFrame || kb.rightWindowsKey.wasReleasedThisFrame)
                _inputForwarder.ForwardVirtualKey(0x5B, false);

            // Home / End / PageUp / PageDown
            if (kb.homeKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x24, true);
            if (kb.homeKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x24, false);

            if (kb.endKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x23, true);
            if (kb.endKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x23, false);

            if (kb.pageUpKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x21, true);
            if (kb.pageUpKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x21, false);

            if (kb.pageDownKey.wasPressedThisFrame) _inputForwarder.ForwardVirtualKey(0x22, true);
            if (kb.pageDownKey.wasReleasedThisFrame) _inputForwarder.ForwardVirtualKey(0x22, false);

            // Ctrl / Shift / Alt Modifiers
            if (kb.leftCtrlKey.wasPressedThisFrame || kb.rightCtrlKey.wasPressedThisFrame)
                _inputForwarder.ForwardVirtualKey(0x11, true);
            if (kb.leftCtrlKey.wasReleasedThisFrame || kb.rightCtrlKey.wasReleasedThisFrame)
                _inputForwarder.ForwardVirtualKey(0x11, false);

            if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame)
                _inputForwarder.ForwardVirtualKey(0x10, true);
            if (kb.leftShiftKey.wasReleasedThisFrame || kb.rightShiftKey.wasReleasedThisFrame)
                _inputForwarder.ForwardVirtualKey(0x10, false);

            if (kb.leftAltKey.wasPressedThisFrame || kb.rightAltKey.wasPressedThisFrame)
                _inputForwarder.ForwardVirtualKey(0x12, true);
            if (kb.leftAltKey.wasReleasedThisFrame || kb.rightAltKey.wasReleasedThisFrame)
                _inputForwarder.ForwardVirtualKey(0x12, false);
        }

        /// <summary>
        /// Directly forwards pointer interactions originating from VR controllers or raycasters.
        /// </summary>
        public void ForwardPointerFromInteractable(Vector2 uv, bool leftDown, bool leftHeld, bool leftUp, Vector2 scroll)
        {
            if (!_isHostStreamActive || _inputForwarder == null || !_inputForwarder.IsForwardingActive) return;

            _inputForwarder.ForwardCursorPosition(uv);

            if (leftDown)
            {
                float timeDelta = Time.unscaledTime - _lastClickTime;
                float uvDist = Vector2.Distance(uv, _lastClickUV);

                if (timeDelta <= DoubleClickThreshold && uvDist <= DoubleClickUvTolerance)
                {
                    _inputForwarder.ForwardDoubleClick(0);
                    _lastClickTime = -1f;
                }
                else
                {
                    _inputForwarder.ForwardMouseDown(0);
                    _lastClickTime = Time.unscaledTime;
                    _lastClickUV = uv;
                }
            }

            if (leftUp)
            {
                _inputForwarder.ForwardMouseUp(0);
            }

            if (Mathf.Abs(scroll.y) > 0.01f)
            {
                _inputForwarder.ForwardMouseScroll(scroll.y);
            }
        }

        public void ToggleHostStream()
        {
            SetHostStreamActive(!_isHostStreamActive);
        }

        public void SetHostStreamActive(bool active)
        {
            CacheScreenReferences();
            EnsureHostStreamSurface();
            _isHostStreamActive = active;

            if (_isHostStreamActive)
            {
                // 1. Start Capture
                if (_screenCapture != null)
                {
                    _screenCapture.StartCapture();
                }

                // 2. Display Host Stream on DesktopCanvas (right-side up, with neon cyan cursor on top!)
                if (_hostStreamDisplay != null && _screenCapture != null)
                {
                    _hostStreamDisplay.texture = _screenCapture.HostTexture;
                    _hostStreamDisplay.gameObject.SetActive(true);

                    // Keep cursor on top
                    Transform cursorTr = _desktopCanvasRect != null ? _desktopCanvasRect.Find("VRCursor") : null;
                    if (cursorTr != null)
                    {
                        cursorTr.SetAsLastSibling();
                    }
                }
                else if (_centerScreen != null && _screenCapture != null)
                {
                    _centerScreen.SetTexture(_screenCapture.HostTexture);
                }

                if (_centerScreen != null && _simulatedRenderTexture != null)
                {
                    _centerScreen.SetTexture(_simulatedRenderTexture);
                }

                // 3. Enable Input Forwarding
                if (_inputForwarder != null && _autoEnableInputForwarding)
                {
                    if (_screenCapture != null)
                    {
                        _inputForwarder.SetScreenResolution(_screenCapture.Resolution.x, _screenCapture.Resolution.y);
                    }
                    _inputForwarder.SetInputForwarding(true);
                }

                // 4. Update IMAX Projection if sharing
                ScreenShareToImaxManager.Instance?.UpdateSharedTexture();

                // 5. Update HUD
                DesktopModeHUD.Instance?.ShowNotificationBanner(
                    "HOST PC STREAM CONNECTED",
                    "Live Windows 11/10 Stream & Bi-Directional Input Forwarding Active [F7: Host | F8: IMAX]",
                    new Color(0f, 1f, 0.55f, 1f));

                AudioManager.Instance?.PlayConnectChime();
                Debug.Log("[HostDesktopStreamManager] Live Windows Host Desktop Stream ACTIVATED with full input forwarding.");
            }
            else
            {
                // 1. Stop Input Forwarding
                if (_inputForwarder != null)
                {
                    _inputForwarder.SetInputForwarding(false);
                }

                // 2. Hide HostStreamDisplay to restore simulated VR applications
                if (_hostStreamDisplay != null)
                {
                    _hostStreamDisplay.gameObject.SetActive(false);
                }

                // Restore Simulated Desktop RenderTexture
                if (_centerScreen != null && _simulatedRenderTexture != null)
                {
                    _centerScreen.SetTexture(_simulatedRenderTexture);
                }

                // 3. Update IMAX Projection if sharing
                ScreenShareToImaxManager.Instance?.UpdateSharedTexture();

                // 4. Update HUD
                DesktopModeHUD.Instance?.ShowNotificationBanner(
                    "SIMULATED VR OS RESTORED",
                    "Local Workstation Applications & Cyber Environment [Press F7 for Host]",
                    new Color(0f, 0.85f, 1f, 1f));

                AudioManager.Instance?.PlaySweepPrompt();
                Debug.Log("[HostDesktopStreamManager] Simulated VR OS RESTORED.");
            }

            UpdateHUDHelp();
        }

        public void UpdateHUDHelp()
        {
            string ambilight = WorkstationAmbilightController.Instance != null 
                ? WorkstationAmbilightController.Instance.CurrentMode.ToString() 
                : "Active";
            string theme = EnvironmentThemeController.Instance != null 
                ? EnvironmentThemeController.Instance.GetThemeDisplayName(EnvironmentThemeController.Instance.CurrentTheme) 
                : "Cyberpunk";

            DesktopModeHUD.Instance?.UpdateBottomHelp(ambilight, theme);
        }
    }
}
