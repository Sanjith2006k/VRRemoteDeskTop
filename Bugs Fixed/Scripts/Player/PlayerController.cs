using UnityEngine;
using UnityEngine.InputSystem;

namespace VRRemoteDesktop.Player
{
    /// <summary>
    /// Handles first-person WASD movement, collision, gravity, and sprint using CharacterController.
    /// Uses modern Unity Input System with fallback for maximum reliability.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [Tooltip("Standard walking speed in meters per second.")]
        [SerializeField] private float _walkSpeed = 4.0f;

        [Tooltip("Sprint speed in meters per second.")]
        [SerializeField] private float _sprintSpeed = 6.5f;

        [Tooltip("Acceleration rate when speeding up.")]
        [SerializeField] private float _acceleration = 14.0f;

        [Tooltip("Deceleration rate when slowing down.")]
        [SerializeField] private float _deceleration = 18.0f;

        [Header("Gravity & Grounding")]
        [Tooltip("Downward acceleration applied when airborne.")]
        [SerializeField] private float _gravity = -18.0f;

        [Tooltip("Downward velocity applied when grounded to keep character securely snapped to slopes/floor.")]
        [SerializeField] private float _groundStickVelocity = -2.0f;

        [Header("Input System Actions (Optional)")]
        [Tooltip("Reference to the Move action (Vector2 composite).")]
        [SerializeField] private InputActionReference _moveAction;

        [Tooltip("Reference to the Sprint action (Button).")]
        [SerializeField] private InputActionReference _sprintAction;

        [Header("State")]
        [SerializeField] private bool _isMovementEnabled = true;

        private CharacterController _characterController;
        private Vector3 _currentHorizontalVelocity = Vector3.zero;
        private float _verticalVelocity = 0.0f;
        private bool _isSprinting = false;

        public bool IsMovementEnabled
        {
            get => _isMovementEnabled;
            set
            {
                _isMovementEnabled = value;
                if (!_isMovementEnabled)
                {
                    _currentHorizontalVelocity = Vector3.zero;
                }
            }
        }

        public float WalkSpeed
        {
            get => _walkSpeed;
            set => _walkSpeed = Mathf.Max(0.5f, value);
        }

        public float SprintSpeed
        {
            get => _sprintSpeed;
            set => _sprintSpeed = Mathf.Max(_walkSpeed, value);
        }

