using System;
using UnityEngine;
using VRRemoteDesktop.Remote;
using VRRemoteDesktop.XR;

namespace VRRemoteDesktop.Computer
{
    public enum ActiveDisplayIndex
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    /// <summary>
    /// Master Triple-Monitor coordinator fulfilling Section 3 & 4 of Master Requirements.
    /// Manages the three physical screens (Left: Diagnostics, Center: Remote Desktop, Right: Operations NOC).
    /// Provides physically accurate, seamless cursor transitions between adjacent monitors:
    /// - Cursor is strictly rendered ON the physical monitor screen surfaces, never in empty room air
    /// - Continuous virtual desktop mapping across Left <-> Center <-> Right boundaries
    /// - 3D world-space height preservation across screens with zero vertical misalignment or jumps
    /// - Full UI event forwarding (Hover, Click, Drag, Scroll) to active workstation applications
    /// </summary>
    public class MultiMonitorManager : MonoBehaviour
    {
        public static MultiMonitorManager Instance { get; private set; }

        [Header("Physical Screen References")]
        [SerializeField] private RemoteDesktopScreen _centerScreen;
        [SerializeField] private RemoteDesktopScreen _leftScreen;
        [SerializeField] private RemoteDesktopScreen _rightScreen;

        [Header("Render Textures")]
        [SerializeField] private RenderTexture _centerRenderTexture;
        [SerializeField] private RenderTexture _leftRenderTexture;
        [SerializeField] private RenderTexture _rightRenderTexture;

        [Header("Display State")]
        [SerializeField] private ActiveDisplayIndex _activeDisplay = ActiveDisplayIndex.Center;
        [SerializeField] private float _globalBrightness = 1.0f;

        [Header("Workstation Extended Desktop")]
        [SerializeField] private bool _enableExtendedDesktop = true;
        [SerializeField] private float _cursorSensitivity = 1.15f;

        private Vector2 _currentUV = new Vector2(0.5f, 0.5f);
        private float _transitionCooldown = 0f;
        private VRDesktopInteractable _centerInteractable;
        private AuxiliaryScreenInteractable _leftInteractable;
        private AuxiliaryScreenInteractable _rightInteractable;

        public RemoteDesktopScreen CenterScreen => _centerScreen;
        public RemoteDesktopScreen LeftScreen => _leftScreen;
        public RemoteDesktopScreen RightScreen => _rightScreen;
        public ActiveDisplayIndex ActiveDisplay => _activeDisplay;
        public Vector2 CurrentUV => _currentUV;
        public bool IsExtendedDesktopActive => _enableExtendedDesktop;

        public RenderTexture CenterRenderTexture
        {
            get
            {
                if (_centerRenderTexture != null) return _centerRenderTexture;
                var cam = GameObject.Find("UICamera")?.GetComponent<Camera>();
                if (cam != null && cam.targetTexture != null) return cam.targetTexture;
                if (_centerScreen != null && _centerScreen.CurrentTexture is RenderTexture rt) return rt;
                return null;
            }
        }

        public Texture GetCenterScreenTexture()
        {
            if (CenterRenderTexture != null) return CenterRenderTexture;
            if (_centerScreen != null && _centerScreen.CurrentTexture != null) return _centerScreen.CurrentTexture;
            var cam = GameObject.Find("UICamera")?.GetComponent<Camera>();
            if (cam != null && cam.targetTexture != null) return cam.targetTexture;
            return null;
        }

        public event Action<ActiveDisplayIndex> OnActiveDisplayChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            Application.runInBackground = true;
            ResolveScreens();
        }

        private void Start()
        {
            BindScreensToTextures();
            EnsureInteractableReferences();
        }

        public void ResolveScreens()
        {
            if (_centerScreen == null)
            {
                var s = GameObject.Find("Screen");
                if (s != null) _centerScreen = s.GetComponent<RemoteDesktopScreen>();
            }
            if (_leftScreen == null)
            {
                var s = GameObject.Find("LeftScreen");
                if (s != null) _leftScreen = s.GetComponent<RemoteDesktopScreen>();
            }
            if (_rightScreen == null)
            {
                var s = GameObject.Find("RightScreen");
                if (s != null) _rightScreen = s.GetComponent<RemoteDesktopScreen>();
            }

            EnsureAuxiliaryScreenInteractables();
            EnsureInteractableReferences();
        }

