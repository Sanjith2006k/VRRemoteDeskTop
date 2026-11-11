using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Remote;
using VRRemoteDesktop.UI;

namespace VRRemoteDesktop.Input
{
    /// <summary>
    /// Master raycaster and interaction bridge for Phase 6 Virtual Desktop Interaction.
    /// Converts physical mouse movement and clicks on the 3D monitor screen into
    /// virtual UI pointer events (Hover, Click, Drag, Scroll, Right-Click) on DesktopCanvas.
    /// </summary>
    public class VirtualDesktopRaycaster : MonoBehaviour
    {
        public static VirtualDesktopRaycaster Instance { get; private set; }

        [Header("Target References")]
        [Tooltip("The screen mesh or box collider to raycast against.")]
        [SerializeField] private Collider _screenCollider;

        [Tooltip("The VRDesktopCursor or DesktopCursor component on DesktopCanvas.")]
        [SerializeField] private VRDesktopCursor _cursor;

        [Tooltip("RectTransform of the master DesktopCanvas.")]
        [SerializeField] private RectTransform _canvasRect;

        [Tooltip("GraphicRaycaster component on the DesktopCanvas.")]
        [SerializeField] private GraphicRaycaster _canvasRaycaster;

        [Tooltip("Camera used to render DesktopCanvas (UICamera).")]
        [SerializeField] private Camera _uiCamera;

        [Header("Settings")]
        [Tooltip("Lock cursor to monitor borders when moving slightly outside.")]
        [SerializeField] private bool _clampToScreen = true;

        [Tooltip("Pixel distance threshold to initiate a drag operation.")]
        [SerializeField] private float _dragThreshold = 4.0f;

        private Camera _mainCamera;
        private GameObject _hoveredObject;
        private GameObject _pressedObject;
        private GameObject _draggedObject;
        private bool _isDragging = false;
        private Vector2 _pressPointerPos;
        private Vector2 _lastUv = new Vector2(0.5f, 0.5f);
        private Vector2 _lastVirtualPixelPos;
        private bool _hasVirtualPos = false;

        public Vector2 CurrentUV => _lastUv;
        public bool IsHoveringScreen { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _screenCollider = GetComponent<Collider>();
            ResolveReferences();
        }

        private void Start()
        {
            if (_cursor != null)
            {
                _cursor.Show(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void ResolveReferences()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;

            if (_screenCollider == null) _screenCollider = GetComponent<Collider>();
            if (_screenCollider == null)
            {
                var screenObj = GameObject.Find("Screen");
                if (screenObj != null) _screenCollider = screenObj.GetComponent<Collider>();
            }

            if (_cursor == null)
            {
                _cursor = Object.FindAnyObjectByType<VRDesktopCursor>();
            }

            if (_canvasRect == null)
            {
                var canvasObj = GameObject.Find("DesktopCanvas");
                if (canvasObj != null)
                {
                    _canvasRect = canvasObj.GetComponent<RectTransform>();
                    _canvasRaycaster = canvasObj.GetComponent<GraphicRaycaster>();
                }
            }

            if (_uiCamera == null)
            {
                var uiCamObj = GameObject.Find("UICamera");
                if (uiCamObj != null) _uiCamera = uiCamObj.GetComponent<Camera>();
            }
        }

        private void Update()
        {
            // Only process desktop mouse interaction when fully seated in Computer Interaction Mode (not during transitions)
            bool inComputerMode = ComputerInteractionManager.Instance != null &&
                                  ComputerInteractionManager.Instance.IsInteractingAtDesktop;

            if (!inComputerMode)
            {
                if (_cursor != null && _cursor.gameObject.activeSelf)
                {
                    ClearHoverState();
                    _cursor.Show(false);
                }
                IsHoveringScreen = false;
                return;
            }

            if (Cursor.visible)
            {
                Cursor.visible = false;
            }

            var cim = ComputerInteractionManager.Instance;
            bool isRmbLooking = (cim != null && cim.IsRightClickLooking);
            if (isRmbLooking)
            {
                if (_cursor != null && _cursor.gameObject.activeSelf)
                {
                    ClearHoverState();
                    _cursor.Show(false);
                }
                IsHoveringScreen = false;
                return;
            }

            ProcessDesktopMouse();
        }

        private void ProcessDesktopMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null || _screenCollider == null) return;
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            Vector2 mouseScreenPos = mouse.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mouseScreenPos);

