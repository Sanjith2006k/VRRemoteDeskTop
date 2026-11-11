using System.Collections.Generic;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.InputSystem;

using UnityEngine.UI;

using UnityEngine.XR.Interaction.Toolkit;

using TMPro;

using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Remote;

using VRRemoteDesktop.Input;

using VRRemoteDesktop.UI;

using VRRemoteDesktop.UI.Windows;



namespace VRRemoteDesktop.XR



{

    /// <summary>

    /// Master VR & PC Desktop Interaction controller attached to the virtual screen.

    /// Provides ultra-smooth plane-projected cursor tracking (zero dropped frames/lag)

    /// and dispatches full Unity UI pointer events (Clicks, Hovers, Drags, Scrolls)

    /// to interactive DesktopIcons, Windows, and Buttons on DesktopCanvas.

    /// Includes resilient geometric hit testing and automatic EventSystem management.

    /// </summary>



    [RequireComponent(typeof(Collider))]

    public class VRDesktopInteractable : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable



    {

        public static VRDesktopInteractable Instance { get; private set; }



        public bool IsHoveringInteractive => _lastHovered != null || _isDragging;



        public bool IsMouseOverScreen { get; private set; }



        [Header("Cursor & UI References")]

        [SerializeField] private VRDesktopCursor _cursor;



        [SerializeField] private RectTransform _canvasRect;



        [SerializeField] private Canvas _canvas;



        [SerializeField] private GraphicRaycaster _canvasRaycaster;



        [SerializeField] private Camera _mainCamera;



        [Header("Editor Debug")]

        [Tooltip("Allows using the PC mouse to interact with the virtual screen")]

        [SerializeField] private bool _enableMouseSimulation = true;



        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor _currentRayInteractor;



        private bool _isHovering = false;



        private bool _isClicking = false;



        private bool _wasClickingLastFrame = false;



        private Collider _collider;



        // Interaction state tracking



        private GameObject _lastHovered = null;



        private GameObject _pressedObject = null;



        private GameObject _draggedObject = null;



        private bool _isDragging = false;



        private Vector2 _pressPointerPos;



        private Vector2 _lastUv = new Vector2(0.5f, 0.5f);



        private Vector2 _lastVirtualPos;



        private bool _hasVirtualPos = false;



        protected override void OnDisable()



        {

            IsMouseOverScreen = false;



            base.OnDisable();

        }



        protected override void Awake()



        {

            base.Awake();



            if (Instance != null && Instance != this)



            {

                Destroy(this);



                return;

            }



            Instance = this;



            _collider = GetComponent<Collider>();



            EnsureEventSystem();



            ResolveReferences();

        }



        private void Start()



        {

            EnsureEventSystem();



            ResolveReferences();

        }



        private EventSystem EnsureEventSystem()



        {

            VRRemoteDesktop.Environment.TheaterCinemaController.EnsureEventSystem();



            return EventSystem.current;

        }
        private Camera GetPlayerCamera()
        {
            if (_mainCamera != null && _mainCamera.gameObject.activeInHierarchy) return _mainCamera;

            if (ComputerInteractionManager.Instance != null && ComputerInteractionManager.Instance.PlayerCamera != null)
            {
                _mainCamera = ComputerInteractionManager.Instance.PlayerCamera;
                return _mainCamera;
            }

            _mainCamera = Camera.main;
            if (_mainCamera != null) return _mainCamera;

            var allCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
            foreach (var cam in allCams)
            {
                if (cam != null && cam.gameObject.activeInHierarchy &&
                    !cam.name.Contains("UI") && cam.targetTexture == null)
                {
                    _mainCamera = cam;
                    try { _mainCamera.tag = "MainCamera"; } catch {}
                    return _mainCamera;
                }
            }

            return null;
        }

        private void ResolveReferences()



        {

            if (_mainCamera == null) _mainCamera = Camera.main;



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



                    _canvas = canvasObj.GetComponent<Canvas>();



                    _canvasRaycaster = canvasObj.GetComponent<GraphicRaycaster>();

                }

            }



            else



