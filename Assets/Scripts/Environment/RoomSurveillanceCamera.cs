using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Player;

namespace VRRemoteDesktop.Environment
{
    public enum SurveillanceCameraMode
    {
        RoomCCTV,       // Wide overhead room perspective showing player walking around
        DesktopWebcam,  // Mounted on center monitor looking directly at the seated/standing player
        ThirdPersonFollow // Dynamic chase cam tracking behind the player
    }

    /// <summary>
    /// CCTV Room Surveillance & Live Desktop Webcam system.
    /// Captures the player character moving around the room and seated at the workstation,
    /// streaming live to a high-resolution RenderTexture for the Center PC Desktop Window.
    /// Supports dynamic multi-angle switching (Overhead CCTV, Monitor Webcam, 3rd-Person Follow).
    /// </summary>
    public class RoomSurveillanceCamera : MonoBehaviour
    {
        private static RoomSurveillanceCamera _instance;
        public static RoomSurveillanceCamera Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<RoomSurveillanceCamera>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RoomSurveillanceCameraRig");
                        _instance = go.AddComponent<RoomSurveillanceCamera>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [Header("Render Texture")]
        [SerializeField] private RenderTexture _cctvRenderTexture;

        [Header("Camera Rig")]
        private Camera _surveillanceCam;
        private SurveillanceCameraMode _currentMode = SurveillanceCameraMode.RoomCCTV;

        // Angle Presets
        private Vector3 _cctvPos = new Vector3(2.4f, 2.7f, -2.6f);
        private Quaternion _cctvRot = Quaternion.Euler(26f, -42f, 0f);

        private Vector3 _webcamPos = new Vector3(0f, 1.48f, 0.84f);
        private Quaternion _webcamRot = Quaternion.Euler(14f, 180f, 0f);

        private Transform _playerTransform;
        private float _timeCounter = 0f;

        public SurveillanceCameraMode CurrentMode => _currentMode;
        public RenderTexture FeedTexture => _cctvRenderTexture;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureRenderTexture();
            SetupCamera();
        }

