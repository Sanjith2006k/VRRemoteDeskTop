using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using VRRemoteDesktop.Player;
using VRRemoteDesktop.UI;

namespace VRRemoteDesktop.Computer
{
    /// <summary>
    /// States representing player interaction with the virtual workstation computer.
    /// </summary>
    public enum WorkstationMode
    {
        Exploration,
        TransitioningToComputer,
        ComputerInteraction,
        TransitioningToExploration
    }

    /// <summary>
    /// Master coordinator for Phase 5 Computer Interaction Mode.
    /// Manages:
    /// - Proximity detection between player and computer monitor
    /// - Entering Computer Interaction Mode with 'E'
    /// - Smooth ergonomic camera interpolation to seated workstation view
    /// - Disabling WASD movement and mouse-look while using the computer
    /// - Unlocking cursor for virtual desktop interaction
    /// - Exiting Computer Interaction Mode with 'ESC' and smooth return to exploration
    /// - Firing mode events for UI, cursor, and input subsystems
    /// </summary>
    public class ComputerInteractionManager : MonoBehaviour
    {
        public static ComputerInteractionManager Instance { get; private set; }

        [Header("Target References")]
        [Tooltip("Transform of the virtual monitor or screen surface.")]
        [SerializeField] private Transform _monitorTransform;

        [Tooltip("Transform representing the seated eye position looking at the screen.")]
        [SerializeField] private Transform _seatedFocusPoint;

        [Tooltip("Player controller reference to disable WASD movement while at the computer.")]
        [SerializeField] private PlayerController _playerController;

        [Tooltip("MouseLook reference to disable camera look and unlock cursor.")]
        [SerializeField] private MouseLook _mouseLook;

        [Tooltip("Main 3D camera that views the world.")]
        [SerializeField] private Camera _playerCamera;

        [Tooltip("Camera offset parent transform under the player.")]
        [SerializeField] private Transform _cameraOffset;

        [Tooltip("Interaction Prompt UI controller (displays 'USE COMPUTER [E]' and 'PRESS [ESC] TO EXIT').")]
        [SerializeField] private InteractionPromptUI _promptUI;

        [Header("Input System Actions (Optional)")]
        [Tooltip("Input action for interacting (defaults to 'E' key).")]
        [SerializeField] private InputActionReference _interactAction;

        [Tooltip("Input action for canceling/exiting (defaults to 'ESC' key).")]
        [SerializeField] private InputActionReference _cancelAction;

        [Header("Proximity Settings")]
        [Tooltip("Maximum distance in meters to prompt computer interaction.")]
        [SerializeField] private float _interactionDistance = 4.5f;

        [Tooltip("Maximum angle in degrees between player forward direction and monitor.")]
        [SerializeField] private float _facingAngleThreshold = 120.0f;

        [Header("Transition Settings")]
        [Tooltip("Duration of the camera transition in seconds.")]
        [SerializeField] private float _transitionDuration = 1.0f;

        [Tooltip("Easing curve for the camera transition.")]
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

#pragma warning disable 0414
        [Header("Seated Offset (If SeatedFocusPoint is unassigned)")]
        [Tooltip("Distance in meters in front of the monitor center for seated view.")]
        [SerializeField] private float _seatedViewingDistance = 0.68f;

        [Tooltip("Vertical eye offset relative to monitor center.")]
        [SerializeField] private float _seatedVerticalOffset = 0.0f;
#pragma warning restore 0414

        [Header("Seated Workstation Navigation")]
        [Tooltip("Lateral slide speed when moving between monitors with A/D or Arrow keys.")]
        [SerializeField] private float _slideSpeed = 1.8f;

        [Tooltip("Zoom / Lean-in speed with W/S or Arrow keys.")]
        [SerializeField] private float _zoomSpeed = 1.4f;

        [Tooltip("Maximum slide distance along desk rail.")]
        [SerializeField] private float _maxSlideDistance = 1.25f;

        [Tooltip("Maximum lean-in distance towards screens.")]
        [SerializeField] private float _maxZoomDistance = 0.28f;

        [Header("Seated Workstation Cockpit FOV")]
        [Tooltip("Wide-angle base FOV when seated at the computer workstation to encompass all 3 screens.")]
        [SerializeField] private float _seatedBaseFOV = 84f;

        private WorkstationMode _currentMode = WorkstationMode.Exploration;
        private bool _isInRange = false;
        private Coroutine _activeTransitionRoutine;
        private float _enteredComputerTime = 0f;

        // Seated Navigation State
        private Vector3 _baseSeatedPos;
        private Quaternion _baseSeatedRot;
        private float _targetSeatX = 0f;
        private float _currentSeatX = 0f;
        private float _targetSeatZ = 0f;
        private float _currentSeatZ = 0f;
        private float _freeLookYaw = 0f;
        private float _freeLookPitch = 0f;
        private float _baseFOV = 84f;
        private float _explorationFOV = 60f;
        private Transform _chairTransform;
        private Vector3 _chairBasePos;

