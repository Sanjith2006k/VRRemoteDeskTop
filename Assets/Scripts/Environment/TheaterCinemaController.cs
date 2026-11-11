using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Player;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Controls the cinematic VIP Theater experience:
    /// 1. Interacting with the VIP Theater Sofa in front of the globe triggers Cinema Mode.
    /// 2. Smoothly lowers the entire PC Workstation setup down through the floor (hydraulic elevator).
    /// 3. Eases the player's view into the VIP Sofa looking at the IMAX theater screen.
    /// 4. Displays an animated, high-contrast Welcome Screen message on the theater screen.
    /// 5. Pressing 'ESC' or 'E' smoothly raises the workstation back up and restores exploration mode.
    /// </summary>
    public class TheaterCinemaController : MonoBehaviour
    {
        public static TheaterCinemaController Instance { get; private set; }

        [Header("Workstation Elevator References")]
        [Tooltip("Root Transform of the entire PC Workstation setup to lower/raise.")]
        [SerializeField] private Transform _workstation;

        [Tooltip("How far down the workstation lowers into the floor (in meters).")]
        [SerializeField] private float _descendDistance = 2.85f;

        [Tooltip("Duration of the hydraulic elevator descent/ascent in seconds.")]
        [SerializeField] private float _elevatorDuration = 3.0f;

        [Tooltip("Smooth ease-in-out easing curve for workstation elevator.")]
        [SerializeField] private AnimationCurve _elevatorCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("VIP Theater Sofa Seated View")]
        [Tooltip("Transform position where player sits on the VIP Theater Sofa.")]
        [SerializeField] private Transform _sofaSeatPoint;

        [Tooltip("Duration of camera transition to/from sofa seat in seconds.")]
        [SerializeField] private float _cameraTransitionDuration = 1.0f;

        [Header("Theater Screen & Projector")]
        [Tooltip("Welcome Canvas GameObject on the projector screen.")]
        [SerializeField] private GameObject _theaterWelcomeCanvas;

        [Tooltip("Optional ceiling projector beam light to intensify during movie mode.")]
        [SerializeField] private Light _projectorBeamLight;

        [Header("Audio / Atmosphere (Optional)")]
        [SerializeField] private AudioSource _audioSource;

        [Header("State")]
        [SerializeField] private bool _isInCinemaMode = false;
        [SerializeField] private bool _isTransitioning = false;

        private Vector3 _workstationOriginalPos;
        private Vector3 _cameraStandbyPos;
        private Quaternion _cameraStandbyRot;
        private Vector3 _playerStandbyPos;
        private Quaternion _playerStandbyRot;
        private Camera _playerCamera;
        private Transform _cameraOffset;
        private Coroutine _activeWorkstationRoutine;
        private Coroutine _activeCameraRoutine;
        private float _enterTime = 0f;
        private float _seatedYaw = 0f;
        private float _seatedPitch = 0f;
        private GameObject _hudObj;
        private CanvasGroup _hudCanvasGroup;

        public bool IsInCinemaMode => _isInCinemaMode;
        public bool IsTransitioning => _isTransitioning;
        public float CameraTransitionDuration => _cameraTransitionDuration;

        public event Action<bool> OnCinemaModeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            EnsureEventSystem();
            ResolveReferences();

            if (_theaterWelcomeCanvas != null)
            {
                _theaterWelcomeCanvas.SetActive(false);
            }
        }

        private void Start()
        {
            EnsureEventSystem();
            ResolveReferences();
        }

        public static void EnsureEventSystem()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null)
            {
                es = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            }
            if (es == null)
            {
                var esObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                var module = esObj.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (module != null)
                {
                    module.AssignDefaultActions();
                }
                Debug.Log("[TheaterCinemaController] Created runtime EventSystem with InputSystemUIInputModule and default actions.");
            }
            else
            {
                var module = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (module == null)
                {
                    var oldModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    if (oldModule != null) UnityEngine.Object.Destroy(oldModule);
                    module = es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    module.AssignDefaultActions();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ResolveReferences()
        {
            if (_workstation == null)
            {
                var ws = GameObject.Find("Workstation");
                if (ws != null)
                {
                    _workstation = ws.transform;
                    _workstationOriginalPos = ws.transform.position;
                }
            }
            else if (_workstationOriginalPos.sqrMagnitude < 0.001f)
            {
                _workstationOriginalPos = _workstation.position;
            }

            if (_sofaSeatPoint == null)
            {
                var sp = GameObject.Find("VIP_TheaterSofa_SeatPoint");
                if (sp != null) _sofaSeatPoint = sp.transform;
            }

            if (_theaterWelcomeCanvas == null)
            {
                var twc = GameObject.Find("Theater_WelcomeCanvas");
                if (twc != null)
                {
                    _theaterWelcomeCanvas = twc;
                }
                else
                {
                    var acousticWall = GameObject.Find("AcousticFeatureWall");
                    if (acousticWall != null)
                    {
                        var t = acousticWall.transform.Find("Theater_WelcomeCanvas");
                        if (t != null) _theaterWelcomeCanvas = t.gameObject;
                    }
                    if (_theaterWelcomeCanvas == null)
                    {
                        foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
                        {
                            if (c.gameObject.name == "Theater_WelcomeCanvas")
                            {
                                _theaterWelcomeCanvas = c.gameObject;
                                break;
                            }
                        }
                    }
                }
            }

            if (_theaterWelcomeCanvas != null)
            {
                var c = _theaterWelcomeCanvas.GetComponent<Canvas>();
                if (c != null && c.worldCamera == null)
                {
                    c.worldCamera = _playerCamera != null ? _playerCamera : Camera.main;
                }
                var gr = _theaterWelcomeCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (gr != null)
                {
                    gr.ignoreReversedGraphics = false;
                    gr.blockingObjects = UnityEngine.UI.GraphicRaycaster.BlockingObjects.None;
                }
            }

            if (_projectorBeamLight == null)
            {
                var pbl = GameObject.Find("ProjectorBeamLight");
                if (pbl != null) _projectorBeamLight = pbl.GetComponent<Light>();
            }

            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }
            if (_playerCamera != null && _cameraOffset == null)
            {
                _cameraOffset = _playerCamera.transform.parent;
            }
        }

        private void Update()
        {
            if (!_isInCinemaMode) return;

            // Seated interaction & camera controls:
            // 1. Right Mouse Button or Middle Mouse Button HELD: Locks cursor and turns head to look around the room/stars/console
            // 2. Normal (unpressed): Free, visible OS cursor so player can hover and click through all cinema buttons, sliders, & browse files!
            if (!_isTransitioning && _playerCamera != null)
            {
                bool isLookingAround = false;
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    isLookingAround = mouse.rightButton.isPressed || mouse.middleButton.isPressed;
                }
                else
                {
                    try
                    {
                        isLookingAround = UnityEngine.Input.GetMouseButton(1) || UnityEngine.Input.GetMouseButton(2);
                    }
                    catch {}
                }

                if (isLookingAround)
                {
                    // Dragging to look around: lock cursor while dragging
                    if (Cursor.lockState != CursorLockMode.Locked)
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }

                    if (mouse != null)
                    {
                        Vector2 delta = mouse.delta.ReadValue();
                        if (delta.sqrMagnitude > 0.0001f)
                        {
                            float sens = 0.12f;
                            _seatedYaw += delta.x * sens;
                            _seatedPitch = Mathf.Clamp(_seatedPitch - delta.y * sens, -35f, 45f);
                            _seatedYaw = Mathf.Clamp(_seatedYaw, -85f, 85f);

                            Vector3 seatPos = _sofaSeatPoint != null ? _sofaSeatPoint.position : new Vector3(0f, 1.21f, -2.60f);
                            _playerCamera.transform.position = seatPos;
                            _playerCamera.transform.rotation = Quaternion.Euler(_seatedPitch, _seatedYaw, 0f);
                        }
                    }
                }
                else
                {
                    // Free cursor mode: normal mouse movement allows clicking through buttons and sliders
                    if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                }
            }

            if (_isTransitioning) return;

            // ESC key or E key exits cinema mode
            // (E is only ignored if player is directly aiming at another interactive prop like the button console)
            if (Time.time < _enterTime + 0.8f) return;

            bool exitPressed = false;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                exitPressed = true;
            }

            try
            {
                if (!exitPressed && UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    exitPressed = true;
                }
            }
            catch {}

            // E key also smoothly stands up when not aiming at the button console
            if (!exitPressed && (InteractiveProp.CurrentBestProp == null || InteractiveProp.CurrentBestProp is TheaterSofa))
            {
                if (kb != null && kb.eKey.wasPressedThisFrame) exitPressed = true;
                try
                {
                    if (!exitPressed && UnityEngine.Input.GetKeyDown(KeyCode.E)) exitPressed = true;
                }
                catch {}
            }

            if (exitPressed)
            {
                ExitTheaterMode();
            }
        }

        /// <summary>
        /// Interacting with the sofa toggles the VIP Theater Cinema Mode.
        /// </summary>
        public void ToggleTheaterMode()
        {
            if (_isTransitioning) return;

            if (!_isInCinemaMode)
            {
                EnterTheaterMode();
            }
            else
            {
                ExitTheaterMode();
            }
        }

        /// <summary>
        /// Smoothly lowers the PC setup and activates the IMAX theater welcome screen.
        /// </summary>
        public void EnterTheaterMode()
        {
            if (_isTransitioning || _isInCinemaMode) return;

            // Ensure player is not currently in seated PC mode
            if (ComputerInteractionManager.Instance != null && ComputerInteractionManager.Instance.IsInComputerMode)
            {
                ComputerInteractionManager.Instance.ExitComputerMode();
            }

            ResolveReferences();

            _isInCinemaMode = true;
            _isTransitioning = true;
            _enterTime = Time.time;
            _seatedYaw = 0f;
            _seatedPitch = 0f;

            Debug.Log("[TheaterCinemaController] Entering VIP Theater Mode: Lowering PC workstation and activating Welcome Screen.");

            // 1. Disable player movement & mouse look during the camera glide into the sofa
            SetPlayerControls(false);

            var pc = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            if (pc != null)
            {
                _playerStandbyPos = pc.transform.position;
                _playerStandbyRot = pc.transform.rotation;
            }
            var cc = pc != null ? pc.GetComponent<CharacterController>() : null;
            if (cc != null)
            {
                cc.enabled = false;
                cc.detectCollisions = false;
            }

            // 2. Cache standby camera position and orientation in world space
            if (_playerCamera != null)
            {
                _cameraStandbyPos = _playerCamera.transform.position;
                _cameraStandbyRot = _playerCamera.transform.rotation;
            }

            // 3. Start Sequence: Ease player body and camera into sofa seat, THEN run 3-second hydraulic descent!
            if (_sofaSeatPoint == null)
            {
                var sp = GameObject.Find("VIP_TheaterSofa_SeatPoint") ?? GameObject.Find("VIP_TheaterSofa");
                if (sp != null) _sofaSeatPoint = sp.transform;
            }

            Vector3 seatPos = _sofaSeatPoint != null ? _sofaSeatPoint.position : new Vector3(0f, 1.15f, -2.60f);
            Quaternion seatRot = _sofaSeatPoint != null ? _sofaSeatPoint.rotation : Quaternion.identity;

            if (_activeCameraRoutine != null) StopCoroutine(_activeCameraRoutine);
            _activeCameraRoutine = StartCoroutine(TheaterCinemaSequenceRoutine(seatPos, seatRot));

            OnCinemaModeChanged?.Invoke(true);
        }

        private IEnumerator TheaterCinemaSequenceRoutine(Vector3 seatPos, Quaternion seatRot)
        {
            // Step 1: Smoothly ease camera into VIP sofa seat (natural sitting-down arc over 1.25 seconds)
            yield return StartCoroutine(SmoothCameraRoutine(seatPos, seatRot, 1.25f));

            // Step 2: Now that player is seated down in the sofa, start the 3-second hydraulic elevator descent!
            Debug.Log("[TheaterCinemaController] Player seated on VIP sofa. Commencing 3.0-second hydraulic elevator descent.");

            _seatedYaw = 0f;
            _seatedPitch = 0f;

            // Turn ON Welcome Screen immediately as elevator begins descending
            if (_theaterWelcomeCanvas == null)
            {
                ResolveReferences();
            }
            if (_theaterWelcomeCanvas != null)
            {
                _theaterWelcomeCanvas.SetActive(true);
                var c = _theaterWelcomeCanvas.GetComponent<Canvas>();
                if (c != null)
                {
                    c.overrideSorting = true;
                    c.sortingOrder = 50;
                    if (_playerCamera == null) _playerCamera = Camera.main;
                    c.worldCamera = _playerCamera;
                }
                var gr = _theaterWelcomeCanvas.GetComponent<GraphicRaycaster>();
                if (gr != null)
                {
                    gr.ignoreReversedGraphics = false;
                    gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;
                }

                // If a video is loaded or was playing, ensure it continues playing!
                if (TheaterVideoPlayer.Instance != null && TheaterVideoPlayer.Instance.HasActiveVideo)
                {
                    TheaterVideoPlayer.Instance.ResumeOrEnsurePlaying();
                }
            }

            // Intensify Projector Beam Light
            if (_projectorBeamLight == null)
            {
                var pbl = GameObject.Find("ProjectorBeamLight");
                if (pbl != null) _projectorBeamLight = pbl.GetComponent<Light>();
            }
            if (_projectorBeamLight != null)
            {
                _projectorBeamLight.intensity = 20f;
            }

            // Step 3: Lower Workstation over exactly 3.0 seconds
            if (_workstation == null)
            {
                _workstation = GameObject.Find("Workstation")?.transform;
            }

            if (_workstation != null)
            {
                if (_workstationOriginalPos.sqrMagnitude < 0.001f) _workstationOriginalPos = _workstation.position;
                Vector3 downTarget = _workstationOriginalPos + Vector3.down * _descendDistance;

                yield return StartCoroutine(SmoothWorkstationRoutine(downTarget, _elevatorDuration));
                Debug.Log("[TheaterCinemaController] Hydraulic elevator descent complete. IMAX Theater view is 100% clear.");
            }

            _isTransitioning = false;
            ShowTheaterHUD(true);

            // Free & visible cursor so player can hover and click all theater controls!
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Smoothly raises the PC setup back up to standard floor height and restores player movement.
        /// </summary>
        public void ExitTheaterMode()
        {
            if (_isTransitioning || !_isInCinemaMode) return;

            _isInCinemaMode = false;
            _isTransitioning = true;
            ShowTheaterHUD(false);

            // Lock and hide cursor immediately as player begins standing up
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("[TheaterCinemaController] Exiting Theater Mode: Raising PC workstation and restoring exploration.");

            // NOTE: The selected video plays continuously forever until changed, even when standing up (ESC)

            // 1. Smoothly Raise PC Workstation back up over 3.0 seconds
            if (_workstation != null)
            {
                if (_activeWorkstationRoutine != null) StopCoroutine(_activeWorkstationRoutine);
                _activeWorkstationRoutine = StartCoroutine(SmoothWorkstationRoutine(_workstationOriginalPos, _elevatorDuration));
            }

            // 2. Camera and player body return target is safely on the open floor in front of the sofa!
            if (_sofaSeatPoint == null)
            {
                var sp = GameObject.Find("VIP_TheaterSofa_SeatPoint") ?? GameObject.Find("VIP_TheaterSofa");
                if (sp != null) _sofaSeatPoint = sp.transform;
            }

            Vector3 sofaFwd = _sofaSeatPoint != null ? _sofaSeatPoint.forward : Vector3.forward;
            Vector3 seatPos = _sofaSeatPoint != null ? _sofaSeatPoint.position : new Vector3(0f, 1.15f, -2.60f);

            // Open floor spot 0.90m in front of sofa cushion
            Vector3 safeExitPos = seatPos - sofaFwd * 0.90f;
            if (_playerStandbyPos.sqrMagnitude > 0.01f && Vector3.Distance(_playerStandbyPos, seatPos) > 0.5f)
            {
                safeExitPos = _playerStandbyPos;
            }

            Vector3 returnPos = safeExitPos + new Vector3(0f, 1.65f, 0f);
            Quaternion returnRot = (_playerStandbyRot != Quaternion.identity) ? _playerStandbyRot : (_sofaSeatPoint != null ? _sofaSeatPoint.rotation : Quaternion.identity);

            if (_activeCameraRoutine != null) StopCoroutine(_activeCameraRoutine);
            _activeCameraRoutine = StartCoroutine(SmoothReturnRoutine(safeExitPos, returnPos, returnRot, 1.25f, () =>
            {
                // Snap camera cleanly back to local origin under CameraOffset (same as ComputerInteractionManager!)
                if (_playerCamera != null && _cameraOffset != null)
                {
                    _playerCamera.transform.SetParent(_cameraOffset, true);
                    _playerCamera.transform.localPosition = Vector3.zero;
                    _playerCamera.transform.localRotation = Quaternion.identity;
                }

                var ml = FindAnyObjectByType<MouseLook>();
                if (ml != null)
                {
                    ml.SyncOrientation(returnRot.eulerAngles.y, 0f);
                    ml.SetCursorLock(true);
                }

                SetPlayerControls(true);
                _isTransitioning = false;
                Debug.Log("[TheaterCinemaController] Stand up complete. PC workstation restored. Exploration active.");
            }));

            // 3. Handle background playback vs watch-only mode
            bool hasActiveVideo = TheaterVideoPlayer.Instance != null && TheaterVideoPlayer.Instance.HasActiveVideo;
            bool playInBg = TheaterVideoPlayer.Instance == null || TheaterVideoPlayer.Instance.PlayInBackground;

            if (_theaterWelcomeCanvas != null)
            {
                if (hasActiveVideo && playInBg)
                {
                    // Background Play: Keep video playing borderless on the IMAX wall screen
                    _theaterWelcomeCanvas.SetActive(true);
                    if (TheaterVideoPlayer.Instance != null)
                    {
                        TheaterVideoPlayer.Instance.SetControlsVisible(false);
                    }
                }
                else
                {
                    // Watch Only: Pause playback and turn off the screen
                    if (hasActiveVideo && !playInBg && TheaterVideoPlayer.Instance != null)
                    {
                        TheaterVideoPlayer.Instance.Pause();
                    }
                    _theaterWelcomeCanvas.SetActive(false);
                }
            }

            // 4. Projector beam stays gently illuminated only if video is actively playing in background
            if (_projectorBeamLight != null)
            {
                _projectorBeamLight.intensity = (hasActiveVideo && playInBg) ? 16f : 10f;
            }

            OnCinemaModeChanged?.Invoke(false);
        }

        private IEnumerator SmoothWorkstationRoutine(Vector3 targetPos, float duration)
        {
            if (_workstation == null) yield break;

            Vector3 startPos = _workstation.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = _elevatorCurve != null ? _elevatorCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

                _workstation.position = Vector3.Lerp(startPos, targetPos, curveT);
                yield return null;
            }

            _workstation.position = targetPos;
        }

        private IEnumerator SmoothCameraRoutine(Vector3 targetPos, Quaternion targetRot, float duration)
        {
            if (_playerCamera == null) _playerCamera = Camera.main;
            if (_playerCamera == null) yield break;

            var pc = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            Vector3 startPlayerPos = pc != null ? pc.transform.position : _playerCamera.transform.position;
            Vector3 targetPlayerPos = new Vector3(targetPos.x, startPlayerPos.y, targetPos.z);
            Quaternion startPlayerRot = pc != null ? pc.transform.rotation : _playerCamera.transform.rotation;

            Vector3 startPos = _playerCamera.transform.position;
            Quaternion startRot = _playerCamera.transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Natural, elegant sitting arc:
                // Smooth horizontal easing in/out
                float hT = Mathf.SmoothStep(0f, 1f, t);
                Vector3 curXZ = Vector3.Lerp(new Vector3(startPos.x, 0f, startPos.z), new Vector3(targetPos.x, 0f, targetPos.z), hT);

                // Vertical easing: soft descent into cushions (starts gentle, sinks gently)
                float vT = Mathf.SmoothStep(0f, 1f, Mathf.Pow(t, 1.35f));
                float curY = Mathf.Lerp(startPos.y, targetPos.y, vT);

                if (pc != null)
                {
                    pc.transform.position = Vector3.Lerp(startPlayerPos, targetPlayerPos, hT);
                    pc.transform.rotation = Quaternion.Slerp(startPlayerRot, targetRot, hT);
                }

                _playerCamera.transform.position = new Vector3(curXZ.x, curY, curXZ.z);
                _playerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, hT);
                yield return null;
            }

            if (pc != null)
            {
                pc.transform.position = targetPlayerPos;
                pc.transform.rotation = targetRot;
            }

            _playerCamera.transform.position = targetPos;
            _playerCamera.transform.rotation = targetRot;
        }

        private IEnumerator SmoothReturnRoutine(Vector3 exitPlayerPos, Vector3 targetCamPos, Quaternion targetRot, float duration, Action onComplete)
        {
            if (_playerCamera == null) _playerCamera = Camera.main;
            if (_playerCamera == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var pc = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            Vector3 startPlayerPos = pc != null ? pc.transform.position : _playerCamera.transform.position;
            Quaternion startPlayerRot = pc != null ? pc.transform.rotation : _playerCamera.transform.rotation;

            Vector3 startPos = _playerCamera.transform.position;
            Quaternion startRot = _playerCamera.transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (pc != null)
                {
                    pc.transform.position = Vector3.Lerp(startPlayerPos, exitPlayerPos, smoothT);
                    pc.transform.rotation = Quaternion.Slerp(startPlayerRot, targetRot, smoothT);
                }

                _playerCamera.transform.position = Vector3.Lerp(startPos, targetCamPos, smoothT);
                _playerCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                yield return null;
            }

            if (pc != null)
            {
                pc.transform.position = exitPlayerPos;
                pc.transform.rotation = targetRot;
            }

            _playerCamera.transform.position = targetCamPos;
            _playerCamera.transform.rotation = targetRot;

            Physics.SyncTransforms();
            var cc = pc != null ? pc.GetComponent<CharacterController>() : null;
            if (cc != null)
            {
                cc.enabled = true;
                cc.detectCollisions = true;
            }

            onComplete?.Invoke();
        }

        public void SetPlayerControls(bool enabled)
        {
            var pcs = FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
            foreach (var pc in pcs)
            {
                if (pc != null) pc.IsMovementEnabled = enabled;
            }

            var mls = FindObjectsByType<MouseLook>(FindObjectsInactive.Include);
            foreach (var ml in mls)
            {
                if (ml != null)
                {
                    ml.IsLookEnabled = enabled;
                    if (enabled) ml.SetCursorLock(true);
                    else ml.SetCursorLock(false);
                }
            }
        }

        private void EnsureTheaterHUD()
        {
            if (_hudObj != null) return;

            _hudObj = new GameObject("TheaterModeHUD");
            var canvas = _hudObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            _hudObj.AddComponent<CanvasScaler>();
            _hudCanvasGroup = _hudObj.AddComponent<CanvasGroup>();
            _hudCanvasGroup.alpha = 0f;
            _hudCanvasGroup.blocksRaycasts = false;

            var pill = new GameObject("HUD_Pill");
            pill.transform.SetParent(_hudObj.transform, false);
            var rt = pill.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(660f, 44f);

            var bg = pill.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.08f, 0.16f, 0.88f);

            var txtObj = new GameObject("HUD_Text");
            txtObj.transform.SetParent(pill.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "<color=#00E5FF><b>[E / ESC]</b></color> STAND UP    <color=#5080a0>|</color>    <color=#00E5FF><b>[B]</b></color> BG PLAY TOGGLE    <color=#5080a0>|</color>    <color=#00E5FF><b>[RMB DRAG]</b></color> LOOK AROUND";
            tmp.fontSize = 15f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private void ShowTheaterHUD(bool show)
        {
            EnsureTheaterHUD();
            if (_hudCanvasGroup != null)
            {
                _hudCanvasGroup.alpha = show ? 1f : 0f;
            }
        }
    }
}