            bool hitScreen = _screenCollider.Raycast(ray, out RaycastHit hit, 100f);

            if (hitScreen)
            {
                IsHoveringScreen = true;
                _lastUv = hit.textureCoord;
            }
            else
            {
                // If dragging or clamp enabled, retain boundary UV
                if (_isDragging || _clampToScreen)
                {
                    IsHoveringScreen = false;
                }
                else
                {
                    IsHoveringScreen = false;
                    ClearHoverState();
                    if (_cursor != null) _cursor.Show(false);
                    return;
                }
            }

            Vector2 clampedUv = new Vector2(Mathf.Clamp01(_lastUv.x), Mathf.Clamp01(_lastUv.y));

            // 1. Update Visual Cursor Position
            if (_cursor != null)
            {
                _cursor.Show(true);
                _cursor.UpdateCursorPosition(clampedUv);
            }

            // 2. Dispatch UI Events to DesktopCanvas
            DispatchUIEvents(clampedUv, mouse);
        }

        private EventSystem EnsureEventSystem()
        {
            EventSystem es = EventSystem.current;
            if (es == null)
            {
                es = Object.FindAnyObjectByType<EventSystem>();
                if (es == null)
                {
                    var esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                    es = esObj.GetComponent<EventSystem>();
                }
            }
            return es;
        }

        private void DispatchUIEvents(Vector2 uv, Mouse mouse)
        {
            if (_canvasRaycaster == null || _canvasRect == null) return;

            if (HostDesktopStreamManager.Instance != null && HostDesktopStreamManager.Instance.IsHostStreamActive)
            {
                bool lDown = mouse != null && mouse.leftButton.wasPressedThisFrame;
                bool lHeld = mouse != null && mouse.leftButton.isPressed;
                bool lUp = mouse != null && mouse.leftButton.wasReleasedThisFrame;
                Vector2 sc = mouse != null ? mouse.scroll.ReadValue() : Vector2.zero;
                HostDesktopStreamManager.Instance.ForwardPointerFromInteractable(uv, lDown, lHeld, lUp, sc);
            }

            float canvasWidth = _canvasRect.rect.width > 0 ? _canvasRect.rect.width : 1920f;
            float canvasHeight = _canvasRect.rect.height > 0 ? _canvasRect.rect.height : 1080f;

            // Convert normalized UV to virtual canvas screen pixels for UICamera
            Vector2 virtualPixelPos = new Vector2(uv.x * canvasWidth, uv.y * canvasHeight);

            Vector2 virtualDelta = _hasVirtualPos ? (virtualPixelPos - _lastVirtualPixelPos) : Vector2.zero;
            _lastVirtualPixelPos = virtualPixelPos;
            _hasVirtualPos = true;

            EventSystem eventSystem = EnsureEventSystem();

            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = virtualPixelPos,
                delta = virtualDelta
            };

            List<RaycastResult> results = new List<RaycastResult>();
            _canvasRaycaster.Raycast(pointerData, results);

            // Filter out cursor itself
            GameObject topObject = null;
            foreach (var result in results)
            {
                if (_cursor != null && (result.gameObject == _cursor.gameObject || result.gameObject.transform.IsChildOf(_cursor.transform)))
                {
                    continue;
                }
                topObject = result.gameObject;
                break;
            }

            pointerData.pointerCurrentRaycast = results.Count > 0 ? results[0] : new RaycastResult();

            // --- A. Hover State Handling ---
            bool isInteractive = false;
            if (topObject != null)
            {
                var icon = topObject.GetComponentInParent<DesktopIcon>();
                var btn = topObject.GetComponentInParent<Selectable>();
                isInteractive = (icon != null || btn != null);
            }