        public WorkstationMode CurrentMode => _currentMode;
        public bool IsInComputerMode => _currentMode != WorkstationMode.Exploration;
        public bool IsInteractingAtDesktop => _currentMode == WorkstationMode.ComputerInteraction;
        public bool IsTransitioning => _currentMode == WorkstationMode.TransitioningToComputer || _currentMode == WorkstationMode.TransitioningToExploration;
        public bool IsInRange => _isInRange;
        public float FacingAngleThreshold => _facingAngleThreshold;
        public Transform PlayerTransform => _playerController != null ? _playerController.transform : transform;
        public PlayerController Player => _playerController;

        public event Action<WorkstationMode> OnModeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            ResolveReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (_promptUI != null)
            {
                _promptUI.HideAllImmediate();
            }

            AudioListener.volume = 1.0f;
            AudioListener.pause = false;
            var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
            if (listeners == null || listeners.Length == 0)
            {
                var cam = _playerCamera ?? Camera.main;
                if (cam != null)
                {
                    cam.gameObject.AddComponent<AudioListener>();
                    Debug.Log($"[ComputerInteractionManager] Added AudioListener to {cam.name}.");
                }
            }
            else if (listeners.Length > 1)
            {
                for (int i = 1; i < listeners.Length; i++)
                {
                    if (listeners[i] != null) listeners[i].enabled = false;
                }
            }
        }

        private void OnEnable()
        {
            EnableAction(_interactAction);
            EnableAction(_cancelAction);
            if (MultiMonitorManager.Instance != null)
            {
                MultiMonitorManager.Instance.OnActiveDisplayChanged += HandleActiveDisplayChanged;
            }
        }

        private void OnDisable()
        {
            DisableAction(_interactAction);
            DisableAction(_cancelAction);
            if (MultiMonitorManager.Instance != null)
            {
                MultiMonitorManager.Instance.OnActiveDisplayChanged -= HandleActiveDisplayChanged;
            }
        }

        private void HandleActiveDisplayChanged(ActiveDisplayIndex display)
        {
            if (_currentMode == WorkstationMode.ComputerInteraction)
            {
                SlideToMonitor(display);
            }
        }

        private void Update()
        {
            switch (_currentMode)
            {
                case WorkstationMode.Exploration:
                    UpdateProximityCheck();
                    CheckEnterTrigger();
                    break;

                case WorkstationMode.ComputerInteraction:
                    UpdateSeatedWorkstationControls();
                    CheckExitTrigger();
                    break;
            }
        }

        public void SetPlayerControlsEnabled(bool enabled)
        {
            var controllers = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
            foreach (var pc in controllers)
            {
                if (pc != null)
                {
                    pc.IsMovementEnabled = enabled;
                }
            }

            var looks = UnityEngine.Object.FindObjectsByType<MouseLook>(FindObjectsInactive.Include);
            foreach (var ml in looks)
            {
                if (ml != null)
                {
                    ml.IsLookEnabled = enabled;
                    if (enabled)
                    {
                        ml.SetCursorLock(true);
                    }
                }
            }
        }

        private void ResolveReferences()
        {
            if (_playerController == null || !_playerController.gameObject.activeInHierarchy)
            {
                _playerController = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            }

            if (_mouseLook == null || !_mouseLook.gameObject.activeInHierarchy)
            {
                _mouseLook = UnityEngine.Object.FindAnyObjectByType<MouseLook>();
            }

            if (_playerCamera == null || !_playerCamera.gameObject.activeInHierarchy)
            {
                _playerCamera = Camera.main;
                if (_playerCamera == null)
                {
                    var allCams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
                    foreach (var cam in allCams)
                    {
                        if (cam.name != "UICamera" && cam.name != "UILeftCamera" && cam.name != "UIRightCamera")
                        {
                            _playerCamera = cam;
                            break;
                        }
                    }
                }
            }

            if (_cameraOffset == null && _playerCamera != null)
            {
                _cameraOffset = _playerCamera.transform.parent;
            }

            if (_chairTransform == null)
            {
                var chairObj = GameObject.Find("Workstation_GamingChair");
                if (chairObj != null)
                {
                    _chairTransform = chairObj.transform;
                    _chairBasePos = _chairTransform.localPosition;
                }
            }

            if (_monitorTransform == null)
            {
                var screenObj = GameObject.Find("VirtualMonitor") ?? GameObject.Find("Screen");
                if (screenObj != null) _monitorTransform = screenObj.transform;
            }

            if (_seatedFocusPoint == null)
            {
                var seatedObj = GameObject.Find("SeatedFocusPoint");
                if (seatedObj != null) _seatedFocusPoint = seatedObj.transform;
            }

            if (_seatedFocusPoint != null)
            {
                // Ensure seated focus point is firmly placed at first-person seated eye level (Y=1.18m, Z=0.40m)
                float chairZ = _chairTransform != null ? _chairTransform.position.z : 0.15f;
                float chairX = _chairTransform != null ? _chairTransform.position.x : 0f;
                _seatedFocusPoint.position = new Vector3(chairX, 1.18f, chairZ + 0.25f);
                _seatedFocusPoint.rotation = Quaternion.identity;
            }

            if (_promptUI == null)
            {
                _promptUI = UnityEngine.Object.FindAnyObjectByType<InteractionPromptUI>();
            }
        }