        private void EnsureAuxiliaryScreenInteractables()
        {
            if (_leftScreen != null && _leftScreen.GetComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>() == null)
            {
                var aux = _leftScreen.gameObject.AddComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>();
                aux.Configure("LeftDesktopCanvas", "UILeftCamera", new Color(0f, 1f, 1f, 0.95f));
            }
            if (_rightScreen != null && _rightScreen.GetComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>() == null)
            {
                var aux = _rightScreen.gameObject.AddComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>();
                aux.Configure("RightDesktopCanvas", "UIRightCamera", new Color(0.85f, 0.4f, 1f, 0.95f));
            }

            // Ensure Left and Right Dashboards
            var leftCanvasObj = GameObject.Find("LeftDesktopCanvas");
            if (leftCanvasObj != null && leftCanvasObj.GetComponent<VRRemoteDesktop.UI.LeftDashboardUI>() == null)
            {
                leftCanvasObj.AddComponent<VRRemoteDesktop.UI.LeftDashboardUI>();
            }

            var rightCanvasObj = GameObject.Find("RightDesktopCanvas");
            if (rightCanvasObj != null && rightCanvasObj.GetComponent<VRRemoteDesktop.UI.RightDashboardUI>() == null)
            {
                rightCanvasObj.AddComponent<VRRemoteDesktop.UI.RightDashboardUI>();
            }
        }

        public void BindScreensToTextures()
        {
            if (_centerScreen != null && _centerRenderTexture != null)
            {
                _centerScreen.SetTexture(_centerRenderTexture);
            }
            if (_leftScreen != null && _leftRenderTexture != null)
            {
                _leftScreen.SetTexture(_leftRenderTexture);
            }
            if (_rightScreen != null && _rightRenderTexture != null)
            {
                _rightScreen.SetTexture(_rightRenderTexture);
            }
        }

        public void SetActiveDisplay(ActiveDisplayIndex display)
        {
            if (_activeDisplay != display)
            {
                // Preserving exact 3D physical world height across screens during manual switch
                Vector3 worldPt = Vector3.zero;
                if (_activeDisplay == ActiveDisplayIndex.Center && _centerScreen != null)
                    worldPt = _centerScreen.transform.TransformPoint(new Vector3(_currentUV.x - 0.5f, _currentUV.y - 0.5f, 0f));
                else if (_activeDisplay == ActiveDisplayIndex.Left && _leftScreen != null)
                    worldPt = _leftScreen.transform.TransformPoint(new Vector3(_currentUV.x - 0.5f, _currentUV.y - 0.5f, 0f));
                else if (_activeDisplay == ActiveDisplayIndex.Right && _rightScreen != null)
                    worldPt = _rightScreen.transform.TransformPoint(new Vector3(_currentUV.x - 0.5f, _currentUV.y - 0.5f, 0f));

                _activeDisplay = display;

                Transform targetTransform = display == ActiveDisplayIndex.Center ? _centerScreen?.transform :
                                            (display == ActiveDisplayIndex.Left ? _leftScreen?.transform : _rightScreen?.transform);
                if (targetTransform != null && worldPt != Vector3.zero)
                {
                    Vector3 localPt = targetTransform.InverseTransformPoint(worldPt);
                    _currentUV = new Vector2(0.5f, Mathf.Clamp01(localPt.y + 0.5f));
                }
                else
                {
                    _currentUV = new Vector2(0.5f, 0.5f);
                }
            }

            // Highlight active display with slight brightness accentuation
            if (_centerScreen != null) _centerScreen.SetBrightness(display == ActiveDisplayIndex.Center ? _globalBrightness : _globalBrightness * 0.78f);
            if (_leftScreen != null) _leftScreen.SetBrightness(display == ActiveDisplayIndex.Left ? _globalBrightness : _globalBrightness * 0.78f);
            if (_rightScreen != null) _rightScreen.SetBrightness(display == ActiveDisplayIndex.Right ? _globalBrightness : _globalBrightness * 0.78f);

            OnActiveDisplayChanged?.Invoke(_activeDisplay);
            Debug.Log($"[MultiMonitorManager] Active Display focused: {_activeDisplay}");
        }

