using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using Unity.XR.CoreUtils;
using VRRemoteDesktop.Player;
using VRRemoteDesktop.Computer;

namespace VRRemoteDesktop.XR
{
    /// <summary>
    /// Master XR Player Rig Manager fulfilling Section 4 of Master Requirements.
    /// Integrates OpenXR and XR Interaction Toolkit into the existing scene:
    /// - Detects XR headset availability at runtime
    /// - Manages 6DoF head tracking and Left/Right motion controllers
    /// - Configures high-precision Ray Interactors with visible laser lines and reticles
    /// - Positions the user ergonomically facing the three-monitor workstation
    /// - Supports both Seated and Standing VR modes with height recalibration
    /// - Seamlessly falls back to Desktop Exploration (WASD + MouseLook) when no VR headset is detected
    /// </summary>
    public class XRPlayerRigManager : MonoBehaviour
    {
        public static XRPlayerRigManager Instance { get; private set; }

        [Header("Rig References")]
        [SerializeField] private XROrigin _xrOrigin;
        [SerializeField] private XRInteractionManager _interactionManager;
        [SerializeField] private Camera _xrCamera;
        [SerializeField] private Transform _cameraOffset;

        [Header("Controller Interactors")]
        [SerializeField] private XRRayInteractor _rightRayInteractor;
        [SerializeField] private XRRayInteractor _leftRayInteractor;
        [SerializeField] private XRInteractorLineVisual _rightLineVisual;
        [SerializeField] private XRInteractorLineVisual _leftLineVisual;

        [Header("Desktop Fallback References")]
        [SerializeField] private GameObject _desktopPlayerObj;
        [SerializeField] private PlayerController _desktopPlayerController;
        [SerializeField] private MouseLook _desktopMouseLook;

        [Header("Ergonomics & Positioning")]
        [Tooltip("Target seated anchor position facing the monitors.")]
        [SerializeField] private Vector3 _seatedWorkstationPos = new Vector3(0f, 0f, 0.15f);
        [SerializeField] private bool _startInSeatedMode = true;
        [SerializeField] private float _seatedEyeHeight = 1.18f;
        [SerializeField] private float _standingEyeHeight = 1.65f;

        [Header("Laser Line Styling")]
        [SerializeField] private Color _laserColor = new Color(0f, 0.9f, 1f, 0.8f);
        [SerializeField] private Color _laserHitColor = new Color(0f, 1f, 0.5f, 0.95f);
        [SerializeField] private float _laserLineWidth = 0.006f;

        private bool _isVRActive = false;

        public bool IsVRActive => _isVRActive;
        public XROrigin XROrigin => _xrOrigin;
        public XRInteractionManager InteractionManager => _interactionManager;
        public XRRayInteractor RightRayInteractor => _rightRayInteractor;
        public XRRayInteractor LeftRayInteractor => _leftRayInteractor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureXRInteractionManager();
            ResolveReferences();
            EvaluateXRDeviceState();
        }

        private void Start()
        {
            EnsureXRInteractionManager();
            ConfigureControllerVisuals();
            ApplyInitialPositioning();
        }

        public XRInteractionManager EnsureXRInteractionManager()
        {
            if (_interactionManager == null)
            {
                _interactionManager = Object.FindAnyObjectByType<XRInteractionManager>();
            }

            if (_interactionManager == null)
            {
                GameObject mgrObj = new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
                _interactionManager = mgrObj.GetComponent<XRInteractionManager>();
                Debug.Log("[XRPlayerRigManager] Instantiated master XRInteractionManager in scene.");
            }

            return _interactionManager;
        }

        private void ResolveReferences()
        {
            if (_xrOrigin == null)
            {
                _xrOrigin = Object.FindAnyObjectByType<XROrigin>();
            }

            if (_desktopPlayerObj == null)
            {
                _desktopPlayerObj = GameObject.Find("Player");
            }

            if (_desktopPlayerObj != null)
            {
                if (_desktopPlayerController == null) _desktopPlayerController = _desktopPlayerObj.GetComponent<PlayerController>();
                if (_desktopMouseLook == null) _desktopMouseLook = _desktopPlayerObj.GetComponentInChildren<MouseLook>();
            }

            if (_xrOrigin != null)
            {
                if (_cameraOffset == null && _xrOrigin.CameraFloorOffsetObject != null)
                {
                    _cameraOffset = _xrOrigin.CameraFloorOffsetObject.transform;
                }
                if (_xrCamera == null && _xrOrigin.Camera != null)
                {
                    _xrCamera = _xrOrigin.Camera;
                }

                // Locate interactors under controllers
                if (_rightRayInteractor == null)
                {
                    var interactors = _xrOrigin.GetComponentsInChildren<XRRayInteractor>(true);
                    foreach (var interactor in interactors)
                    {
                        if (interactor.gameObject.name.Contains("Right") || interactor.transform.parent.name.Contains("Right"))
                        {
                            _rightRayInteractor = interactor;
                        }
                        else if (interactor.gameObject.name.Contains("Left") || interactor.transform.parent.name.Contains("Left"))
                        {
                            _leftRayInteractor = interactor;
                        }
                    }
                }
            }
        }

        public void EvaluateXRDeviceState()
        {
            // Detect if an active XR display subsystem or OpenXR loader is running
            bool xrDevicePresent = false;

            var displays = new List<UnityEngine.XR.XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays)
            {
                if (d.running)
                {
                    xrDevicePresent = true;
                    break;
                }
            }

            if (!xrDevicePresent)
            {
                xrDevicePresent = UnityEngine.XR.XRSettings.isDeviceActive;
            }