        /// <summary>
        /// Calculates distance and orientation between player and the monitor.
        /// </summary>
        private void UpdateProximityCheck()
        {
            if (VRRemoteDesktop.Environment.TheaterCinemaController.Instance != null &&
                VRRemoteDesktop.Environment.TheaterCinemaController.Instance.IsInCinemaMode)
            {
                if (_isInRange)
                {
                    _isInRange = false;
                    if (_promptUI != null) _promptUI.ShowEnterPrompt(false);
                }
                return;
            }

            if (_playerCamera == null)
            {
                ResolveReferences();
                if (_playerCamera == null) return;
            }

            Vector3 playerPos = _playerCamera.transform.position;
            Vector3 deskFocusPos = _seatedFocusPoint != null ? _seatedFocusPoint.position : (_monitorTransform != null ? _monitorTransform.position : new Vector3(0f, 1.15f, 0.65f));

            float distance = Vector3.Distance(playerPos, deskFocusPos);

            // Generous workstation cockpit zone: encompasses standing/walking around the chair and desk
            bool inWorkstationZone = Mathf.Abs(playerPos.x) <= 2.4f && playerPos.z >= -1.4f && playerPos.z <= 2.0f;

            Vector3 toDesk = (deskFocusPos - playerPos).normalized;
            float angle = Vector3.Angle(_playerCamera.transform.forward, toDesk);

            // Yield only if the player is actively targeting a close non-workstation prop (e.g. coffee machine right in front of them)
            var currentProp = VRRemoteDesktop.Environment.InteractiveProp.CurrentBestProp;
            bool targetedOtherCloseProp = currentProp != null && 
                                          !(currentProp is VRRemoteDesktop.Environment.WorkstationChair) &&
                                          Vector3.Distance(playerPos, currentProp.transform.position) < 1.3f;

            bool nowInRange = !targetedOtherCloseProp && 
                              ((distance <= 3.2f && angle <= 85.0f) || 
                               (inWorkstationZone && angle <= 95.0f));

            if (nowInRange != _isInRange)
            {
                _isInRange = nowInRange;
                if (_promptUI != null)
                {
                    _promptUI.ShowEnterPrompt(_isInRange);
                }
            }
        }

        /// <summary>
        /// Checks for 'E' key press to enter Computer Interaction Mode.
        /// </summary>
        private void CheckEnterTrigger()
        {
            if (!_isInRange || _currentMode != WorkstationMode.Exploration) return;

            // If InteractiveProp is focusing the WorkstationChair, let the chair handle the interaction exclusively
            var currentProp = VRRemoteDesktop.Environment.InteractiveProp.CurrentBestProp;
            if (currentProp is VRRemoteDesktop.Environment.WorkstationChair)
            {
                return;
            }

            // Only yield if player is directly targeting another close prop
            if (currentProp != null)
            {
                if (_playerCamera != null && Vector3.Distance(_playerCamera.transform.position, currentProp.transform.position) < 1.3f)
                {
                    return;
                }
            }

            bool interactPressed = false;

            if (_interactAction != null && _interactAction.action != null && _interactAction.action.enabled)
            {
                interactPressed = _interactAction.action.WasPressedThisFrame() || _interactAction.action.triggered;
            }

            var keyboard = Keyboard.current;
            if (!interactPressed && keyboard != null)
            {
                interactPressed = keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame;
            }

            try
            {
                if (!interactPressed && (UnityEngine.Input.GetKeyDown(KeyCode.E) || UnityEngine.Input.GetKeyDown(KeyCode.Return)))
                {
                    interactPressed = true;
                }
            }
            catch {}

            var gamepad = Gamepad.current;
            if (!interactPressed && gamepad != null)
            {
                interactPressed = gamepad.buttonNorth.wasPressedThisFrame || gamepad.buttonSouth.wasPressedThisFrame;
            }

            if (interactPressed)
            {
                Debug.Log("[ComputerInteractionManager] Interacting at workstation - seating player and entering computer mode.");
                EnterComputerMode();
            }
        }