        public void SetGlobalBrightness(float brightness)
        {
            _globalBrightness = Mathf.Clamp(brightness, 0.2f, 2.0f);
            if (_centerScreen != null) _centerScreen.SetBrightness(_globalBrightness);
            if (_leftScreen != null) _leftScreen.SetBrightness(_globalBrightness);
            if (_rightScreen != null) _rightScreen.SetBrightness(_globalBrightness);
        }

        #region Extended Desktop Multi-Monitor Cursor Coordinator

        private void Update()
        {
            if (!_enableExtendedDesktop) return;

            var cim = ComputerInteractionManager.Instance;
            bool inComputerMode = cim != null && cim.IsInteractingAtDesktop;

            if (!inComputerMode)
            {
                HideAllScreenCursors();
                return;
            }

            // Keep operating system mouse hidden & locked to prevent window border confinement traps
            if (Cursor.visible) Cursor.visible = false;
            if (Cursor.lockState != CursorLockMode.Locked) Cursor.lockState = CursorLockMode.Locked;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;

            // While RMB free-look is active, preserve on-screen cursor position but do not move it or dispatch clicks
            bool isRmbLooking = (cim != null && cim.IsRightClickLooking) || mouse.rightButton.isPressed;
            try
            {
                if (!isRmbLooking && UnityEngine.Input.GetMouseButton(1)) isRmbLooking = true;
            }
            catch {}

            if (isRmbLooking)
            {
                RenderActiveCursor(false, false);
                return;
            }

            if (_transitionCooldown > 0f) _transitionCooldown -= Time.unscaledDeltaTime;

            // Read physical hardware mouse movement delta
            Vector2 delta = mouse.delta.ReadValue();
            if (float.IsNaN(delta.x) || float.IsNaN(delta.y)) delta = Vector2.zero;

            // Human flick sanity cap (1200px/frame) so user input is NEVER sluggish or stuck
            float maxDelta = 1200f;
            delta.x = Mathf.Clamp(delta.x, -maxDelta, maxDelta);
            delta.y = Mathf.Clamp(delta.y, -maxDelta, maxDelta);

            bool leftDown = mouse.leftButton.wasPressedThisFrame;
            bool leftHeld = mouse.leftButton.isPressed;
            bool leftUp = mouse.leftButton.wasReleasedThisFrame;
            try
            {
                if (!leftDown && UnityEngine.Input.GetMouseButtonDown(0)) leftDown = true;
                if (!leftHeld && UnityEngine.Input.GetMouseButton(0)) leftHeld = true;
                if (!leftUp && UnityEngine.Input.GetMouseButtonUp(0)) leftUp = true;
            }
            catch {}

            Vector2 scroll = mouse.scroll.ReadValue();

            // Convert delta into normalized UV increments based on standard 1080p display resolution
            float du = (delta.x / 1920f) * _cursorSensitivity;
            float dv = (delta.y / 1080f) * _cursorSensitivity;

            Vector2 targetUV = _currentUV + new Vector2(du, dv);

            // Seamless boundary crossing between monitors preserving exact 3D physical world height
            _currentUV = ProcessDisplayTransition(_activeDisplay, targetUV, out ActiveDisplayIndex newDisplay);

            if (newDisplay != _activeDisplay)
            {
                _activeDisplay = newDisplay;

                // Adjust monitor brightness highlights
                if (_centerScreen != null) _centerScreen.SetBrightness(_activeDisplay == ActiveDisplayIndex.Center ? _globalBrightness : _globalBrightness * 0.78f);
                if (_leftScreen != null) _leftScreen.SetBrightness(_activeDisplay == ActiveDisplayIndex.Left ? _globalBrightness : _globalBrightness * 0.78f);
                if (_rightScreen != null) _rightScreen.SetBrightness(_activeDisplay == ActiveDisplayIndex.Right ? _globalBrightness : _globalBrightness * 0.78f);

                OnActiveDisplayChanged?.Invoke(_activeDisplay);
                Debug.Log($"[MultiMonitorManager] Seamless transition to display: {_activeDisplay}");
            }

            bool isInteractive = IsCurrentHoveringInteractive();
            RenderActiveCursor(isInteractive, leftHeld);
            DispatchActivePointerEvents(_currentUV, leftDown, leftHeld, leftUp, scroll);
        }

