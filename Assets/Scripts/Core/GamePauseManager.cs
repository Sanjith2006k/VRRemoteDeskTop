using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Player;

namespace VRRemoteDesktop.Core
{
    /// <summary>
    /// Master lifecycle manager for Game Pause and Resume (Section 4 & 5).
    /// Handles:
    /// 1. Pausing game systems cleanly with 'P' key or Pause button.
    /// 2. Freezing Time.timeScale and AudioListener without destroying state.
    /// 3. Preserving exact player position, camera orientation, and seated state across multiple pause/resume cycles.
    /// 4. Preventing cursor lock desynchronization when the game window loses focus or sits idle for 5-10 minutes.
    /// 5. Clamping Time.maximumDeltaTime and setting SleepTimeout.NeverSleep to prevent OS sleep or delta-time physics explosions.
    /// 6. Re-synchronizing physics, mouse-look, and audio listeners with zero glitching or snapping.
    /// </summary>
    public class GamePauseManager : MonoBehaviour
    {
        private static GamePauseManager _instance;
        public static GamePauseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<GamePauseManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GamePauseManager");
                        _instance = go.AddComponent<GamePauseManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("State")]
        [SerializeField] private bool _isPaused = false;
        public bool IsPaused => _isPaused;

        [Header("UI Feedback")]
        [SerializeField] private GameObject _pauseOverlayUI;
        private CanvasGroup _overlayCanvasGroup;

        // Cached state for rock-solid preservation across pause cycles
        private Vector3 _cachedPlayerPos;
        private Quaternion _cachedPlayerRot;
        private Vector3 _cachedCamPos;
        private Quaternion _cachedCamRot;
        private float _cachedCamFov = 65f;
        private bool _wasSeatedAtComputer = false;

        public event Action<bool> OnPauseStateChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var mgr = Instance; // triggers property getter
                Debug.Log("[GamePauseManager] Initialized master pause manager.");
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Prevent OS from sleeping or dimming display after 5-10 minutes of inactivity
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Clamp max delta time to 50ms to prevent physics explosions after long pause or tab out
            Time.maximumDeltaTime = 0.05f;