        public bool IsGrounded => _characterController != null && _characterController.isGrounded;
        public bool IsSprinting => _isSprinting;
        public float CurrentSpeed => _currentHorizontalVelocity.magnitude;
        public Vector3 Velocity => _characterController != null ? _characterController.velocity : Vector3.zero;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            ConfigureCharacterController();
            EnsureAvatarAndFootsteps();
        }

        private void EnsureAvatarAndFootsteps()
        {
            // Ensure AudioListener on player camera so all footsteps, UI sounds, and media are heard
            AudioListener.volume = 1f;
            AudioListener.pause = false;
            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
            if (listeners == null || listeners.Length == 0)
            {
                Camera cam = GetComponentInChildren<Camera>() ?? Camera.main;
                if (cam != null && cam.GetComponent<AudioListener>() == null)
                {
                    cam.gameObject.AddComponent<AudioListener>();
                    Debug.Log($"[PlayerController] Attached AudioListener to {cam.name}.");
                }
            }

            if (GetComponent<PlayerFootsteps>() == null)
            {
                gameObject.AddComponent<PlayerFootsteps>();
            }
            if (GetComponent<PlayerAvatarBody>() == null)
            {
                gameObject.AddComponent<PlayerAvatarBody>();
            }
        }

        private void OnEnable()
        {
            EnableAction(_moveAction);
            EnableAction(_sprintAction);
        }

        private void OnDisable()
        {
            DisableAction(_moveAction);
            DisableAction(_sprintAction);
        }

        private void ConfigureCharacterController()
        {
            if (_characterController == null) return;

            // Standard human-scale collision bounds (1.8m height, 0.35m radius)
            _characterController.height = 1.8f;
            _characterController.radius = 0.35f;
            _characterController.center = new Vector3(0f, 0.9f, 0f);
            _characterController.stepOffset = 0.3f;
            _characterController.slopeLimit = 45f;
            _characterController.minMoveDistance = 0f;
        }

        private void Update()
        {
            ApplyMovement();
        }

        private void ApplyMovement()
        {
            if (_characterController == null) return;

            // In Computer Interaction Mode, Theater Cinema Mode, or during smooth transitions, player cannot move
            bool isSeatedAtComputer = VRRemoteDesktop.Computer.ComputerInteractionManager.Instance != null &&
                                      VRRemoteDesktop.Computer.ComputerInteractionManager.Instance.IsInComputerMode;
            bool isSeatedInTheater = VRRemoteDesktop.Environment.TheaterCinemaController.Instance != null &&
                                     VRRemoteDesktop.Environment.TheaterCinemaController.Instance.IsInCinemaMode;
            bool isTransitioningAtComputer = VRRemoteDesktop.Computer.ComputerInteractionManager.Instance != null &&
                                             VRRemoteDesktop.Computer.ComputerInteractionManager.Instance.IsTransitioning;
            bool isTransitioningInTheater = VRRemoteDesktop.Environment.TheaterCinemaController.Instance != null &&
                                            VRRemoteDesktop.Environment.TheaterCinemaController.Instance.IsTransitioning;

            if (isSeatedAtComputer || isSeatedInTheater || isTransitioningAtComputer || isTransitioningInTheater)
            {
                _currentHorizontalVelocity = Vector3.zero;
                _verticalVelocity = 0f;
                return;
            }

            // Self-healing recovery: ensure movement and CharacterController are active in Exploration mode
            if (!_isMovementEnabled) _isMovementEnabled = true;
            if (!_characterController.enabled)
            {
                _characterController.enabled = true;
                _characterController.detectCollisions = true;
            }

            Vector2 inputVector = ReadMoveInput();
            bool wantsSprint = ReadSprintInput();

            _isSprinting = wantsSprint && inputVector.sqrMagnitude > 0.01f;
            float targetSpeed = inputVector.sqrMagnitude > 0.01f ? (_isSprinting ? _sprintSpeed : _walkSpeed) : 0f;

            // Calculate movement direction relative to the player's horizontal orientation
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 targetVelocity = (forward * inputVector.y + right * inputVector.x).normalized * targetSpeed;

            // Smooth horizontal acceleration / deceleration
            float accelRate = targetSpeed > _currentHorizontalVelocity.magnitude ? _acceleration : _deceleration;
            _currentHorizontalVelocity = Vector3.MoveTowards(_currentHorizontalVelocity, targetVelocity, accelRate * Time.deltaTime);

            // Handle vertical velocity & gravity
            if (_characterController.isGrounded)
            {
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = _groundStickVelocity;
                }
            }
            else
            {
                _verticalVelocity += _gravity * Time.deltaTime;
            }

            // Combine into total motion
            Vector3 totalMotion = _currentHorizontalVelocity;
            totalMotion.y = _verticalVelocity;

            _characterController.Move(totalMotion * Time.deltaTime);
        }

        private Vector2 ReadMoveInput()
        {
            // 1. Try modern InputActionReference if bound
            if (_moveAction != null && _moveAction.action != null && _moveAction.action.enabled)
            {
                Vector2 val = _moveAction.action.ReadValue<Vector2>();
                if (val.sqrMagnitude > 0.001f) return val;
            }

            // 2. Fallback to modern Keyboard.current
            float x = 0f;
            float y = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            }

            // 3. Fallback to legacy InputManager for 100% reliability
            try
            {
                if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow)) y += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow)) y -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow)) x += 1f;
            }
            catch {}

            Vector2 combined = new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
            return combined.sqrMagnitude > 0.001f ? combined.normalized : Vector2.zero;
        }

        private bool ReadSprintInput()
        {
            if (_sprintAction != null && _sprintAction.action != null && _sprintAction.action.enabled)
            {
                if (_sprintAction.action.IsPressed()) return true;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
            {
                return true;
            }

            try
            {
                if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift))
                {
                    return true;
                }
            }
            catch {}

            return false;
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
