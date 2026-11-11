using UnityEngine;
using UnityEngine.InputSystem;

namespace VRRemoteDesktop.Player
{
    /// <summary>
    /// Handles first-person mouse look, pitch clamping, sensitivity, and cursor locking / unlocking with ESC.
    /// Rotates the player body on Y (Yaw) and the camera on X (Pitch).
    /// </summary>
    public class MouseLook : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("The player root or body transform rotated horizontally (Yaw).")]
        [SerializeField] private Transform _playerBody;

        [Tooltip("The camera transform rotated vertically (Pitch).")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Sensitivity & Limits")]
        [Tooltip("Base sensitivity factor.")]
        [SerializeField] private float _sensitivity = 1.0f;

        [Tooltip("Multiplier to calibrate raw mouse delta to comfortable degrees.")]
        [SerializeField] private float _mouseMultiplier = 0.12f;

        [Tooltip("Minimum vertical angle (looking down).")]
        [SerializeField] private float _minPitch = -85.0f;

        [Tooltip("Maximum vertical angle (looking up).")]
        [SerializeField] private float _maxPitch = 85.0f;

        [Tooltip("Invert vertical look axis.")]
        [SerializeField] private bool _invertY = false;

        [Header("Smoothing")]
        [Tooltip("Optionally interpolate mouse look for smoother camera motion.")]
        [SerializeField] private bool _enableSmoothing = true;

        [Tooltip("Smoothing sharpness factor.")]
        [SerializeField] private float _smoothSpeed = 25.0f;

        [Header("Cursor Settings")]
        [Tooltip("Lock and hide cursor when scene begins.")]
        [SerializeField] private bool _lockCursorOnStart = true;

        [Tooltip("Allow ESC to unlock the cursor.")]
        [SerializeField] private bool _unlockOnEscape = true;

        [Header("Input System Actions (Optional)")]
        [Tooltip("Reference to the Look action (Vector2 delta).")]
        [SerializeField] private InputActionReference _lookAction;

        [Tooltip("Reference to the Cancel/Escape action.")]
        [SerializeField] private InputActionReference _cancelAction;

        private float _pitch = 0.0f;
        private float _yaw = 0.0f;
        private Vector2 _smoothedDelta = Vector2.zero;
        private bool _isLookEnabled = true;
        private bool _isCursorLocked = false;

        public bool IsLookEnabled
        {
            get => _isLookEnabled;
            set => _isLookEnabled = value;
        }

        public bool IsCursorLocked => _isCursorLocked;

        public float Sensitivity
        {
            get => _sensitivity;
            set => _sensitivity = Mathf.Max(0.05f, value);
        }

        public bool InvertY
        {
            get => _invertY;
            set => _invertY = value;
        }

        public bool EnableSmoothing
        {
            get => _enableSmoothing;
            set => _enableSmoothing = value;
        }

        private void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Time.maximumDeltaTime = 0.05f;

            // Auto-resolve references if not explicitly set
            if (_playerBody == null)
            {
                _playerBody = transform.root != transform ? transform.root : transform;
            }

            if (_cameraTransform == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) _cameraTransform = cam.transform;
                else _cameraTransform = transform;
            }

            // Initialize pitch and yaw from existing scene rotation
            if (_playerBody != null)
            {
                _yaw = _playerBody.eulerAngles.y;
            }

            if (_cameraTransform != null)
            {
                float initialPitch = _cameraTransform.localEulerAngles.x;
                if (initialPitch > 180f) initialPitch -= 360f;
                _pitch = Mathf.Clamp(initialPitch, _minPitch, _maxPitch);
            }
        }

        private void Start()
        {
            // Guarantee rock-solid frame pacing and eliminate choppiness
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 120;

            if (_lockCursorOnStart)
            {
                SetCursorLock(true);
            }
        }

        private void OnEnable()
        {
            EnableAction(_lookAction);
            EnableAction(_cancelAction);
        }

        private void OnDisable()
        {
            DisableAction(_lookAction);
            DisableAction(_cancelAction);
        }

        private void Update()
        {
            if (IsInSpecialInteractionMode())
            {
                _isLookEnabled = false;
                _isCursorLocked = false;
                return;
            }

            if (!_isLookEnabled) return;
            HandleCursorToggle();
        }

        private void LateUpdate()
        {
            if (IsInSpecialInteractionMode())
            {
                return;
            }

            // Skip updating look when game is paused
            if (Time.timeScale <= 0.001f) return;

            // Camera look runs in LateUpdate after PlayerController.Update moves the character
            if (!_isLookEnabled || !_isCursorLocked) return;

            ApplyLook();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                if (!IsInSpecialInteractionMode() && _isLookEnabled)
                {
                    SetCursorLock(true);
                    if (_playerBody != null)
                    {
                        _yaw = _playerBody.eulerAngles.y;
                    }
                    _smoothedDelta = Vector2.zero;
                }
            }
            else
            {
                _smoothedDelta = Vector2.zero;
            }
        }

        private bool IsInSpecialInteractionMode()
        {
            if (VRRemoteDesktop.Computer.ComputerInteractionManager.Instance != null &&
                VRRemoteDesktop.Computer.ComputerInteractionManager.Instance.IsInComputerMode)
            {
                return true;
            }

            if (VRRemoteDesktop.Environment.TheaterCinemaController.Instance != null &&
                VRRemoteDesktop.Environment.TheaterCinemaController.Instance.IsInCinemaMode)
            {
                return true;
            }

            return false;
        }

        private void HandleCursorToggle()
        {
            // Do not re-lock cursor if in computer or theater mode
            if (IsInSpecialInteractionMode())
            {
                return;
            }

            // Unlock with ESC key
            if (_unlockOnEscape && CheckEscapeTriggered())
            {
                SetCursorLock(false);
            }

            // Re-lock when clicking the game view
            if ((!_isCursorLocked || Cursor.lockState != CursorLockMode.Locked) && CheckClickTriggered())
            {
                SetCursorLock(true);
            }
        }

        private bool CheckEscapeTriggered()
        {
            if (_cancelAction != null && _cancelAction.action != null && _cancelAction.action.enabled)
            {
                if (_cancelAction.action.WasPressedThisFrame()) return true;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                return true;
            }

            return false;
        }

        private bool CheckClickTriggered()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return true;
            }
            return false;
        }

        private void ApplyLook()
        {
            Vector2 rawDelta = ReadLookDelta();

            // Apply frame-rate independent exponential smoothing
            Vector2 lookDelta;
            if (_enableSmoothing)
            {
                float dampFactor = 1.0f - Mathf.Exp(-_smoothSpeed * Time.deltaTime);
                _smoothedDelta = Vector2.Lerp(_smoothedDelta, rawDelta, dampFactor);
                lookDelta = _smoothedDelta;
            }
            else
            {
                lookDelta = rawDelta;
            }

            float mouseX = lookDelta.x * _sensitivity * _mouseMultiplier;
            float mouseY = lookDelta.y * _sensitivity * _mouseMultiplier * (_invertY ? -1.0f : 1.0f);

            // Yaw rotates player body around Y
            _yaw += mouseX;
            if (_playerBody != null)
            {
                _playerBody.rotation = Quaternion.Euler(0.0f, _yaw, 0.0f);
            }

            // Pitch rotates camera around X, clamped
            _pitch = Mathf.Clamp(_pitch - mouseY, _minPitch, _maxPitch);
            if (_cameraTransform != null)
            {
                _cameraTransform.localRotation = Quaternion.Euler(_pitch, 0.0f, 0.0f);
            }
        }

        private Vector2 ReadLookDelta()
        {
            if (_lookAction != null && _lookAction.action != null && _lookAction.action.enabled)
            {
                return _lookAction.action.ReadValue<Vector2>();
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.delta.ReadValue();
            }

            return Vector2.zero;
        }

        public void SetCursorLock(bool locked)
        {
            if (locked && VRRemoteDesktop.Computer.ComputerInteractionManager.Instance != null &&
                VRRemoteDesktop.Computer.ComputerInteractionManager.Instance.IsInComputerMode)
            {
                _isCursorLocked = false;
                return;
            }
            _isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        /// <summary>
        /// Synchronizes the internal yaw and pitch angles without causing camera snapping.
        /// </summary>
        public void SyncOrientation(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
            _smoothedDelta = Vector2.zero;
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
    }
}
