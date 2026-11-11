using System.Collections.Generic;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.InputSystem;

using UnityEngine.UI;

using UnityEngine.XR.Interaction.Toolkit;

using UnityEngine.XR.Interaction.Toolkit.Interactables;

using UnityEngine.XR.Interaction.Toolkit.Interactors;

using VRRemoteDesktop.Computer;



namespace VRRemoteDesktop.XR



{

    /// <summary>

    /// Interaction bridge for Left and Right Auxiliary Displays (Section 5 & 6).

    /// Supports both 6DoF VR controller raycasts and PC mouse simulation.

    /// Maps 3D hit coordinates to target Canvas UV coordinates, displays a neon cursor,

    /// dispatches UI pointer events (Hover, Click, Drag) to interactive buttons and sliders,

    /// and synchronizes active display selection in MultiMonitorManager.

    /// </summary>



    [RequireComponent(typeof(Collider))]

    public class AuxiliaryScreenInteractable : XRBaseInteractable
    {

        [Header("Target UI System")]

        [SerializeField] private Canvas _targetCanvas;



        [SerializeField] private GraphicRaycaster _raycaster;



        [SerializeField] private Camera _uiCamera;



        [SerializeField] private string _canvasName = "LeftDesktopCanvas";



        [SerializeField] private string _cameraName = "UILeftCamera";



        [SerializeField] private ActiveDisplayIndex _displayIndex = ActiveDisplayIndex.Left;



        [Header("Cursor Settings")]

        [SerializeField] private Color _cursorColor = new Color(0f, 1f, 1f, 0.95f);



        [SerializeField] private float _cursorSize = 56f;



        [Header("Simulation Fallback")]

        [SerializeField] private bool _enableMouseSimulation = true;



        private static readonly List<AuxiliaryScreenInteractable> _activeAuxScreens = new List<AuxiliaryScreenInteractable>();



        public static bool IsAnyHoveringInteractive



        {

            get



            {

                for (int i = 0; i < _activeAuxScreens.Count; i++)



                {

                    if (_activeAuxScreens[i] != null && _activeAuxScreens[i].IsHoveringInteractive) return true;

                }



                return false;

            }

        }



        public static bool IsAnyMouseOverScreen



        {

            get



            {

                for (int i = 0; i < _activeAuxScreens.Count; i++)



                {

                    if (_activeAuxScreens[i] != null && _activeAuxScreens[i].IsMouseOverScreen) return true;

                }



                return false;

            }

        }



        private Camera _mainCamera;



        private RectTransform _canvasRect;



        private RectTransform _cursorRt;



        private Image _cursorImage;



        private Collider _screenCollider;



        private GameObject _hoveredObject;



        private Vector2 _lastUv = new Vector2(0.5f, 0.5f);



        private bool _isHovering = false;



        private bool _isClicking = false;



        private bool _wasClickingLastFrame = false;



        private XRRayInteractor _currentRayInteractor;



        private bool _isMouseOverScreen = false;



        public bool IsMouseOverScreen => _isMouseOverScreen;



        public ActiveDisplayIndex DisplayIndex => _displayIndex;



        public bool IsHovering => _isHovering;



        public bool IsHoveringInteractive => _hoveredObject != null;



        protected override void OnEnable()



        {

            base.OnEnable();



            if (!_activeAuxScreens.Contains(this)) _activeAuxScreens.Add(this);

        }



        public void Configure(string canvasName, string cameraName, Color cursorColor)



        {

            _canvasName = canvasName;



            _cameraName = cameraName;



            _cursorColor = cursorColor;



            if (_canvasName.Contains("Right")) _displayIndex = ActiveDisplayIndex.Right;



            else _displayIndex = ActiveDisplayIndex.Left;



            ResolveReferences();

        }



        protected override void Awake()



        {

            base.Awake();



            if (_canvasName.Contains("Right")) _displayIndex = ActiveDisplayIndex.Right;



            else _displayIndex = ActiveDisplayIndex.Left;



            ResolveReferences();

        }



        private void Start()



        {

            ResolveReferences();



            EnsureCursor();

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



            if (_targetCanvas == null)



            {

                var cObj = GameObject.Find(_canvasName);



                if (cObj != null)



                {

                    _targetCanvas = cObj.GetComponent<Canvas>();



                    _raycaster = cObj.GetComponent<GraphicRaycaster>();



                    _canvasRect = cObj.GetComponent<RectTransform>();

                }

            }



            else if (_canvasRect == null)



            {

                _canvasRect = _targetCanvas.GetComponent<RectTransform>();



                if (_raycaster == null) _raycaster = _targetCanvas.GetComponent<GraphicRaycaster>();

            }



            if (_uiCamera == null)



            {

                var camObj = GameObject.Find(_cameraName);



                if (camObj != null) _uiCamera = camObj.GetComponent<Camera>();

            }



            if (_screenCollider == null)



            {

                _screenCollider = GetComponent<Collider>();

            }

        }