        private void Start()
        {
            ResolvePlayer();
            SetCameraMode(SurveillanceCameraMode.RoomCCTV);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsureRenderTexture()
        {
            if (_cctvRenderTexture == null)
            {
                _cctvRenderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                _cctvRenderTexture.name = "CCTV_LiveFeed_RT";
                _cctvRenderTexture.antiAliasing = 2;
                _cctvRenderTexture.filterMode = FilterMode.Bilinear;
                _cctvRenderTexture.Create();
            }
        }

        private void SetupCamera()
        {
            GameObject camObj = new GameObject("Surveillance_EyeCam");
            camObj.transform.SetParent(transform, false);

            _surveillanceCam = camObj.AddComponent<Camera>();
            _surveillanceCam.targetTexture = _cctvRenderTexture;
            _surveillanceCam.fieldOfView = 68f;
            _surveillanceCam.nearClipPlane = 0.1f;
            _surveillanceCam.farClipPlane = 50f;
            _surveillanceCam.clearFlags = CameraClearFlags.Skybox;
            _surveillanceCam.renderingPath = RenderingPath.Forward;
            _surveillanceCam.allowHDR = true;
            _surveillanceCam.allowMSAA = true;
            _surveillanceCam.cullingMask = -1; // Render all layers including PlayerAvatar and Head
            _surveillanceCam.cullingMask |= (1 << PlayerAvatarBody.AVATAR_LAYER);
            _surveillanceCam.cullingMask |= (1 << PlayerAvatarBody.AVATAR_HEAD_LAYER);

            // Audio listener should stay disabled on feed camera
            var listener = camObj.GetComponent<AudioListener>();
            if (listener != null) Destroy(listener);
        }

        private void ResolvePlayer()
        {
            if (_playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (_surveillanceCam == null) return;
            _surveillanceCam.cullingMask |= (1 << PlayerAvatarBody.AVATAR_LAYER);
            _surveillanceCam.cullingMask |= (1 << PlayerAvatarBody.AVATAR_HEAD_LAYER);
            _timeCounter += Time.deltaTime;

            if (_playerTransform == null) ResolvePlayer();

            bool inComputer = ComputerInteractionManager.Instance != null && ComputerInteractionManager.Instance.IsInComputerMode;

            switch (_currentMode)
            {
                case SurveillanceCameraMode.RoomCCTV:
                    // Subtle security camera patrol pan
                    float pan = Mathf.Sin(_timeCounter * 0.25f) * 4.0f;
                    _surveillanceCam.transform.position = _cctvPos;
                    _surveillanceCam.transform.rotation = _cctvRot * Quaternion.Euler(0f, pan, 0f);
                    _surveillanceCam.fieldOfView = 70f;
                    break;

                case SurveillanceCameraMode.DesktopWebcam:
                    // Mounted on top of monitor, tracking player head
                    _surveillanceCam.transform.position = _webcamPos;
                    Vector3 targetLook;

                    if (inComputer)
                    {
                        if (PlayerAvatarBody.Instance != null && PlayerAvatarBody.Instance.HeadTransform != null)
                        {
                            targetLook = PlayerAvatarBody.Instance.HeadTransform.position;
                        }
                        else
                        {
                            var chair = GameObject.Find("Workstation_GamingChair");
                            Vector3 cPos = chair != null ? chair.transform.position : new Vector3(0f, 0.12f, -0.15f);
                            targetLook = new Vector3(cPos.x, 1.18f, cPos.z + 0.08f);
                        }
                    }
                    else if (_playerTransform != null)
                    {
                        targetLook = _playerTransform.position + Vector3.up * 1.45f;
                    }
                    else
                    {
                        targetLook = new Vector3(0f, 1.18f, 0f);
                    }

                    Vector3 dir = (targetLook - _webcamPos).normalized;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        _surveillanceCam.transform.rotation = Quaternion.Slerp(_surveillanceCam.transform.rotation, Quaternion.LookRotation(dir, Vector3.up), Time.deltaTime * 6f);
                    }
                    _surveillanceCam.fieldOfView = 62f;
                    break;

                case SurveillanceCameraMode.ThirdPersonFollow:
                    // Dynamic chase cam behind player (or over-the-shoulder when seated at workstation)
                    if (inComputer)
                    {
                        var chair = GameObject.Find("Workstation_GamingChair");
                        Vector3 cPos = chair != null ? chair.transform.position : new Vector3(0f, 0.12f, -0.15f);
                        Vector3 seatedCamPos = new Vector3(cPos.x - 0.70f, 1.45f, cPos.z - 1.10f);
                        _surveillanceCam.transform.position = Vector3.Lerp(_surveillanceCam.transform.position, seatedCamPos, Time.deltaTime * 8f);

                        Vector3 lookAtSeated = new Vector3(cPos.x, 1.15f, cPos.z + 0.45f);
                        _surveillanceCam.transform.rotation = Quaternion.Slerp(_surveillanceCam.transform.rotation, Quaternion.LookRotation((lookAtSeated - _surveillanceCam.transform.position).normalized, Vector3.up), Time.deltaTime * 10f);
                        _surveillanceCam.fieldOfView = 65f;
                    }
                    else if (_playerTransform != null)
                    {
                        Vector3 targetPos = _playerTransform.position - _playerTransform.forward * 1.8f + Vector3.up * 1.6f + _playerTransform.right * 0.35f;
                        _surveillanceCam.transform.position = Vector3.Lerp(_surveillanceCam.transform.position, targetPos, Time.deltaTime * 8f);

                        Vector3 lookAtPoint = _playerTransform.position + Vector3.up * 1.1f;
                        _surveillanceCam.transform.rotation = Quaternion.Slerp(_surveillanceCam.transform.rotation, Quaternion.LookRotation((lookAtPoint - _surveillanceCam.transform.position).normalized, Vector3.up), Time.deltaTime * 10f);
                        _surveillanceCam.fieldOfView = 65f;
                    }
                    break;
            }
        }

        public void SetCameraMode(SurveillanceCameraMode mode)
        {
            _currentMode = mode;
            Debug.Log($"[RoomSurveillanceCamera] Switched live camera feed mode to: {_currentMode}");
        }

        public void CycleCameraMode()
        {
            int next = ((int)_currentMode + 1) % 3;
            SetCameraMode((SurveillanceCameraMode)next);
        }
    }
}