        /// <summary>
        /// Checks for 'ESC' key press to exit Computer Interaction Mode.
        /// </summary>
        private void CheckExitTrigger()
        {
            // Cooldown buffer so user does not immediately exit right after entering
            if (Time.time < _enteredComputerTime + 0.8f) return;

            bool isTyping = (VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance != null && VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance.IsOpen) ||
                            (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                             EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null);

            bool exitPressed = false;

            if (_cancelAction != null && _cancelAction.action != null && _cancelAction.action.enabled)
            {
                exitPressed = _cancelAction.action.WasPressedThisFrame() || _cancelAction.action.triggered;
            }

            var keyboard = Keyboard.current;
            if (!exitPressed && keyboard != null)
            {
                exitPressed = keyboard.escapeKey.wasPressedThisFrame;
                if (!exitPressed && !isTyping && keyboard.eKey.wasPressedThisFrame)
                {
                    exitPressed = true;
                }
            }

            try
            {
                if (!exitPressed)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                    {
                        exitPressed = true;
                    }
                    else if (!isTyping && UnityEngine.Input.GetKeyDown(KeyCode.E))
                    {
                        exitPressed = true;
                    }
                }
            }
            catch {}

            var gamepad = Gamepad.current;
            if (!exitPressed && gamepad != null)
            {
                exitPressed = gamepad.buttonEast.wasPressedThisFrame;
            }

            if (exitPressed)
            {
                // If virtual keyboard is open or an input field is active, dismiss text focus rather than exiting computer mode
                if (VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance != null && VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance.IsOpen)
                {
                    VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance.Close();
                    return;
                }
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
                    EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    return;
                }