        private void EnsureCursor()



        {

            if (_cursorRt != null || _targetCanvas == null) return;



            Transform existing = _targetCanvas.transform.Find("AuxVirtualCursor") ?? _targetCanvas.transform.Find("VRCursor");



            if (existing != null)



            {

                _cursorRt = existing.GetComponent<RectTransform>();



                _cursorImage = existing.GetComponent<Image>();



                return;

            }



            GameObject cursorObj = new GameObject("AuxVirtualCursor", typeof(RectTransform), typeof(Image));



            cursorObj.transform.SetParent(_targetCanvas.transform, false);



            _cursorRt = cursorObj.GetComponent<RectTransform>();



            _cursorSize = 72f;
            _cursorRt.sizeDelta = new Vector2(_cursorSize, _cursorSize);
            _cursorRt.pivot = new Vector2(0.08f, 0.92f); // top-left cursor tip



            _cursorRt.anchorMin = new Vector2(0f, 0f);



            _cursorRt.anchorMax = new Vector2(0f, 0f);



            _cursorImage = cursorObj.GetComponent<Image>();



            _cursorImage.color = _cursorColor;



            _cursorImage.raycastTarget = false;



            if (_cursorImage.sprite == null)



            {

                var arrowSprite = Resources.Load<Sprite>("Tex_CursorArrow");



                if (arrowSprite == null)



                {

#if UNITY_EDITOR



                    arrowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tex_CursorArrow.png");



#endif

                }



                if (arrowSprite != null)



                {

                    _cursorImage.sprite = arrowSprite;

                }

            }



            cursorObj.SetActive(false);

        }



        #region XR Interaction Toolkit



        protected override void OnHoverEntered(HoverEnterEventArgs args)



        {

            base.OnHoverEntered(args);



            if (args.interactorObject is XRRayInteractor rayInteractor)



            {

                _currentRayInteractor = rayInteractor;



                _isHovering = true;



                EnsureCursor();



                if (_cursorRt != null) _cursorRt.gameObject.SetActive(true);

            }

        }



        protected override void OnHoverExited(HoverExitEventArgs args)



        {

            base.OnHoverExited(args);



            if (ReferenceEquals(args.interactorObject, _currentRayInteractor))



            {

                _currentRayInteractor = null;



                _isHovering = false;



                if (_cursorRt != null) _cursorRt.gameObject.SetActive(false);



                ClearHoverState();

            }

        }



        protected override void OnSelectEntered(SelectEnterEventArgs args)