            BuildPauseUI();
        }

        private void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Time.maximumDeltaTime = 0.05f;

            if (_pauseOverlayUI != null)
            {
                _pauseOverlayUI.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            CheckPauseInput();
        }

        private void CheckPauseInput()
        {
            bool togglePressed = false;

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.pKey.wasPressedThisFrame || kb.pauseKey.wasPressedThisFrame)
                {
                    togglePressed = true;
                }
            }

            try
            {
                if (!togglePressed && (UnityEngine.Input.GetKeyDown(KeyCode.P) || UnityEngine.Input.GetKeyDown(KeyCode.Pause)))
                {
                    togglePressed = true;
                }
            }
            catch {}

            if (togglePressed)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void PauseGame()
        {
            if (_isPaused) return;

            _isPaused = true;

            // 1. Cache exact player and camera transforms
            CacheCurrentState();

            // 2. Freeze time and audio cleanly
            Time.timeScale = 0.0f;
            AudioListener.pause = true;

            // 3. Unlock cursor so user can interact with pause menus or OS
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 4. Show pause overlay
            if (_pauseOverlayUI != null)
            {
                _pauseOverlayUI.SetActive(true);
            }

            OnPauseStateChanged?.Invoke(true);
            Debug.Log("[GamePauseManager] Game paused cleanly. Systems frozen, state preserved.");
        }

        public void ResumeGame()
        {
            if (!_isPaused) return;

            _isPaused = false;

            // 1. Restore time scale and audio
            Time.timeScale = 1.0f;
            AudioListener.pause = false;
            AudioListener.volume = 1.0f;

            // 2. Restore exact cached state to guarantee 0 drift across 10+ pause/resume cycles
            RestoreState();

            // 3. Hide pause overlay
            if (_pauseOverlayUI != null)
            {
                _pauseOverlayUI.SetActive(false);
            }

            // 4. Sync physics and mouse-look orientation
            Physics.SyncTransforms();

            var mouseLook = UnityEngine.Object.FindAnyObjectByType<MouseLook>();
            if (mouseLook != null)
            {
                var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                float yaw = pc != null ? pc.transform.eulerAngles.y : _cachedPlayerRot.eulerAngles.y;
                mouseLook.SyncOrientation(yaw, 0f);
            }

            // 5. Re-lock cursor depending on whether seated or exploring
            bool inComputer = ComputerInteractionManager.Instance != null && ComputerInteractionManager.Instance.IsInComputerMode;
            if (inComputer)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            else
            {
                if (mouseLook != null && mouseLook.IsLookEnabled)
                {
                    mouseLook.SetCursorLock(true);
                }
            }

            OnPauseStateChanged?.Invoke(false);
            Debug.Log("[GamePauseManager] Game resumed cleanly. Transforms, camera, and seating preserved.");
        }

        private void CacheCurrentState()
        {
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                _cachedPlayerPos = pc.transform.position;
                _cachedPlayerRot = pc.transform.rotation;
            }

            var cam = Camera.main;
            if (cam != null)
            {
                _cachedCamPos = cam.transform.position;
                _cachedCamRot = cam.transform.rotation;
                _cachedCamFov = cam.fieldOfView;
            }

            _wasSeatedAtComputer = (ComputerInteractionManager.Instance != null && ComputerInteractionManager.Instance.IsInComputerMode);
        }

        private void RestoreState()
        {
            var pc = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (pc != null && _cachedPlayerPos.sqrMagnitude > 0.001f)
            {
                // Temporarily disable CharacterController to prevent physics collision resolution from rejecting restored position
                var cc = pc.GetComponent<CharacterController>();
                bool ccWasEnabled = cc != null && cc.enabled;
                if (cc != null) cc.enabled = false;

                pc.transform.position = _cachedPlayerPos;
                pc.transform.rotation = _cachedPlayerRot;

                if (cc != null && ccWasEnabled) cc.enabled = true;
            }

            var cam = Camera.main;
            if (cam != null && _cachedCamPos.sqrMagnitude > 0.001f)
            {
                cam.transform.position = _cachedCamPos;
                cam.transform.rotation = _cachedCamRot;
                cam.fieldOfView = _cachedCamFov;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Time.maximumDeltaTime = 0.05f;

            if (hasFocus)
            {
                // Regained focus after Alt-Tab, long idle (5+ minutes), or editor pause
                if (!_isPaused)
                {
                    AudioListener.pause = false;
                    AudioListener.volume = 1.0f;
                    Physics.SyncTransforms();

                    bool inComputer = ComputerInteractionManager.Instance != null && ComputerInteractionManager.Instance.IsInComputerMode;
                    if (inComputer)
                    {
                        Cursor.lockState = CursorLockMode.Confined;
                        Cursor.visible = false;
                    }
                    else
                    {
                        var mouseLook = UnityEngine.Object.FindAnyObjectByType<MouseLook>();
                        if (mouseLook != null && mouseLook.IsLookEnabled)
                        {
                            mouseLook.SetCursorLock(true);
                        }
                    }
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // OS suspended or application paused
                CacheCurrentState();
            }
            else
            {
                // OS resumed
                if (!_isPaused)
                {
                    AudioListener.pause = false;
                    AudioListener.volume = 1.0f;
                    Physics.SyncTransforms();
                }
            }
        }

        private void BuildPauseUI()
        {
            // Build an elegant, non-intrusive cyber HUD banner
            GameObject canvasObj = new GameObject("PauseCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(CanvasGroup));
            canvasObj.transform.SetParent(transform, false);

            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Above all world UI

            _overlayCanvasGroup = canvasObj.GetComponent<CanvasGroup>();

            // Dark semi-transparent backdrop
            GameObject panel = new GameObject("PausePanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.sizeDelta = Vector2.zero;

            var panelImg = panel.GetComponent<UnityEngine.UI.Image>();
            panelImg.color = new Color(0.02f, 0.04f, 0.08f, 0.75f);

            // Centered glowing text box
            GameObject textObj = new GameObject("PauseText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(panel.transform, false);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.sizeDelta = new Vector2(650f, 180f);
            textRt.anchoredPosition = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "<size=36><b><color=#00e5ff>GAME PAUSED</color></b></size>\n<size=18><color=#8be9fd>SYSTEMS FROZEN // STATE PRESERVED</color>\n\n<color=#ffffff>PRESS <b>[P]</b> TO RESUME</color></size>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            _pauseOverlayUI = canvasObj;
            _pauseOverlayUI.SetActive(false);
        }
    }
}

