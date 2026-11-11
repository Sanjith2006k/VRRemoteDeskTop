using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TMPro;
using VRRemoteDesktop.UI;
using VRRemoteDesktop.UI.Windows;

namespace VRRemoteDesktop.Computer
{
    /// <summary>
    /// Master Monitor Interaction Controller fulfilling Section 5 & 6 of Master Requirements.
    /// Reusable across all three physical monitors (Left, Center, Right).
    /// Converts 6DoF VR controller raycasts (and PC mouse input) into accurate UV coordinates,
    /// drives the on-screen cursor, dispatches Unity UI pointer events (Clicks, Hovers, Drags, Scrolls),
    /// manages active monitor focus, and provides visible hover/selection highlights.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DesktopMonitorController : XRBaseInteractable
    {
        [Header("Monitor Identity")]
        [SerializeField] private ActiveDisplayIndex _displayIndex = ActiveDisplayIndex.Center;
        [SerializeField] private string _displayTitle = "Primary Remote Desktop";

        [Header("Canvas & Rendering Target")]
        [SerializeField] private Canvas _targetCanvas;
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private GraphicRaycaster _raycaster;
        [SerializeField] private Camera _uiCamera;
        [SerializeField] private string _canvasName = "DesktopCanvas";
        [SerializeField] private string _uiCameraName = "UICamera";

        [Header("Cursor Settings")]
        [SerializeField] private RectTransform _cursorRect;
        [SerializeField] private Image _cursorImage;
        [SerializeField] private Color _cursorColor = new Color(0f, 1f, 1f, 0.95f);
        [SerializeField] private float _cursorSize = 22f;

        [Header("Visual Feedback / Frame Glow")]
        [SerializeField] private Renderer _frameRenderer;
        [SerializeField] private Material _activeFrameMaterial;
        [SerializeField] private Material _inactiveFrameMaterial;
        [SerializeField] private Color _activeGlowColor = new Color(0f, 0.85f, 1f, 1f);
        [SerializeField] private Color _hoverGlowColor = new Color(0.2f, 1f, 0.9f, 0.7f);

        [Header("Simulation & Testing Fallback")]
#pragma warning disable 0414
        [SerializeField] private bool _enableMouseSimulation = true;
#pragma warning restore 0414

        // XR State
        private XRRayInteractor _currentRayInteractor;
        private bool _isHovering = false;
        private bool _isClicking = false;
        private bool _wasClickingLastFrame = false;
        private Collider _screenCollider;
        private Camera _mainCamera;

        // UI Event Dispatch State
        private GameObject _lastHovered = null;
        private GameObject _pressedObject = null;
        private GameObject _draggedObject = null;
        private bool _isDragging = false;
        private Vector2 _pressPointerPos;
        private Vector2 _lastUv = new Vector2(0.5f, 0.5f);
        private Vector2 _lastVirtualPos;
        private bool _hasVirtualPos = false;
        private bool _isActiveDisplay = false;

        public ActiveDisplayIndex DisplayIndex => _displayIndex;
        public string DisplayTitle => _displayTitle;
        public bool IsMonitorHovered => _isHovering;
        public bool IsActiveDisplay => _isActiveDisplay;
        public Vector2 CurrentUV => _lastUv;

        protected override void Awake()
        {
            base.Awake();
            _screenCollider = GetComponent<Collider>();
            EnsureEventSystem();
            ResolveReferences();
        }

        private void Start()
        {
            EnsureEventSystem();
            ResolveReferences();
            EnsureCursor();
            UpdateVisualFeedback();

            if (MultiMonitorManager.Instance != null)
            {
                MultiMonitorManager.Instance.OnActiveDisplayChanged += HandleActiveDisplayChanged;
                _isActiveDisplay = (MultiMonitorManager.Instance.ActiveDisplay == _displayIndex);
                UpdateVisualFeedback();
            }
        }

        protected override void OnDestroy()
        {
            if (MultiMonitorManager.Instance != null)
            {
                MultiMonitorManager.Instance.OnActiveDisplayChanged -= HandleActiveDisplayChanged;
            }
            base.OnDestroy();
        }

        private EventSystem EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                return esObj.GetComponent<EventSystem>();
            }
            return EventSystem.current;
        }

        public void Configure(ActiveDisplayIndex index, string title, string canvasName, string cameraName, Color cursorColor)
        {
            _displayIndex = index;
            _displayTitle = title;
            _canvasName = canvasName;
            _uiCameraName = cameraName;
            _cursorColor = cursorColor;
            ResolveReferences();
            EnsureCursor();
        }

        public void ResolveReferences()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (_targetCanvas == null && !string.IsNullOrEmpty(_canvasName))
            {
                var cObj = GameObject.Find(_canvasName);
                if (cObj != null)
                {
                    _targetCanvas = cObj.GetComponent<Canvas>();
                    _raycaster = cObj.GetComponent<GraphicRaycaster>();
                    _canvasRect = cObj.GetComponent<RectTransform>();
                }
            }
            else if (_targetCanvas != null)
            {
                if (_canvasRect == null) _canvasRect = _targetCanvas.GetComponent<RectTransform>();
                if (_raycaster == null) _raycaster = _targetCanvas.GetComponent<GraphicRaycaster>();
            }

            if (_uiCamera == null && !string.IsNullOrEmpty(_uiCameraName))
            {
                var camObj = GameObject.Find(_uiCameraName);
                if (camObj != null) _uiCamera = camObj.GetComponent<Camera>();
            }

            // Find matching monitor frame for visual glow if not set
            if (_frameRenderer == null)
            {
                Transform parent = transform.parent;
                if (parent != null)
                {
                    Transform frameTr = parent.Find("MonitorFrame") ?? parent.Find("Frame");
                    if (frameTr != null) _frameRenderer = frameTr.GetComponent<Renderer>();
                }
            }
        }

        private void EnsureCursor()
        {
            if (_cursorRect != null || _targetCanvas == null) return;

            // Check if existing cursor in canvas
            Transform existingCursor = _targetCanvas.transform.Find("VRCursor") ?? _targetCanvas.transform.Find("AuxVirtualCursor");
            if (existingCursor != null)
            {
                _cursorRect = existingCursor.GetComponent<RectTransform>();
                _cursorImage = existingCursor.GetComponent<Image>();
                return;
            }

            // Instantiate cursor UI element on target canvas
            GameObject cursorObj = new GameObject("VRCursor", typeof(RectTransform), typeof(Image));
            cursorObj.transform.SetParent(_targetCanvas.transform, false);
            _cursorRect = cursorObj.GetComponent<RectTransform>();
            _cursorRect.sizeDelta = new Vector2(_cursorSize, _cursorSize);
            _cursorRect.pivot = new Vector2(0f, 1f); // top-left cursor tip
            _cursorRect.anchorMin = new Vector2(0f, 0f);
            _cursorRect.anchorMax = new Vector2(0f, 0f);

            _cursorImage = cursorObj.GetComponent<Image>();
            _cursorImage.color = _cursorColor;
            _cursorImage.raycastTarget = false;

            cursorObj.SetActive(false);
        }

        private void HandleActiveDisplayChanged(ActiveDisplayIndex activeIndex)
        {
            _isActiveDisplay = (activeIndex == _displayIndex);
            UpdateVisualFeedback();
        }

        private void UpdateVisualFeedback()
        {
            if (_frameRenderer != null && _frameRenderer.material != null)
            {
                if (_isActiveDisplay)
                {
                    _frameRenderer.material.SetColor("_EmissionColor", _activeGlowColor * 1.5f);
                }
                else if (_isHovering)
                {
                    _frameRenderer.material.SetColor("_EmissionColor", _hoverGlowColor * 0.8f);
                }
                else
                {
                    _frameRenderer.material.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        #region XR Interaction Toolkit Overrides

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            if (args.interactorObject is XRRayInteractor rayInteractor)
            {
                _currentRayInteractor = rayInteractor;
                _isHovering = true;
                EnsureCursor();
                if (_cursorRect != null) _cursorRect.gameObject.SetActive(true);
                UpdateVisualFeedback();
            }
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            if (ReferenceEquals(args.interactorObject, _currentRayInteractor))
            {
                _currentRayInteractor = null;
                _isHovering = false;
                if (_cursorRect != null) _cursorRect.gameObject.SetActive(false);
                ClearHoverState();
                UpdateVisualFeedback();
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (ReferenceEquals(args.interactorObject, _currentRayInteractor))
            {
                _isClicking = true;

                // Focus this monitor as the active display
                if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.ActiveDisplay != _displayIndex)
                {
                    MultiMonitorManager.Instance.SetActiveDisplay(_displayIndex);
                }
            }
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            if (ReferenceEquals(args.interactorObject, _currentRayInteractor))
            {
                _isClicking = false;
            }
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (_isHovering && _currentRayInteractor != null)
                {
                    if (_currentRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                    {
                        // Calculate UV from 3D hit point relative to screen quad
                        Vector3 localPos = transform.InverseTransformPoint(hit.point);
                        float uvX = Mathf.Clamp01(localPos.x + 0.5f);
                        float uvY = Mathf.Clamp01(localPos.y + 0.5f);
                        Vector2 uv = new Vector2(uvX, uvY);
                        _lastUv = uv;

                        UpdateCursorPosition(uv);

                        bool vrDown = _isClicking && !_wasClickingLastFrame;
                        bool vrHeld = _isClicking;
                        bool vrUp = !_isClicking && _wasClickingLastFrame;
                        _wasClickingLastFrame = _isClicking;

                        ProcessUIPointerEvents(uv, vrDown, vrHeld, vrUp, Vector2.zero);
                    }
                }
            }
        }

        #endregion

        #region Desktop Mouse Simulation Fallback

        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            // Only process mouse simulation when not hovered by a physical XR controller ray
            if (_enableMouseSimulation && !_isHovering)
            {
                ProcessMouseInteraction();
            }
#endif
        }

        private void ProcessMouseInteraction()
        {
            bool inComputerMode = ComputerInteractionManager.Instance != null &&
                                  ComputerInteractionManager.Instance.IsInteractingAtDesktop;

            // In desktop mode, process mouse rays against monitor plane
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Vector2 mousePos = mouse.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mousePos);

            Plane screenPlane = new Plane(-transform.forward, transform.position);
            bool hitValid = false;
            Vector2 uv = _lastUv;

            if (screenPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 localPos = transform.InverseTransformPoint(hitPoint);

                // Check quad boundaries [-0.5, 0.5]
                if (localPos.x >= -0.505f && localPos.x <= 0.505f &&
                    localPos.y >= -0.505f && localPos.y <= 0.505f)
                {
                    uv = new Vector2(Mathf.Clamp01(localPos.x + 0.5f), Mathf.Clamp01(localPos.y + 0.5f));
                    hitValid = true;
                    _lastUv = uv;
                }
            }

            if (!hitValid)
            {
                if (_cursorRect != null && _cursorRect.gameObject.activeSelf)
                {
                    ClearHoverState();
                    _cursorRect.gameObject.SetActive(false);
                }
                return;
            }

            EnsureCursor();
            if (_cursorRect != null)
            {
                _cursorRect.gameObject.SetActive(true);
                UpdateCursorPosition(uv);
            }

            // Click focus monitor
            if (mouse.leftButton.wasPressedThisFrame && MultiMonitorManager.Instance != null &&
                MultiMonitorManager.Instance.ActiveDisplay != _displayIndex)
            {
                MultiMonitorManager.Instance.SetActiveDisplay(_displayIndex);
            }

            ProcessUIPointerEvents(uv, mouse.leftButton.wasPressedThisFrame, mouse.leftButton.isPressed, mouse.leftButton.wasReleasedThisFrame, mouse.scroll.ReadValue());
        }

        #endregion

        #region Cursor & Pointer Event Dispatching

        private void UpdateCursorPosition(Vector2 uv)
        {
            if (_cursorRect == null) return;

            float canvasW = _canvasRect != null ? _canvasRect.rect.width : 1920f;
            float canvasH = _canvasRect != null ? _canvasRect.rect.height : 1080f;

            // Position cursor anchored to bottom-left (0,0) to (canvasW, canvasH)
            _cursorRect.anchoredPosition = new Vector2(uv.x * canvasW, uv.y * canvasH);
        }

        private void ProcessUIPointerEvents(Vector2 uv, bool leftDown, bool leftHeld, bool leftUp, Vector2 scroll)
        {
            if (_targetCanvas == null || _canvasRect == null)
            {
                ResolveReferences();
                if (_canvasRect == null) return;
            }

            EventSystem eventSystem = EnsureEventSystem();

            float width = _canvasRect.rect.width > 0 ? _canvasRect.rect.width : 1920f;
            float height = _canvasRect.rect.height > 0 ? _canvasRect.rect.height : 1080f;

            Vector2 virtualPixelPos = new Vector2(uv.x * width, uv.y * height);
            Vector2 delta = _hasVirtualPos ? (virtualPixelPos - _lastVirtualPos) : Vector2.zero;
            _lastVirtualPos = virtualPixelPos;
            _hasVirtualPos = true;

            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = virtualPixelPos,
                delta = delta,
                scrollDelta = scroll,
                button = PointerEventData.InputButton.Left
            };

            GameObject topObject = FindTopUIObject(virtualPixelPos, pointerData);

            // A. Hover Handling
            if (topObject != _lastHovered)
            {
                if (_lastHovered != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_lastHovered, pointerData, ExecuteEvents.pointerExitHandler);
                    var oldIcon = _lastHovered.GetComponentInParent<DesktopIcon>();
                    if (oldIcon != null) oldIcon.OnPointerExit(pointerData);
                }

                _lastHovered = topObject;

                if (_lastHovered != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_lastHovered, pointerData, ExecuteEvents.pointerEnterHandler);
                    var newIcon = _lastHovered.GetComponentInParent<DesktopIcon>();
                    if (newIcon != null) newIcon.OnPointerEnter(pointerData);
                }
            }

            // B. Pointer Down
            if (leftDown)
            {
                _pressedObject = topObject;
                _pressPointerPos = virtualPixelPos;
                _isDragging = false;

                if (_pressedObject != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_pressedObject, pointerData, ExecuteEvents.pointerDownHandler);
                    var icon = _pressedObject.GetComponentInParent<DesktopIcon>();
                    if (icon != null) icon.OnPointerDown(pointerData);

                    // Focus input field if selected
                    var inputField = _pressedObject.GetComponentInParent<TMP_InputField>();
                    if (inputField != null)
                    {
                        inputField.ActivateInputField();
                    }
                }
            }

            // C. Pointer Drag
            if (leftHeld && _pressedObject != null)
            {
                float moveDist = Vector2.Distance(virtualPixelPos, _pressPointerPos);
                var icon = _pressedObject.GetComponentInParent<DesktopIcon>();

                if (!_isDragging && moveDist > 12f && icon == null)
                {
                    _isDragging = true;
                    _draggedObject = _pressedObject;
                    ExecuteEvents.ExecuteHierarchy(_draggedObject, pointerData, ExecuteEvents.beginDragHandler);
                }

                if (_isDragging && _draggedObject != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_draggedObject, pointerData, ExecuteEvents.dragHandler);

                    var win = _draggedObject.GetComponentInParent<DesktopWindow>();
                    if (win != null && (_draggedObject.name.Contains("Title") || _draggedObject.name.Contains("Header") || _draggedObject.GetComponentInParent<WindowTitleBarDragHandler>() != null))
                    {
                        win.Drag(delta);
                    }
                }
            }

            // D. Pointer Up & Click Execution
            if (leftUp)
            {
                if (_isDragging && _draggedObject != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_draggedObject, pointerData, ExecuteEvents.endDragHandler);
                    _isDragging = false;
                    _draggedObject = null;
                }

                GameObject clickTarget = _pressedObject ?? topObject;
                if (clickTarget != null)
                {
                    ExecuteEvents.ExecuteHierarchy(clickTarget, pointerData, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.ExecuteHierarchy(clickTarget, pointerData, ExecuteEvents.pointerClickHandler);

                    // 1. DesktopIcon (opens app windows)
                    var icon = clickTarget.GetComponentInParent<DesktopIcon>();
                    if (icon != null)
                    {
                        VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();
                        icon.OnPointerUp(pointerData);
                        icon.OnPointerClick(pointerData);

                        if (DesktopWindowManager.Instance != null)
                        {
                            DesktopWindowManager.Instance.OpenWindow(icon.AppName);
                        }
                    }

                    // 2. Buttons (Window controls, close, max, min, dialog buttons)
                    var btn = clickTarget.GetComponentInParent<Button>();
                    if (btn != null && btn.interactable)
                    {
                        VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();
                        btn.onClick.Invoke();

                        var targetWin = clickTarget.GetComponentInParent<DesktopWindow>();
                        if (targetWin != null)
                        {
                            if (btn.name.Contains("Close")) targetWin.Close();
                            else if (btn.name.Contains("Max")) targetWin.ToggleMaximize();
                            else if (btn.name.Contains("Min")) targetWin.Minimize();
                        }
                    }

                    // 3. Window focus
                    var win = clickTarget.GetComponentInParent<DesktopWindow>();
                    if (win != null)
                    {
                        win.Focus();
                    }
                }

                _pressedObject = null;
            }

            // E. Scroll Handling
            if (scroll != Vector2.zero && topObject != null)
            {
                ExecuteEvents.ExecuteHierarchy(topObject, pointerData, ExecuteEvents.scrollHandler);
            }
        }

        private GameObject FindTopUIObject(Vector2 virtualPixelPos, PointerEventData pointerData)
        {
            if (_raycaster == null || _targetCanvas == null) return null;

            List<RaycastResult> results = new List<RaycastResult>();
            _raycaster.Raycast(pointerData, results);

            foreach (var r in results)
            {
                if (r.gameObject != null && r.gameObject != _cursorRect?.gameObject && r.gameObject != gameObject)
                {
                    return r.gameObject;
                }
            }

            return null;
        }

        private void ClearHoverState()
        {
            if (_lastHovered != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                ExecuteEvents.ExecuteHierarchy(_lastHovered, pointerData, ExecuteEvents.pointerExitHandler);
                _lastHovered = null;
            }
            _pressedObject = null;
            _draggedObject = null;
            _isDragging = false;
        }

        #endregion
    }
}