        {

            base.OnSelectEntered(args);



            if (ReferenceEquals(args.interactorObject, _currentRayInteractor))



            {

                _isClicking = true;



                // Focus active monitor in MultiMonitorManager and slide to it



                if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.ActiveDisplay != _displayIndex)



                {

                    MultiMonitorManager.Instance.SetActiveDisplay(_displayIndex);

                }



                ComputerInteractionManager.Instance?.SlideToMonitor(_displayIndex);

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



                        ProcessCanvasPointerEvents(uv, vrDown, vrHeld, vrUp);

                    }

                }

            }

        }



        #endregion



        #region Mouse Simulation Fallback



        private void Update()



        {

            if (_isHovering || !_enableMouseSimulation) return;



            var cim = ComputerInteractionManager.Instance;



            bool inComputerMode = cim != null && cim.IsInteractingAtDesktop;

            if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.IsExtendedDesktopActive && inComputerMode)
            {
                // MultiMonitorManager coordinates extended virtual desktop across all three screens
                return;
            }



            if (!inComputerMode)



            {

                _isMouseOverScreen = false;



                if (_cursorRt != null && _cursorRt.gameObject.activeSelf)



                {

                    ClearHoverState();



                    _cursorRt.gameObject.SetActive(false);

                }



                return;

            }



            // Hide the PC operating system cursor while in computer mode



            if (Cursor.visible)



            {

                Cursor.visible = false;

            }



            Camera cam = GetPlayerCamera();
            if (cam == null) return;



            var mouse = Mouse.current;



            if (mouse == null) return;



            // When player is right-click looking around the room, bypass screen hover and click dispatch



            bool isRmbLooking = (cim != null && cim.IsRightClickLooking) || mouse.rightButton.isPressed;



            try



            {

                if (!isRmbLooking && UnityEngine.Input.GetMouseButton(1)) isRmbLooking = true;

            }



            catch {}



            if (isRmbLooking)



            {

                _isMouseOverScreen = false;



                if (_cursorRt != null && _cursorRt.gameObject.activeSelf)



                {

                    ClearHoverState();



                    _cursorRt.gameObject.SetActive(false);

                }



                return;

            }



            Vector2 mousePos = mouse.position.ReadValue();



            Ray ray = cam.ScreenPointToRay(mousePos);



            if (_screenCollider == null) _screenCollider = GetComponent<Collider>();



            if (_screenCollider == null) _screenCollider = GetComponent<Collider>();



            Vector2 uv = _lastUv;



            bool hitValid = false;



            // 1. Direct screen collider raycast



            if (_screenCollider != null && _screenCollider.Raycast(ray, out RaycastHit hit, 50f))



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



            // Screen-space HUD buttons do not block monitor raycast



            if (!hitValid)



            {

                _isMouseOverScreen = false;



                ClearHoverState();



                return;

            }



            _isMouseOverScreen = true;

            EnsureCursor();
            if (_cursorRt != null)
            {
                if (!_cursorRt.gameObject.activeSelf) _cursorRt.gameObject.SetActive(true);
                UpdateCursorPosition(uv);
                _cursorRt.SetAsLastSibling();
            }



            // Click focus & slide to monitor



            if (mouse.leftButton.wasPressedThisFrame)



            {

                if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.ActiveDisplay != _displayIndex)



                {

                    MultiMonitorManager.Instance.SetActiveDisplay(_displayIndex);

                }



                ComputerInteractionManager.Instance?.SlideToMonitor(_displayIndex);

            }



            ProcessCanvasPointerEvents(uv, mouse.leftButton.wasPressedThisFrame, mouse.leftButton.isPressed, mouse.leftButton.wasReleasedThisFrame);

        }



        #endregion



        private void UpdateCursorPosition(Vector2 uv)



        {

            if (_cursorRt == null) return;



            float canvasW = _canvasRect != null ? _canvasRect.rect.width : 1920f;



            float canvasH = _canvasRect != null ? _canvasRect.rect.height : 1080f;



            _cursorRt.anchoredPosition = new Vector2(uv.x * canvasW, uv.y * canvasH);

        }



        private GameObject FindTopUIObject(Vector2 screenPixelPos, PointerEventData pointerData)



        {

            // Safely get cursor GameObject reference without triggering MissingReferenceException on destroyed objects



            GameObject cursorObj = null;



            if (_cursorRt != null)



            {

                try { cursorObj = _cursorRt.gameObject; } catch {}

            }



            // 1. Primary: GraphicRaycaster



            if (_raycaster != null && pointerData != null)



            {

                List<RaycastResult> results = new List<RaycastResult>();



                _raycaster.Raycast(pointerData, results);



                for (int i = 0; i < results.Count; i++)



                {

                    GameObject go = results[i].gameObject;



                    if (go != null && (cursorObj == null || go != cursorObj) && go != gameObject)



                    {

                        return go;

                    }

                }

            }



            // 2. Geometric fallback across canvas components



            if (_targetCanvas == null) return null;



            Camera cam = (_uiCamera != null) ? _uiCamera : null;



            try



            {

                var buttons = _targetCanvas.GetComponentsInChildren<Button>(false);



                if (buttons != null)



                {

                    for (int i = buttons.Length - 1; i >= 0; i--)



                    {

                        var btn = buttons[i];



                        if (btn == null || !btn.gameObject.activeInHierarchy) continue;



                        var bRect = btn.GetComponent<RectTransform>();



                        if (bRect != null && RectTransformUtility.RectangleContainsScreenPoint(bRect, screenPixelPos, cam))



                        {

                            return btn.gameObject;

                        }

                    }

                }



                var sliders = _targetCanvas.GetComponentsInChildren<Slider>(false);



                if (sliders != null)



                {

                    for (int i = sliders.Length - 1; i >= 0; i--)



                    {

                        var sl = sliders[i];



                        if (sl == null || !sl.gameObject.activeInHierarchy) continue;



                        var sRect = sl.GetComponent<RectTransform>();



                        if (sRect != null && RectTransformUtility.RectangleContainsScreenPoint(sRect, screenPixelPos, cam))



                        {

                            return sl.gameObject;

                        }

                    }

                }

            }



            catch {}



            return null;

        }



        private void ProcessCanvasPointerEvents(Vector2 uv, bool leftDown, bool leftHeld, bool leftUp)



        {

            if (_targetCanvas == null || _raycaster == null)



            {

                ResolveReferences();



                if (_targetCanvas == null || _raycaster == null) return;

            }



            EventSystem eventSystem = EventSystem.current;



            if (eventSystem == null) return;



            float canvasW = _canvasRect != null ? _canvasRect.rect.width : 1920f;



            float canvasH = _canvasRect != null ? _canvasRect.rect.height : 1080f;



            Vector2 screenPixelPos = new Vector2(uv.x * canvasW, uv.y * canvasH);



            PointerEventData pointerData = new PointerEventData(eventSystem)



            {

                position = screenPixelPos,



                button = PointerEventData.InputButton.Left

            };



            GameObject currentHit = FindTopUIObject(screenPixelPos, pointerData);



            // Hover transitions



            if (currentHit != _hoveredObject)



            {

                if (_hoveredObject != null)



                {

                    ExecuteEvents.Execute(_hoveredObject, pointerData, ExecuteEvents.pointerExitHandler);

                }



                _hoveredObject = currentHit;



                if (_hoveredObject != null)



                {

                    ExecuteEvents.Execute(_hoveredObject, pointerData, ExecuteEvents.pointerEnterHandler);

                }

            }



            // Clicks



            if (leftDown && currentHit != null)



            {

                ExecuteEvents.Execute(currentHit, pointerData, ExecuteEvents.pointerDownHandler);



                var btn = currentHit.GetComponentInParent<Button>();



                if (btn != null && btn.interactable)



                {

                    btn.onClick.Invoke();



                    VRRemoteDesktop.Audio.AudioManager.Instance?.PlayClick();

                }



                else



                {

                    ExecuteEvents.Execute(currentHit, pointerData, ExecuteEvents.pointerClickHandler);

                }

            }



            else if (leftHeld && currentHit != null)



            {

                ExecuteEvents.Execute(currentHit, pointerData, ExecuteEvents.dragHandler);

            }



            else if (leftUp && currentHit != null)



            {

                ExecuteEvents.Execute(currentHit, pointerData, ExecuteEvents.pointerUpHandler);

            }

        }



        private void ClearHoverState()



        {

            if (_hoveredObject != null && EventSystem.current != null)



            {

                PointerEventData pointerData = new PointerEventData(EventSystem.current);



                ExecuteEvents.Execute(_hoveredObject, pointerData, ExecuteEvents.pointerExitHandler);



                _hoveredObject = null;

            }

        }



        protected override void OnDisable()



        {

            _isMouseOverScreen = false;



            ClearHoverState();



            if (_cursorRt != null) _cursorRt.gameObject.SetActive(false);



            _activeAuxScreens.Remove(this);



            base.OnDisable();

        }



        protected override void OnDestroy()



        {

            _activeAuxScreens.Remove(this);

            base.OnDestroy();
        }

        #region MultiMonitorManager Extended Desktop Bridge

        public void UpdateCursorFromManager(Vector2 uv, bool isHovering, bool isClicking)
        {
            _isMouseOverScreen = true;
            _lastUv = uv;
            EnsureCursor();
            if (_cursorRt != null)
            {
                if (!_cursorRt.gameObject.activeSelf) _cursorRt.gameObject.SetActive(true);
                UpdateCursorPosition(uv);
                _cursorRt.SetAsLastSibling();

                if (_cursorImage != null)
                {
                    Color targetCol = isClicking ? new Color(1f, 0.05f, 0.55f, 1f) :
                                     (isHovering || _hoveredObject != null ? new Color(0.2f, 1f, 0.8f, 1f) : _cursorColor);
                    _cursorImage.color = targetCol;
                }
            }
        }

        public void ProcessPointerFromManager(Vector2 uv, bool leftDown, bool leftHeld, bool leftUp, Vector2 scroll)
        {
            _lastUv = uv;
            ProcessCanvasPointerEvents(uv, leftDown, leftHeld, leftUp);
        }

        public void HideCursorFromManager()
        {
            _isMouseOverScreen = false;
            ClearHoverState();
            if (_cursorRt != null && _cursorRt.gameObject.activeSelf)
            {
                _cursorRt.gameObject.SetActive(false);
            }
        }

        #endregion
    }

}