            {

                if (_canvas == null) _canvas = _canvasRect.GetComponent<Canvas>();



                if (_canvasRaycaster == null) _canvasRaycaster = _canvasRect.GetComponent<GraphicRaycaster>();

            }

        }



        public void SetCursor(VRDesktopCursor cursor)
        {
            _cursor = cursor;
        }



        protected override void OnHoverEntered(HoverEnterEventArgs args)



        {

            base.OnHoverEntered(args);



            if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor)



            {

                _currentRayInteractor = rayInteractor;



                _isHovering = true;



                if (_cursor != null)



                {

                    _cursor.Show(true);



                    _cursor.SetState(_isHovering, _isClicking);

                }

            }

        }



        protected override void OnHoverExited(HoverExitEventArgs args)



        {

            base.OnHoverExited(args);



            if (ReferenceEquals(args.interactorObject, _currentRayInteractor))



            {

                _currentRayInteractor = null;



                _isHovering = false;



                if (_cursor != null)



                {

                    _cursor.Show(false);

                }



                ClearHoverState();

            }

        }



        protected override void OnSelectEntered(SelectEnterEventArgs args)



        {

            base.OnSelectEntered(args);



            if (ReferenceEquals(args.interactorObject, _currentRayInteractor))



            {

                _isClicking = true;



                if (_cursor != null)



                {

                    _cursor.SetState(_isHovering, _isClicking);

                }



                if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.ActiveDisplay != ActiveDisplayIndex.Center)



                {

                    MultiMonitorManager.Instance.SetActiveDisplay(ActiveDisplayIndex.Center);

                }

            }

        }



        protected override void OnSelectExited(SelectExitEventArgs args)



        {

            base.OnSelectExited(args);



            if (ReferenceEquals(args.interactorObject, _currentRayInteractor))



            {

                _isClicking = false;



                if (_cursor != null)



                {

                    _cursor.SetState(_isHovering, _isClicking);

                }

            }

        }



        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)



        {

            base.ProcessInteractable(updatePhase);



            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)



            {

                if (_isHovering && _currentRayInteractor != null && _cursor != null)



                {

                    if (_currentRayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))



                    {

                        Vector3 localPos = transform.InverseTransformPoint(hit.point);



                        float uvX = Mathf.Clamp01(localPos.x + 0.5f);



                        float uvY = Mathf.Clamp01(localPos.y + 0.5f);



                        Vector2 uv = new Vector2(uvX, uvY);



                        _lastUv = uv;



                        _cursor.UpdateCursorPosition(uv);



                        bool vrDown = _isClicking && !_wasClickingLastFrame;



                        bool vrHeld = _isClicking;



                        bool vrUp = !_isClicking && _wasClickingLastFrame;



                        _wasClickingLastFrame = _isClicking;



                        ProcessUIPointerEvents(uv, vrDown, vrHeld, vrUp, Vector2.zero);
                        if (HostDesktopStreamManager.Instance != null && HostDesktopStreamManager.Instance.IsHostStreamActive)
                        {
                            HostDesktopStreamManager.Instance.ForwardPointerFromInteractable(uv, vrDown, vrHeld, vrUp, Vector2.zero);
                        }

                    }

                }

            }

        }



        private void Update()



        {

#if UNITY_EDITOR || UNITY_STANDALONE



            ProcessMouseInteraction();



#endif

        }



        private void ProcessMouseInteraction()



        {

            if (!_enableMouseSimulation) return;



            // Only track desktop cursor when fully seated in Computer Interaction Mode (or XR hover)



            bool inComputerMode = ComputerInteractionManager.Instance != null &&
                                  ComputerInteractionManager.Instance.IsInteractingAtDesktop;

            if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.IsExtendedDesktopActive && inComputerMode)
            {
                // MultiMonitorManager coordinates extended virtual desktop across all three screens
                return;
            }



            if (!inComputerMode && !_isHovering)



            {

                IsMouseOverScreen = false;



                if (_cursor != null && _cursor.gameObject.activeSelf)



                {

                    ClearHoverState();



                    _cursor.Show(false);

                }



                return;

            }



            // Hide the PC operating system cursor while in computer mode



            if (inComputerMode && Cursor.visible)



            {

                Cursor.visible = false;

            }



            var mouse = Mouse.current;



            if (mouse == null) return;



            Camera cam = GetPlayerCamera();
            if (cam == null) return;



            // When player is right-click looking around the room, bypass screen hover and click dispatch



            bool isRmbLooking = (ComputerInteractionManager.Instance != null && ComputerInteractionManager.Instance.IsRightClickLooking) ||



                                mouse.rightButton.isPressed;



            try



            {

                if (!isRmbLooking && UnityEngine.Input.GetMouseButton(1)) isRmbLooking = true;

            }



            catch {}



            if (isRmbLooking)



            {

                IsMouseOverScreen = false;



                if (_cursor != null && _cursor.gameObject.activeSelf)



                {

                    ClearHoverState();



                    _cursor.Show(false);

                }



                return;

            }



            Vector2 mousePos = mouse.position.ReadValue();



            Ray ray = cam.ScreenPointToRay(mousePos);



            if (_collider == null) _collider = GetComponent<Collider>();



            Vector2 uv = _lastUv;



            bool hitValid = false;



            // 1. Direct screen collider raycast



            if (_collider != null && _collider.Raycast(ray, out RaycastHit hit, 50f))



            {

                uv = hit.textureCoord;



                hitValid = true;



                _lastUv = uv;

            }



            // 2. High-precision plane fallback with strict boundaries



            else



            {

                Plane screenPlane = new Plane(-transform.forward, transform.position);



                if (screenPlane.Raycast(ray, out float enter))



                {

                    Vector3 hitPoint = ray.GetPoint(enter);



                    Vector3 localPos = transform.InverseTransformPoint(hitPoint);



                    // Quad local extents are [-0.5, 0.5]. Bounded to avoid leaking to adjacent monitors



                    if (localPos.x >= -0.515f && localPos.x <= 0.515f &&



                        localPos.y >= -0.58f && localPos.y <= 0.515f)



                    {

                        uv = new Vector2(Mathf.Clamp01(localPos.x + 0.5f), Mathf.Clamp01(localPos.y + 0.5f));



                        hitValid = true;



                        _lastUv = uv;

                    }

                }

            }



            // Screen-space HUD buttons do not block workstation monitor raycast



            if (!hitValid)



            {

                IsMouseOverScreen = false;



                if (_cursor != null && _cursor.gameObject.activeSelf)



                {

                    _cursor.Show(false);

                }



                ClearHoverState();



                return;

            }



            IsMouseOverScreen = true;

            if (_cursor == null) _cursor = Object.FindAnyObjectByType<VRDesktopCursor>();
            if (_cursor != null)
            {
                if (!_cursor.gameObject.activeSelf) _cursor.Show(true);
                _cursor.UpdateCursorPosition(uv);
                _cursor.transform.SetAsLastSibling();
            }



            // 3. Dispatch Full Unity UI Events (Clicks, Hovers, Drags, Scrolls)



            bool leftDown = mouse.leftButton.wasPressedThisFrame;



            bool leftHeld = mouse.leftButton.isPressed;



            bool leftUp = mouse.leftButton.wasReleasedThisFrame;



            try



            {

                if (!leftDown && UnityEngine.Input.GetMouseButtonDown(0)) leftDown = true;



                if (!leftUp && UnityEngine.Input.GetMouseButtonUp(0)) leftUp = true;



                if (!leftHeld && UnityEngine.Input.GetMouseButton(0)) leftHeld = true;

            }



            catch {}



            if (leftDown)
            {
                if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.ActiveDisplay != ActiveDisplayIndex.Center)
                {
                    MultiMonitorManager.Instance.SetActiveDisplay(ActiveDisplayIndex.Center);
                }
                // SlideToMonitor removed so mouse clicks do not move chair
            }



            Vector2 scroll = mouse.scroll.ReadValue();



            ProcessUIPointerEvents(uv, leftDown, leftHeld, leftUp, scroll);

        }



        private void ProcessUIPointerEvents(Vector2 uv, bool leftDown, bool leftHeld, bool leftUp, Vector2 scroll)



        {

            if (_canvasRect == null)



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



                button = PointerEventData.InputButton.Left

            };



            // Dual Hit-Testing: GraphicRaycaster + Geometric RectTransform fallback



            GameObject topObject = FindTopUIObject(virtualPixelPos, pointerData);



            // A. Hover Handling



            bool isInteractive = false;



            if (topObject != null)



            {

                var icon = topObject.GetComponentInParent<DesktopIcon>();



                var btn = topObject.GetComponentInParent<Button>();



                var sel = topObject.GetComponentInParent<Selectable>();



                var win = topObject.GetComponentInParent<DesktopWindow>();



                isInteractive = (icon != null || btn != null || sel != null || win != null);

            }



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



            // B. Mouse Down



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

                }

            }



            // C. Mouse Dragging (for window moving, sliders, etc.)



            if (leftHeld && _pressedObject != null)



            {

                float moveDist = Vector2.Distance(virtualPixelPos, _pressPointerPos);



                var icon = _pressedObject.GetComponentInParent<DesktopIcon>();



                // Icons do not drag; only drag if moved > 16px and not an icon



                if (!_isDragging && moveDist > 16f && icon == null)



                {

                    _isDragging = true;



                    _draggedObject = _pressedObject;



                    ExecuteEvents.ExecuteHierarchy(_draggedObject, pointerData, ExecuteEvents.beginDragHandler);

                }



                if (_isDragging && _draggedObject != null)



                {

                    ExecuteEvents.ExecuteHierarchy(_draggedObject, pointerData, ExecuteEvents.dragHandler);



                    var win = _draggedObject.GetComponentInParent<DesktopWindow>();



                    if (win != null && (_draggedObject.name.Contains("Title") || _draggedObject.GetComponentInParent<WindowTitleBarDragHandler>() != null))



                    {

                        win.Drag(delta);

                    }

                }

            }



            // D. Mouse Up & Click Execution



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



                        Debug.Log($"[VRDesktopInteractable] Activated DesktopIcon: '{icon.AppName}'");



                        icon.OnPointerUp(pointerData);



                        icon.OnPointerClick(pointerData);



                        string appName = icon.AppName;



                        if (string.IsNullOrEmpty(appName) || appName == "App")



                        {

                            appName = icon.gameObject.name.Replace("Icon_", "").Replace("Dock_", "").Trim();

                        }



                        if (DesktopUIManager.Instance != null)



                        {

                            DesktopUIManager.Instance.LaunchApp(appName);

                        }



                        else



                        {

                            var winMgr = DesktopWindowManager.Instance ?? Object.FindAnyObjectByType<DesktopWindowManager>();



                            if (winMgr != null) winMgr.OpenWindow(appName);

                        }

                    }



                    else



                    {

                        // Direct app launch fallback if clicked directly on an icon container or dock item



                        Transform curr = clickTarget.transform;



                        while (curr != null && curr != _canvasRect)



                        {

                            if (curr.name.StartsWith("Icon_") || curr.name.StartsWith("Dock_"))



                            {

                                string appName = curr.name.Replace("Icon_", "").Replace("Dock_", "").Trim();



                                VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();



                                Debug.Log($"[VRDesktopInteractable] Directly activated app from name: '{appName}'");



                                var winMgr = DesktopWindowManager.Instance ?? Object.FindAnyObjectByType<DesktopWindowManager>();



                                if (winMgr != null) winMgr.OpenWindow(appName);



                                break;

                            }



                            curr = curr.parent;

                        }

                    }



                    // 2. Button



                    var btn = clickTarget.GetComponentInParent<Button>();



                    if (btn != null && btn.interactable)



                    {

                        VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();



                        Debug.Log($"[VRDesktopInteractable] Activated Button: {btn.name}");



                        btn.onClick.Invoke();



                        // Guaranteed direct window controls fallback



                        var targetWin = clickTarget.GetComponentInParent<DesktopWindow>();



                        if (targetWin != null)



                        {

                            if (btn.name.Contains("Close"))



                            {

                                Debug.Log($"[VRDesktopInteractable] Closing window: {targetWin.AppName}");



                                targetWin.Close();

                            }



                            else if (btn.name.Contains("Max"))



                            {

                                Debug.Log($"[VRDesktopInteractable] Toggling maximize window: {targetWin.AppName}");



                                targetWin.ToggleMaximize();

                            }



                            else if (btn.name.Contains("Min"))



                            {

                                Debug.Log($"[VRDesktopInteractable] Minimizing window: {targetWin.AppName}");



                                targetWin.Minimize();

                            }

                        }



                        // Terminal Execute button direct trigger fallback



                        if (btn.name.Contains("RunBtn") || btn.name.Contains("Execute"))



                        {

                            var term = clickTarget.GetComponentInParent<VRRemoteDesktop.UI.Windows.TerminalApp>();



                            if (term != null)



                            {

                                term.SubmitCurrentCommand();

                            }

                        }

                    }



                    else



                    {

                        // 3. DesktopWindow (focus to front)



                        var win = clickTarget.GetComponentInParent<DesktopWindow>();



                        if (win != null)



                        {

                            win.Focus();

                        }

                    }



                    // 4. TMP_InputField



                    var inputField = clickTarget.GetComponentInParent<TMP_InputField>();



                    if (inputField != null)



                    {

                        inputField.ActivateInputField();



                        if (EventSystem.current != null)



                        {

                            EventSystem.current.SetSelectedGameObject(inputField.gameObject);

                        }



                        if (VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance != null)



                        {

                            VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance.Open(inputField);

                        }

                    }



                    else if (clickTarget.name.Contains("InputBar") || clickTarget.name.Contains("Prompt"))



                    {

                        var term = clickTarget.GetComponentInParent<VRRemoteDesktop.UI.Windows.TerminalApp>();



                        if (term != null)



                        {

                            term.FocusInputField();

                        }

                    }

                }



                _pressedObject = null;

            }



            // E. Right-click is reserved strictly for seated 3D head/camera free look



            // F. Scroll Wheel



            if (scroll.sqrMagnitude > 0.001f && topObject != null)



            {

                pointerData.scrollDelta = scroll;



                ExecuteEvents.ExecuteHierarchy(topObject, pointerData, ExecuteEvents.scrollHandler);

            }



            // G. Update Visual Cursor Feedback



            if (_cursor != null)



            {

                _cursor.SetState(isInteractive, leftHeld);



                if (_isDragging) _cursor.SetDragState(true);

            }

        }



        /// <summary>

        /// Combines GraphicRaycaster with a geometric RectTransform hierarchy check

        /// to guarantee 100% reliable hit testing across icons, buttons, and windows.

        /// </summary>



        private GameObject FindTopUIObject(Vector2 virtualPixelPos, PointerEventData pointerData)



        {

            if (_canvasRect == null)



            {

                ResolveReferences();



                if (_canvasRect == null) return null;

            }



            // 1. Primary: Unity GraphicRaycaster



            if (_canvasRaycaster != null && pointerData != null)



            {

                List<RaycastResult> results = new List<RaycastResult>();



                _canvasRaycaster.Raycast(pointerData, results);



                for (int i = 0; i < results.Count; i++)



                {

                    GameObject go = results[i].gameObject;



                    if (_cursor != null && (go == _cursor.gameObject || go.transform.IsChildOf(_cursor.transform)))



                        continue;



                    // Ignore canvas background wallpaper so interactive elements underneath or geometric fallback can be chosen



                    if (go != null && (go.name.Equals("Background", System.StringComparison.OrdinalIgnoreCase) || go.name.Equals("DesktopCanvas", System.StringComparison.OrdinalIgnoreCase)))



                        continue;



                    if (go != null) return go;

                }

            }



            // 2. High-Precision Geometric Hit-Testing



            float width = _canvasRect.rect.width > 0 ? _canvasRect.rect.width : 1920f;



            float height = _canvasRect.rect.height > 0 ? _canvasRect.rect.height : 1080f;



            float normX = Mathf.Clamp01(virtualPixelPos.x / width);



            float normY = Mathf.Clamp01(virtualPixelPos.y / height);



            Vector2 canvasLocalPos = new Vector2(



                Mathf.Lerp(_canvasRect.rect.xMin, _canvasRect.rect.xMax, normX),



                Mathf.Lerp(_canvasRect.rect.yMin, _canvasRect.rect.yMax, normY)



            );



            Vector3 canvasWorldPoint = _canvasRect.TransformPoint(canvasLocalPos);



            Camera uiCam = (_canvas != null && _canvas.worldCamera != null) ? _canvas.worldCamera : null;



            bool RectContainsPoint(RectTransform rt)



            {

                if (rt == null || !rt.gameObject.activeInHierarchy) return false;



                Vector3 local = rt.InverseTransformPoint(canvasWorldPoint);



                if (rt.rect.Contains((Vector2)local)) return true;



                if (uiCam != null && RectTransformUtility.RectangleContainsScreenPoint(rt, virtualPixelPos, uiCam)) return true;



                return false;

            }



            // A0. Check Virtual Keyboard first if open (it sits above windows)



            var kb = VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance;



            if (kb != null && kb.IsOpen && kb.gameObject.activeInHierarchy)



            {

                var buttons = kb.GetComponentsInChildren<Button>(false);



                foreach (var btn in buttons)



                {

                    if (RectContainsPoint(btn.GetComponent<RectTransform>()))



                        return btn.gameObject;

                }



                if (RectContainsPoint(kb.GetComponent<RectTransform>()))



                    return kb.gameObject;

            }



            // A. Check open windows first (top-most sibling order)



            var winMgr = DesktopWindowManager.Instance;



            if (winMgr != null)



            {

                var windows = winMgr.GetComponentsInChildren<DesktopWindow>(false);



                for (int i = windows.Length - 1; i >= 0; i--)



                {

                    var win = windows[i];



                    if (win == null || !win.IsOpen || win.IsMinimized || !win.gameObject.activeInHierarchy) continue;



                    var winRect = win.GetComponent<RectTransform>();



                    if (RectContainsPoint(winRect))



                    {

                        // Test child buttons inside this window



                        var buttons = win.GetComponentsInChildren<Button>(false);



                        foreach (var btn in buttons)



                        {

                            if (RectContainsPoint(btn.GetComponent<RectTransform>()))



                                return btn.gameObject;

                        }



                        // Test child input fields



                        var inputs = win.GetComponentsInChildren<TMP_InputField>(false);



                        foreach (var inp in inputs)



                        {

                            if (RectContainsPoint(inp.GetComponent<RectTransform>()))



                                return inp.gameObject;

                        }



                        return win.gameObject;

                    }

                }

            }



            // B. Check all DesktopIcons (desktop grid and dock)



            var allIcons = _canvasRect.GetComponentsInChildren<DesktopIcon>(false);



            for (int i = 0; i < allIcons.Length; i++)



            {

                var icon = allIcons[i];



                if (icon == null || !icon.gameObject.activeInHierarchy) continue;



                var iRect = icon.GetComponent<RectTransform>();



                if (RectContainsPoint(iRect))



                    return icon.gameObject;



                // Also check child elements inside the icon (IconBox, Title, etc.)



                for (int c = 0; c < icon.transform.childCount; c++)



                {

                    var childRt = icon.transform.GetChild(c) as RectTransform;



                    if (RectContainsPoint(childRt))



                        return icon.gameObject;

                }

            }



            // C. Check all Buttons across the Canvas



            var allButtons = _canvasRect.GetComponentsInChildren<Button>(false);



            for (int i = allButtons.Length - 1; i >= 0; i--)



            {

                var btn = allButtons[i];



                if (btn == null || !btn.gameObject.activeInHierarchy) continue;



                if (RectContainsPoint(btn.GetComponent<RectTransform>()))



                    return btn.gameObject;

            }



            return null;

        }



        private void ClearHoverState()



        {

            if (_lastHovered != null)



            {

                EventSystem es = EnsureEventSystem();



                PointerEventData pointerData = new PointerEventData(es);



                ExecuteEvents.ExecuteHierarchy(_lastHovered, pointerData, ExecuteEvents.pointerExitHandler);



                var icon = _lastHovered.GetComponentInParent<DesktopIcon>();



                if (icon != null) icon.OnPointerExit(pointerData);



                _lastHovered = null;

            }



            _pressedObject = null;



            _draggedObject = null;



            _isDragging = false;



            _hasVirtualPos = false;
        }

        #region MultiMonitorManager Extended Desktop Bridge

        public void UpdateCursorFromManager(Vector2 uv, bool isHovering, bool isClicking)
        {
            IsMouseOverScreen = true;
            _lastUv = uv;
            if (_cursor == null) _cursor = Object.FindAnyObjectByType<VRDesktopCursor>();
            if (_cursor != null)
            {
                if (!_cursor.gameObject.activeSelf) _cursor.Show(true);
                _cursor.UpdateCursorPosition(uv);
                _cursor.SetState(isHovering || _lastHovered != null || _isDragging, isClicking);
                _cursor.transform.SetAsLastSibling();
            }
        }

        public void ProcessPointerFromManager(Vector2 uv, bool leftDown, bool leftHeld, bool leftUp, Vector2 scroll)
        {
            _lastUv = uv;
            ProcessUIPointerEvents(uv, leftDown, leftHeld, leftUp, scroll);
            // HostDesktopStreamManager.Update() authoritatively forwards mouse input to prevent duplicate clicks
        }

        public void HideCursorFromManager()
        {
            IsMouseOverScreen = false;
            ClearHoverState();
            if (_cursor == null) _cursor = Object.FindAnyObjectByType<VRDesktopCursor>();
            if (_cursor != null && _cursor.gameObject.activeSelf)
            {
                _cursor.Show(false);
            }
        }

        #endregion
    }

}