        /// <summary>
        /// Handles smooth transition across monitor boundaries:
        /// Left right edge (u >= 1) -> Center left edge (u = 0 + overflow)
        /// Center left edge (u <= 0) -> Left right edge (u = 1 - underflow)
        /// Center right edge (u >= 1) -> Right left edge (u = 0 + overflow)
        /// Right left edge (u <= 0) -> Center right edge (u = 1 - underflow)
        /// Vertical coordinate v is mapped using 3D world height to ensure millimeter physical alignment.
        /// </summary>
        private Vector2 ProcessDisplayTransition(ActiveDisplayIndex currentDisplay, Vector2 targetUV, out ActiveDisplayIndex newDisplay)
        {
            newDisplay = currentDisplay;
            Vector2 uv = targetUV;

            // During transition cooldown, anchor cursor strictly inside the current display to prevent boundary bounce
            if (_transitionCooldown > 0f)
            {
                uv.x = Mathf.Clamp01(uv.x);
                uv.y = Mathf.Clamp01(uv.y);
                return uv;
            }

            if (currentDisplay == ActiveDisplayIndex.Left)
            {
                if (uv.x < 0f)
                {
                    uv.x = 0f; // Clamp to left outer border
                }
                else if (uv.x > 1.02f && _centerScreen != null && _leftScreen != null)
                {
                    // Physical 3D world height mapping from LeftScreen right edge to CenterScreen
                    Vector3 worldPt = _leftScreen.transform.TransformPoint(new Vector3(0.5f, uv.y - 0.5f, 0f));
                    Vector3 centerLocal = _centerScreen.transform.InverseTransformPoint(worldPt);
                    float mappedV = Mathf.Clamp01(centerLocal.y + 0.5f);

                    newDisplay = ActiveDisplayIndex.Center;
                    _transitionCooldown = 0.20f;
                    // Land securely inside CenterScreen (0.06f) to prevent boundary bounce
                    uv = new Vector2(0.06f, mappedV);
                }
            }
            else if (currentDisplay == ActiveDisplayIndex.Center)
            {
                if (uv.x < -0.03f && _leftScreen != null && _centerScreen != null)
                {
                    // Physical 3D world height mapping from CenterScreen left edge to LeftScreen
                    Vector3 worldPt = _centerScreen.transform.TransformPoint(new Vector3(-0.5f, uv.y - 0.5f, 0f));
                    Vector3 leftLocal = _leftScreen.transform.InverseTransformPoint(worldPt);
                    float mappedV = Mathf.Clamp01(leftLocal.y + 0.5f);

                    newDisplay = ActiveDisplayIndex.Left;
                    _transitionCooldown = 0.20f;
                    // Land securely inside LeftScreen (0.94f) to prevent boundary bounce
                    uv = new Vector2(0.94f, mappedV);
                }
                else if (uv.x > 1.03f && _rightScreen != null && _centerScreen != null)
                {
                    // Physical 3D world height mapping from CenterScreen right edge to RightScreen
                    Vector3 worldPt = _centerScreen.transform.TransformPoint(new Vector3(0.5f, uv.y - 0.5f, 0f));
                    Vector3 rightLocal = _rightScreen.transform.InverseTransformPoint(worldPt);
                    float mappedV = Mathf.Clamp01(rightLocal.y + 0.5f);

                    newDisplay = ActiveDisplayIndex.Right;
                    _transitionCooldown = 0.20f;
                    // Land securely inside RightScreen (0.06f) to prevent boundary bounce
                    uv = new Vector2(0.06f, mappedV);
                }
            }
            else if (currentDisplay == ActiveDisplayIndex.Right)
            {
                if (uv.x > 1f)
                {
                    uv.x = 1f; // Clamp to right outer border
                }
                else if (uv.x < -0.02f && _centerScreen != null && _rightScreen != null)
                {
                    // Physical 3D world height mapping from RightScreen left edge to CenterScreen
                    Vector3 worldPt = _rightScreen.transform.TransformPoint(new Vector3(-0.5f, uv.y - 0.5f, 0f));
                    Vector3 centerLocal = _centerScreen.transform.InverseTransformPoint(worldPt);
                    float mappedV = Mathf.Clamp01(centerLocal.y + 0.5f);

                    newDisplay = ActiveDisplayIndex.Center;
                    _transitionCooldown = 0.20f;
                    // Land securely inside CenterScreen (0.94f) to prevent boundary bounce
                    uv = new Vector2(0.94f, mappedV);
                }
            }

            uv.y = Mathf.Clamp01(uv.y);
            return uv;
        }