            _isVRActive = xrDevicePresent;

            if (_isVRActive)
            {
                Debug.Log("[XRPlayerRigManager] VR Headset / OpenXR detected! Enabling 6DoF XR Player Rig.");
                EnableVRMode();
            }
            else
            {
                Debug.Log("[XRPlayerRigManager] No active VR headset detected. Enabling Desktop Mode (WASD + MouseLook) with XR Simulation fallback.");
                EnableDesktopFallback();
            }
        }

        private void EnableVRMode()
        {
            if (_desktopPlayerObj != null)
            {
                _desktopPlayerObj.SetActive(false);
            }

            if (_xrOrigin != null)
            {
                _xrOrigin.gameObject.SetActive(true);
            }

            if (_xrCamera != null)
            {
                _xrCamera.gameObject.SetActive(true);
                SyncAudioListenerToCamera(_xrCamera);
            }

            // In VR, the user looks directly at monitors with head tracking
            if (ComputerInteractionManager.Instance != null)
            {
                // Ensure computer interaction mode is engaged so the virtual monitors are interactable
                ComputerInteractionManager.Instance.EnterComputerMode();
            }
        }

        private void EnableDesktopFallback()
        {
            // Deactivate VR origin so its camera does not conflict or render over desktop camera
            if (_xrOrigin != null)
            {
                _xrOrigin.gameObject.SetActive(false);
            }

            // Keep desktop player active
            if (_desktopPlayerObj != null)
            {
                _desktopPlayerObj.SetActive(true);
                if (_desktopPlayerController != null) _desktopPlayerController.IsMovementEnabled = true;
                if (_desktopMouseLook != null)
                {
                    _desktopMouseLook.IsLookEnabled = true;
                    _desktopMouseLook.SetCursorLock(true);
                }

                Camera desktopCam = _desktopPlayerObj.GetComponentInChildren<Camera>();
                if (desktopCam != null)
                {
                    desktopCam.gameObject.SetActive(true);
                    SyncAudioListenerToCamera(desktopCam);
                }
            }
        }

        public static void SyncAudioListenerToCamera(Camera targetCam)
        {
            if (targetCam == null) return;

            AudioListener.volume = 1.0f;
            AudioListener.pause = false;

            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            foreach (var l in listeners)
            {
                if (l != null && l.gameObject != targetCam.gameObject)
                {
                    l.enabled = false;
                }
            }

            var targetListener = targetCam.GetComponent<AudioListener>();
            if (targetListener == null) targetListener = targetCam.gameObject.AddComponent<AudioListener>();
            targetListener.enabled = true;
            Debug.Log($"[XRPlayerRigManager] AudioListener synchronized to {targetCam.name} (Volume=1.0, Active=true).");
        }

        public void ConfigureControllerVisuals()
        {
            ConfigureSingleRayVisual(_rightRayInteractor, ref _rightLineVisual, "RightLaser");
            ConfigureSingleRayVisual(_leftRayInteractor, ref _leftLineVisual, "LeftLaser");
        }

        private void ConfigureSingleRayVisual(XRRayInteractor interactor, ref XRInteractorLineVisual lineVisual, string lineName)
        {
            if (interactor == null) return;

            if (lineVisual == null)
            {
                lineVisual = interactor.GetComponent<XRInteractorLineVisual>();
            }

            if (lineVisual == null)
            {
                lineVisual = interactor.gameObject.AddComponent<XRInteractorLineVisual>();
            }

            lineVisual.lineWidth = _laserLineWidth;
            lineVisual.validColorGradient = CreateGradient(_laserColor, _laserHitColor);
            lineVisual.invalidColorGradient = CreateGradient(new Color(0.1f, 0.3f, 0.4f, 0.3f), new Color(0.1f, 0.3f, 0.4f, 0.1f));
            lineVisual.smoothMovement = true;
        }

        private Gradient CreateGradient(Color start, Color end)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(end.a, 1f) }
            );
            return g;
        }

        public void ApplyInitialPositioning()
        {
            if (_xrOrigin != null)
            {
                _xrOrigin.transform.position = _seatedWorkstationPos;
                _xrOrigin.transform.rotation = Quaternion.identity;

                if (_xrOrigin.CameraFloorOffsetObject != null)
                {
                    _xrOrigin.CameraFloorOffsetObject.transform.localPosition = new Vector3(0f, _startInSeatedMode ? _seatedEyeHeight : _standingEyeHeight, 0f);
                }
            }
        }

        /// <summary>
        /// Toggles between Seated ergonomic workstation mode and Standing room-scale mode.
        /// </summary>
        public void SetSeatedMode(bool seated)
        {
            if (_xrOrigin != null && _xrOrigin.CameraFloorOffsetObject != null)
            {
                float targetY = seated ? _seatedEyeHeight : _standingEyeHeight;
                Vector3 cur = _xrOrigin.CameraFloorOffsetObject.transform.localPosition;
                _xrOrigin.CameraFloorOffsetObject.transform.localPosition = new Vector3(cur.x, targetY, cur.z);
            }
        }

        /// <summary>
        /// Recalibrates VR headset view centering to face directly at the central workstation monitor.
        /// </summary>
        public void RecalibrateCenterView()
        {
            if (_xrOrigin != null && _xrCamera != null)
            {
                Vector3 camRot = _xrCamera.transform.localEulerAngles;
                _xrOrigin.transform.rotation = Quaternion.Euler(0f, -camRot.y, 0f);
                _xrOrigin.transform.position = _seatedWorkstationPos;
                Debug.Log("[XRPlayerRigManager] View recalibrated towards central workstation monitor.");
            }
        }
    }
}