                Debug.Log("[ComputerInteractionManager] Exit key pressed ('ESC' or 'E') - initiating smooth return camera transition.");
                ExitComputerMode();
            }
        }

        /// <summary>
        /// Initiates the smooth transition into Computer Interaction Mode.
        /// </summary>
        public void EnterComputerMode()
        {
            if (_currentMode != WorkstationMode.Exploration) return;
            _currentMode = WorkstationMode.TransitioningToComputer;
            OnModeChanged?.Invoke(_currentMode);

            if (_activeTransitionRoutine != null) StopCoroutine(_activeTransitionRoutine);
            _activeTransitionRoutine = StartCoroutine(TransitionToComputerRoutine());
        }

        /// <summary>
        /// Initiates the smooth transition back to Exploration Mode.
        /// </summary>
        public void ExitComputerMode()
        {
            if (_currentMode != WorkstationMode.ComputerInteraction) return;
            _currentMode = WorkstationMode.TransitioningToExploration;
            OnModeChanged?.Invoke(_currentMode);

            if (_activeTransitionRoutine != null) StopCoroutine(_activeTransitionRoutine);
            _activeTransitionRoutine = StartCoroutine(TransitionToExplorationRoutine());
        }

        private IEnumerator TransitionToComputerRoutine()
        {
            // Re-resolve active scene references to ensure non-dangling handles
            ResolveReferences();

            // Hide enter prompt immediately
            if (_promptUI != null) _promptUI.ShowEnterPrompt(false);

            // Keep cursor locked and invisible during the camera glide
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Comprehensively disable player movement and camera look across all controllers in scene
            SetPlayerControlsEnabled(false);

            // Compute target seated camera position & rotation
            GetSeatedFocusTarget(out Vector3 targetPos, out Quaternion targetRot);

            if (_chairTransform == null)
            {
                var chairObj = GameObject.Find("Workstation_GamingChair");
                if (chairObj != null)
                {
                    _chairTransform = chairObj.transform;
                    _chairBasePos = _chairTransform.localPosition;
                }
            }

            // Ensure player camera culls the avatar layer so no avatar parts clip into view
            _playerCamera.cullingMask &= ~(1 << VRRemoteDesktop.Player.PlayerAvatarBody.AVATAR_LAYER);
            _playerCamera.nearClipPlane = 0.03f;

            // Hide avatar upper body in first person when seated so it cannot block the monitors
            VRRemoteDesktop.Player.PlayerAvatarBody.Instance?.SetFirstPersonSeated(true);

            // Safe CharacterController suspension to avoid collision snagging or snapping
            CharacterController cc = _playerController != null ? _playerController.GetComponent<CharacterController>() : null;
            if (cc != null)
            {
                cc.enabled = false;
                cc.detectCollisions = false;
            }

            Vector3 startPlayerPos = _playerController != null ? _playerController.transform.position : _playerCamera.transform.position;
            Quaternion startPlayerRot = _playerController != null ? _playerController.transform.rotation : _playerCamera.transform.rotation;

            // Target player body position at chair base on the floor
            Vector3 targetPlayerPos = _chairTransform != null ? _chairTransform.position : new Vector3(0f, startPlayerPos.y, 0.15f);
            targetPlayerPos.y = startPlayerPos.y;
            Quaternion targetPlayerRot = _chairTransform != null ? _chairTransform.rotation : Quaternion.identity;

            Vector3 startCamPos = _playerCamera.transform.position;
            Quaternion startCamRot = _playerCamera.transform.rotation;
            float startFov = _playerCamera.fieldOfView;
            _explorationFOV = startFov;

            float elapsed = 0f;
            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _transitionDuration);
                float curveT = Mathf.SmoothStep(0f, 1f, t);

                // Smoothly glide player body into the workstation chair
                if (_playerController != null)
                {
                    _playerController.transform.position = Vector3.Lerp(startPlayerPos, targetPlayerPos, curveT);
                    _playerController.transform.rotation = Quaternion.Slerp(startPlayerRot, targetPlayerRot, curveT);
                }

                // Smoothly ease camera to seated eye focus point without detaching
                _playerCamera.transform.position = Vector3.Lerp(startCamPos, targetPos, curveT);
                _playerCamera.transform.rotation = Quaternion.Slerp(startCamRot, targetRot, curveT);
                _playerCamera.fieldOfView = Mathf.Lerp(startFov, _seatedBaseFOV, curveT);

                yield return null;
            }

            if (_playerController != null)
            {
                _playerController.transform.position = targetPlayerPos;
                _playerController.transform.rotation = targetPlayerRot;
            }

            _playerCamera.transform.position = targetPos;
            _playerCamera.transform.rotation = targetRot;
            _playerCamera.fieldOfView = _seatedBaseFOV;

            // Keep CharacterController disabled while seated to prevent physics fights or drifting
            if (cc != null)
            {
                cc.enabled = false;
            }

            _currentMode = WorkstationMode.ComputerInteraction;
            _enteredComputerTime = Time.time;

            // Initialize seated navigation coordinates
            _baseSeatedPos = targetPos;
            _baseSeatedRot = targetRot;
            _targetSeatX = 0f;
            _currentSeatX = 0f;
            _targetSeatZ = 0f;
            _currentSeatZ = 0f;
            _freeLookYaw = 0f;
            _freeLookPitch = 0f;
            _baseFOV = _seatedBaseFOV;

            if (_chairTransform == null)
            {
                var chairObj = GameObject.Find("Workstation_GamingChair");
                if (chairObj != null)
                {
                    _chairTransform = chairObj.transform;
                    _chairBasePos = _chairTransform.localPosition;
                }
            }

            EnsureAuxiliaryInteractables();

            OnModeChanged?.Invoke(_currentMode);
            VRRemoteDesktop.Environment.AchievementManager.Instance?.UnlockAchievement("Desk Jockey", "Entered Computer Interaction Mode at the workstation.");

            // Confine mouse to game window and hide OS pointer so custom virtual neon cursor handles interaction
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;

            // Show exit prompt hint via DesktopModeHUD
            if (DesktopModeHUD.Instance != null) DesktopModeHUD.Instance.Show(true);
            if (_promptUI != null) _promptUI.ShowExitPrompt(true);

            _activeTransitionRoutine = null;
        }

        private IEnumerator TransitionToExplorationRoutine()
        {
            _currentMode = WorkstationMode.TransitioningToExploration;
            OnModeChanged?.Invoke(_currentMode);

            ResolveReferences();

            // Hide exit prompt
            if (DesktopModeHUD.Instance != null) DesktopModeHUD.Instance.Show(false);
            if (_promptUI != null) _promptUI.ShowExitPrompt(false);

            // Keep cursor locked and invisible during the camera return glide
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (_chairTransform == null)
            {
                var chairObj = GameObject.Find("Workstation_GamingChair");
                if (chairObj != null)
                {
                    _chairTransform = chairObj.transform;
                    _chairBasePos = _chairTransform.localPosition;
                }
            }

            Vector3 chairForward = _chairTransform != null ? _chairTransform.forward : Vector3.forward;
            Quaternion chairRot = _chairTransform != null ? _chairTransform.rotation : Quaternion.identity;

            Vector3 startBodyPos = _playerController != null ? _playerController.transform.position : (_chairTransform != null ? _chairTransform.position : Vector3.zero);
            // Safe standing exit position placed smoothly on the floor outside the chair
            Vector3 exitBodyPos = startBodyPos - chairForward * 1.05f;
            exitBodyPos.y = startBodyPos.y;

            Vector3 startCamPos = _playerCamera.transform.position;
            Quaternion startCamRot = _playerCamera.transform.rotation;
            Vector3 targetCamPos = exitBodyPos + new Vector3(0f, 1.65f, 0f);
            Quaternion targetCamRot = chairRot;
            float startFov = _playerCamera.fieldOfView;

            float elapsed = 0f;
            float returnDuration = _transitionDuration * 0.85f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / returnDuration);
                float curveT = Mathf.SmoothStep(0f, 1f, t);

                // Smoothly glide player body out into the open floor
                if (_playerController != null)
                {
                    _playerController.transform.position = Vector3.Lerp(startBodyPos, exitBodyPos, curveT);
                    _playerController.transform.rotation = Quaternion.Slerp(chairRot, targetCamRot, curveT);
                }

                _playerCamera.transform.position = Vector3.Lerp(startCamPos, targetCamPos, curveT);
                _playerCamera.transform.rotation = Quaternion.Slerp(startCamRot, targetCamRot, curveT);
                _playerCamera.fieldOfView = Mathf.Lerp(startFov, _explorationFOV, curveT);

                yield return null;
            }

            if (_playerController != null)
            {
                _playerController.transform.position = exitBodyPos;
                _playerController.transform.rotation = targetCamRot;
            }

            _playerCamera.transform.position = targetCamPos;
            _playerCamera.transform.rotation = targetCamRot;

            // Snap camera cleanly back to local origin under CameraOffset
            if (_cameraOffset != null)
            {
                _playerCamera.transform.SetParent(_cameraOffset, true);
                _playerCamera.transform.localPosition = Vector3.zero;
                _playerCamera.transform.localRotation = Quaternion.identity;
            }

            // Sync transforms and safely re-enable CharacterController on the open floor
            Physics.SyncTransforms();
            CharacterController cc = _playerController != null ? _playerController.GetComponent<CharacterController>() : null;
            if (cc != null)
            {
                cc.enabled = true;
                cc.detectCollisions = true;
            }

            // Sync MouseLook orientation with player body yaw before re-enabling look to prevent any rotation snapping
            if (_mouseLook != null && _playerController != null)
            {
                _mouseLook.SyncOrientation(_playerController.transform.eulerAngles.y, 0f);
            }

            // Reset seated navigation coordinates and restore camera FOV
            _targetSeatX = 0f;
            _currentSeatX = 0f;
            _targetSeatZ = 0f;
            _currentSeatZ = 0f;
            _freeLookYaw = 0f;
            _freeLookPitch = 0f;
            if (_playerCamera != null) _playerCamera.fieldOfView = _explorationFOV;
            if (_chairTransform != null) _chairTransform.localPosition = _chairBasePos;

            // Keep avatar body & head culled in first person exploration mode so FPV is 100% clear
            VRRemoteDesktop.Player.PlayerAvatarBody.Instance?.SetFirstPersonSeated(false);
            if (_playerCamera != null)
            {
                _playerCamera.cullingMask &= ~(1 << VRRemoteDesktop.Player.PlayerAvatarBody.AVATAR_LAYER);
                _playerCamera.cullingMask &= ~(1 << VRRemoteDesktop.Player.PlayerAvatarBody.AVATAR_HEAD_LAYER);
            }

            // Re-enable player movement and camera look across all scene components
            SetPlayerControlsEnabled(true);

            _currentMode = WorkstationMode.Exploration;
            OnModeChanged?.Invoke(_currentMode);

            _activeTransitionRoutine = null;
        }

        private void GetSeatedFocusTarget(out Vector3 position, out Quaternion rotation)
        {
            // If the user manually positioned the SeatedFocusPoint in the scene, respect that exact transform!
            if (_seatedFocusPoint != null)
            {
                position = _seatedFocusPoint.position;
                rotation = _seatedFocusPoint.rotation;
                return;
            }

            if (_chairTransform == null)
            {
                var chairObj = GameObject.Find("Workstation_GamingChair");
                if (chairObj != null)
                {
                    _chairTransform = chairObj.transform;
                    _chairBasePos = _chairTransform.localPosition;
                }
            }

            // Ergonomic first-person seated perspective facing the three monitors:
            // Eye level matches the center monitor (Y = 1.18m).
            // Eye depth sits comfortably at desk front edge (Z = 0.40m), safely in front of chair cushion & backrest.
            float chairX = _chairTransform != null ? _chairTransform.position.x : 0f;
            float chairZ = _chairTransform != null ? _chairTransform.position.z : 0.15f;

            float eyeZ = Mathf.Max(chairZ + 0.25f, 0.40f);
            position = new Vector3(chairX, 1.18f, eyeZ);
            rotation = Quaternion.identity; // Straight ahead at center monitor
        }

        private void EnableAction(InputActionReference actionRef)
        {
            if (actionRef != null && actionRef.action != null && !actionRef.action.enabled)
            {
                actionRef.action.Enable();
            }
        }

        private void DisableAction(InputActionReference actionRef)
        {
            if (actionRef != null && actionRef.action != null && actionRef.action.enabled)
            {
                actionRef.action.Disable();
            }
        }

        /// <summary>
        /// Provides smooth lateral chair sliding, lean-in zoom, free-look swivel,
        /// and multi-monitor focus while seated at the computer workstation.
        /// </summary>
        private void UpdateSeatedWorkstationControls()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var gp = Gamepad.current;

            // Accurate typing check: only treat as typing if virtual keyboard is open or a TMP_InputField is actively focused
            bool isTyping = false;
            if (VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance != null && VRRemoteDesktop.UI.Keyboard.VirtualKeyboard.Instance.IsOpen)
            {
                isTyping = true;
            }
            else if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                var inputField = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
                if (inputField != null && inputField.isFocused)
                {
                    isTyping = true;
                }
            }

            // 1. Sliding laterally between monitors (A / D, Left / Right, Q / E, 1 / 2 / 3, Gamepad, Scroll Wheel)
            bool slideLeft = false;
            bool slideRight = false;
            bool focusLeft = false;
            bool focusCenter = false;
            bool focusRight = false;
            bool zoomIn = false;
            bool zoomOut = false;
            bool toggleZoom = false;

            if (kb != null)
            {
                if (!isTyping)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed || kb.qKey.isPressed) slideLeft = true;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) slideRight = true;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) zoomIn = true;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) zoomOut = true;
                    if (kb.zKey.wasPressedThisFrame) toggleZoom = true;
                }

                // Number keys / function keys can always switch monitors if Alt is held or if not actively typing
                bool altHeld = kb.altKey.isPressed || kb.leftAltKey.isPressed || kb.rightAltKey.isPressed;
                if (!isTyping || altHeld)
                {
                    if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame) focusLeft = true;
                    if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame || kb.f2Key.wasPressedThisFrame) focusCenter = true;
                    if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame || kb.f3Key.wasPressedThisFrame) focusRight = true;
                }
            }

            // Fallback for Legacy Input / Editor Focus Loss
            try
            {
                if (!isTyping)
                {
                    if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.Q)) slideLeft = true;
                    if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) slideRight = true;
                    if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) zoomIn = true;
                    if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) zoomOut = true;
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Z)) toggleZoom = true;
                }

                bool altHeld = UnityEngine.Input.GetKey(KeyCode.LeftAlt) || UnityEngine.Input.GetKey(KeyCode.RightAlt);
                if (!isTyping || altHeld)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad1) || UnityEngine.Input.GetKeyDown(KeyCode.F1)) focusLeft = true;
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad2) || UnityEngine.Input.GetKeyDown(KeyCode.F2)) focusCenter = true;
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3) || UnityEngine.Input.GetKeyDown(KeyCode.Keypad3) || UnityEngine.Input.GetKeyDown(KeyCode.F3)) focusRight = true;
                }
            }
            catch {}

            // Gamepad Navigation
            if (gp != null)
            {
                float stickX = gp.leftStick.x.ReadValue();
                if (stickX < -0.25f || gp.dpad.left.isPressed) slideLeft = true;
                if (stickX > 0.25f || gp.dpad.right.isPressed) slideRight = true;

                float stickY = gp.leftStick.y.ReadValue();
                if (stickY > 0.25f || gp.dpad.up.isPressed) zoomIn = true;
                if (stickY < -0.25f || gp.dpad.down.isPressed) zoomOut = true;

                if (gp.leftShoulder.wasPressedThisFrame) focusLeft = true;
                if (gp.rightShoulder.wasPressedThisFrame) focusRight = true;
            }

            // Mouse Scroll Wheel (Shift+Wheel to slide between monitors horizontally, Wheel to zoom)
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.05f)
                {
                    bool shift = (kb != null && kb.shiftKey.isPressed);
                    try { if (!shift && (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift))) shift = true; } catch {}

                    if (shift)
                    {
                        if (scroll > 0f) slideRight = true;
                        else slideLeft = true;
                    }
                    else
                    {
                        _targetSeatZ += (scroll > 0f ? 0.06f : -0.06f);
                    }
                }
            }

            // Apply direct monitor focus
            if (focusLeft) SlideToMonitor(ActiveDisplayIndex.Left);
            else if (focusCenter) SlideToMonitor(ActiveDisplayIndex.Center);
            else if (focusRight) SlideToMonitor(ActiveDisplayIndex.Right);

            // Apply smooth continuous sliding
            if (slideLeft) _targetSeatX -= _slideSpeed * Time.deltaTime;
            if (slideRight) _targetSeatX += _slideSpeed * Time.deltaTime;

            // Apply zooming
            if (zoomIn) _targetSeatZ += _zoomSpeed * Time.deltaTime;
            if (zoomOut) _targetSeatZ -= _zoomSpeed * Time.deltaTime;
            if (toggleZoom) _targetSeatZ = _targetSeatZ > 0.1f ? 0f : 0.22f;

            _targetSeatX = Mathf.Clamp(_targetSeatX, -_maxSlideDistance, _maxSlideDistance);
            _targetSeatZ = Mathf.Clamp(_targetSeatZ, -0.06f, _maxZoomDistance);

            _currentSeatX = Mathf.Lerp(_currentSeatX, _targetSeatX, Time.deltaTime * 8.5f);
            _currentSeatZ = Mathf.Lerp(_currentSeatZ, _targetSeatZ, Time.deltaTime * 8.5f);

            if (Mathf.Abs(_currentSeatX - _targetSeatX) < 0.0015f) _currentSeatX = _targetSeatX;
            if (Mathf.Abs(_currentSeatZ - _targetSeatZ) < 0.0015f) _currentSeatZ = _targetSeatZ;

            // 3. Free-Look / Swivel by holding Right Mouse Button
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    _freeLookYaw += delta.x * 0.15f;
                    _freeLookPitch -= delta.y * 0.15f;
                    _freeLookYaw = Mathf.Clamp(_freeLookYaw, -85f, 85f);
                    _freeLookPitch = Mathf.Clamp(_freeLookPitch, -45f, 45f);
                }
                else
                {
                    // Middle-click resets free-look smoothly to front
                    if (mouse.middleButton.wasPressedThisFrame)
                    {
                        _freeLookYaw = 0f;
                        _freeLookPitch = 0f;
                    }
                }
            }

            // 4. Ergonomic monitor centering rotation:
            // When moving to Left monitor (-X), rotate camera LEFT (negative Yaw: -25 deg) so the Left screen is full and centered!
            // When moving to Right monitor (+X), rotate camera RIGHT (positive Yaw: +25 deg) so the Right screen is full and centered!
            float monitorCenteringYaw = Mathf.Clamp(_currentSeatX / 1.14f, -1.25f, 1.25f) * 25.0f;

            // Follow curved cockpit desk contour so screen distance remains uniform
            float curvedDepthOffset = (Mathf.Abs(_currentSeatX) / 1.14f) * 0.04f;

            // 5. Update Camera Transform & Optical Zoom FOV
            if (_playerCamera != null)
            {
                Vector3 newPos = _baseSeatedPos + new Vector3(_currentSeatX, 0f, _currentSeatZ + curvedDepthOffset);
                Quaternion newRot = _baseSeatedRot * Quaternion.Euler(_freeLookPitch, monitorCenteringYaw + _freeLookYaw, 0f);

                _playerCamera.transform.position = newPos;
                _playerCamera.transform.rotation = newRot;

                // Optical zoom when leaning in makes fonts and text 1.5x - 2x larger and crisper,
                // while rolling backward opens wide-angle cockpit FOV covering all 3 monitors
                float targetFov = _seatedBaseFOV - (_currentSeatZ * 45f);
                _playerCamera.fieldOfView = Mathf.Lerp(_playerCamera.fieldOfView, targetFov, Time.deltaTime * 8f);
            }

            // 6. Synchronize Physical Gaming Chair Position and Swivel
            if (_chairTransform != null)
            {
                _chairTransform.localPosition = new Vector3(_chairBasePos.x + _currentSeatX, _chairBasePos.y, _chairBasePos.z + _currentSeatZ * 0.45f);
                _chairTransform.localRotation = Quaternion.Euler(0f, monitorCenteringYaw * 0.65f, 0f);
            }

            // 7. Synchronize MultiMonitorManager Active Display Index
            if (MultiMonitorManager.Instance != null)
            {
                ActiveDisplayIndex expectedDisplay = ActiveDisplayIndex.Center;
                if (_currentSeatX < -0.5f) expectedDisplay = ActiveDisplayIndex.Left;
                else if (_currentSeatX > 0.5f) expectedDisplay = ActiveDisplayIndex.Right;

                if (MultiMonitorManager.Instance.ActiveDisplay != expectedDisplay)
                {
                    MultiMonitorManager.Instance.SetActiveDisplay(expectedDisplay);
                }
            }
        }

        public void SlideToMonitor(ActiveDisplayIndex display)
        {
            switch (display)
            {
                case ActiveDisplayIndex.Left: _targetSeatX = -1.14f; break;
                case ActiveDisplayIndex.Center: _targetSeatX = 0f; break;
                case ActiveDisplayIndex.Right: _targetSeatX = 1.14f; break;
            }

            if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.ActiveDisplay != display)
            {
                MultiMonitorManager.Instance.SetActiveDisplay(display);
            }
        }

        public void ToggleZoom()
        {
            _targetSeatZ = _targetSeatZ > 0.1f ? 0f : 0.22f;
        }

        private void EnsureAuxiliaryInteractables()
        {
            var leftScreen = GameObject.Find("LeftScreen");
            if (leftScreen != null && leftScreen.GetComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>() == null)
            {
                var aux = leftScreen.AddComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>();
                aux.Configure("LeftDesktopCanvas", "UILeftCamera", new Color(0f, 1f, 1f, 0.95f));
            }

            var rightScreen = GameObject.Find("RightScreen");
            if (rightScreen != null && rightScreen.GetComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>() == null)
            {
                var aux = rightScreen.AddComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>();
                aux.Configure("RightDesktopCanvas", "UIRightCamera", new Color(0.85f, 0.4f, 1f, 0.95f));
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_monitorTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_monitorTransform.position, _interactionDistance);

                GetSeatedFocusTarget(out Vector3 seatedPos, out Quaternion seatedRot);
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(seatedPos, 0.15f);
                Gizmos.DrawRay(seatedPos, seatedRot * Vector3.forward * 0.5f);
            }
        }
    }
}