        private void RenderActiveCursor(bool isHovering, bool isClicking)
        {
            EnsureInteractableReferences();

            if (_activeDisplay == ActiveDisplayIndex.Center)
            {
                _centerInteractable?.UpdateCursorFromManager(_currentUV, isHovering, isClicking);
                _leftInteractable?.HideCursorFromManager();
                _rightInteractable?.HideCursorFromManager();
            }
            else if (_activeDisplay == ActiveDisplayIndex.Left)
            {
                _leftInteractable?.UpdateCursorFromManager(_currentUV, isHovering, isClicking);
                _centerInteractable?.HideCursorFromManager();
                _rightInteractable?.HideCursorFromManager();
            }
            else if (_activeDisplay == ActiveDisplayIndex.Right)
            {
                _rightInteractable?.UpdateCursorFromManager(_currentUV, isHovering, isClicking);
                _centerInteractable?.HideCursorFromManager();
                _leftInteractable?.HideCursorFromManager();
            }
        }

        private void DispatchActivePointerEvents(Vector2 uv, bool leftDown, bool leftHeld, bool leftUp, Vector2 scroll)
        {
            EnsureInteractableReferences();

            if (_activeDisplay == ActiveDisplayIndex.Center)
            {
                _centerInteractable?.ProcessPointerFromManager(uv, leftDown, leftHeld, leftUp, scroll);
            }
            else if (_activeDisplay == ActiveDisplayIndex.Left)
            {
                _leftInteractable?.ProcessPointerFromManager(uv, leftDown, leftHeld, leftUp, scroll);
            }
            else if (_activeDisplay == ActiveDisplayIndex.Right)
            {
                _rightInteractable?.ProcessPointerFromManager(uv, leftDown, leftHeld, leftUp, scroll);
            }
        }

        private bool IsCurrentHoveringInteractive()
        {
            EnsureInteractableReferences();

            if (_activeDisplay == ActiveDisplayIndex.Center)
                return _centerInteractable != null && _centerInteractable.IsHoveringInteractive;
            if (_activeDisplay == ActiveDisplayIndex.Left)
                return _leftInteractable != null && _leftInteractable.IsHoveringInteractive;
            if (_activeDisplay == ActiveDisplayIndex.Right)
                return _rightInteractable != null && _rightInteractable.IsHoveringInteractive;
            return false;
        }

        public void HideAllScreenCursors()
        {
            EnsureInteractableReferences();
            _centerInteractable?.HideCursorFromManager();
            _leftInteractable?.HideCursorFromManager();
            _rightInteractable?.HideCursorFromManager();
        }

        private void EnsureInteractableReferences()
        {
            if (_centerInteractable == null && _centerScreen != null)
                _centerInteractable = _centerScreen.GetComponent<VRDesktopInteractable>();
            if (_leftInteractable == null && _leftScreen != null)
                _leftInteractable = _leftScreen.GetComponent<AuxiliaryScreenInteractable>();
            if (_rightInteractable == null && _rightScreen != null)
                _rightInteractable = _rightScreen.GetComponent<AuxiliaryScreenInteractable>();
        }

        #endregion
    }
}