            if (topObject != _hoveredObject)
            {
                if (_hoveredObject != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_hoveredObject, pointerData, ExecuteEvents.pointerExitHandler);
                }

                _hoveredObject = topObject;

                if (_hoveredObject != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_hoveredObject, pointerData, ExecuteEvents.pointerEnterHandler);
                }
            }

            // --- B. Left Mouse Button (Down, Drag, Up, Click) ---
            bool leftDown = mouse.leftButton.wasPressedThisFrame;
            bool leftHeld = mouse.leftButton.isPressed;
            bool leftUp = mouse.leftButton.wasReleasedThisFrame;

            if (leftDown)
            {
                _pressPointerPos = virtualPixelPos;
                _pressedObject = topObject;

                if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.ActiveDisplay != ActiveDisplayIndex.Center)
                {
                    MultiMonitorManager.Instance.SetActiveDisplay(ActiveDisplayIndex.Center);
                }

                if (_pressedObject != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_pressedObject, pointerData, ExecuteEvents.pointerDownHandler);
                }
            }

            if (leftHeld && _pressedObject != null)
            {
                if (!_isDragging && Vector2.Distance(virtualPixelPos, _pressPointerPos) > _dragThreshold)
                {
                    _isDragging = true;
                    _draggedObject = _pressedObject;
                    ExecuteEvents.ExecuteHierarchy(_draggedObject, pointerData, ExecuteEvents.beginDragHandler);
                }

                if (_isDragging && _draggedObject != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_draggedObject, pointerData, ExecuteEvents.dragHandler);
                }
            }

            if (leftUp)
            {
                if (_isDragging && _draggedObject != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_draggedObject, pointerData, ExecuteEvents.endDragHandler);
                    _isDragging = false;
                    _draggedObject = null;
                }

                if (_pressedObject != null)
                {
                    ExecuteEvents.ExecuteHierarchy(_pressedObject, pointerData, ExecuteEvents.pointerUpHandler);

                    // If released on the same object that was pressed, trigger Click!
                    if (topObject != null && (_pressedObject == topObject || topObject.transform.IsChildOf(_pressedObject.transform) || _pressedObject.transform.IsChildOf(topObject.transform)))
                    {
                        ExecuteEvents.ExecuteHierarchy(_pressedObject, pointerData, ExecuteEvents.pointerClickHandler);

                        // Fallback click on Button component
                        var btn = _pressedObject.GetComponentInParent<Button>();
                        if (btn != null && btn.interactable)
                        {
                            btn.onClick.Invoke();
                        }
                    }

                    _pressedObject = null;
                }
            }

            // --- C. Right Click Handling ---
            if (mouse.rightButton.wasPressedThisFrame && topObject != null)
            {
                PointerEventData rightData = new PointerEventData(eventSystem)
                {
                    position = virtualPixelPos,
                    button = PointerEventData.InputButton.Right
                };
                ExecuteEvents.ExecuteHierarchy(topObject, rightData, ExecuteEvents.pointerClickHandler);
            }

            // --- D. Scroll Wheel Handling ---
            Vector2 scroll = mouse.scroll.ReadValue();
            if (scroll.sqrMagnitude > 0.001f && topObject != null)
            {
                pointerData.scrollDelta = scroll;
                ExecuteEvents.ExecuteHierarchy(topObject, pointerData, ExecuteEvents.scrollHandler);
            }

            // --- E. Update Visual Cursor Feedback ---
            if (_cursor != null)
            {
                _cursor.SetState(isInteractive, leftHeld);
            }
        }

        private void ClearHoverState()
        {
            if (_hoveredObject != null)
            {
                PointerEventData pointerData = new PointerEventData(EnsureEventSystem());
                ExecuteEvents.ExecuteHierarchy(_hoveredObject, pointerData, ExecuteEvents.pointerExitHandler);
                _hoveredObject = null;
            }
            _pressedObject = null;
            _draggedObject = null;
            _isDragging = false;
            _hasVirtualPos = false;
        }
    }
}
