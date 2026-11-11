using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using TMPro;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// High-Fidelity VIP Theater Video Player.
    /// Supports:
    /// 1. Playing local downloaded videos (.mp4, .webm, .mov, .m4v, .avi, .mkv) from anywhere on the user's PC via native Windows file dialog.
    /// 2. Scanning and playing videos dropped into StreamingAssets/Videos.
    /// 3. Interactive on-screen & sofa HUD media controls: Play, Pause, Stop, Seek Scrubbing, Volume, Mute, Step Forward/Back.
    /// 4. Seated keyboard shortcuts: [O] Browse, [Space] Play/Pause, [Arrows] Seek/Volume, [M] Mute, [S] Stop.
    /// 5. Auto-fading cinema controls during video playback for unobstructed viewing.
    /// 6. Spatial audio routed through the theater's cinema sound system.
    /// </summary>
    public class TheaterVideoPlayer : MonoBehaviour
    {
        public static TheaterVideoPlayer Instance { get; private set; }

        [Header("Video Engine")]
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private RenderTexture _renderTexture;
        [SerializeField] private RawImage _screenDisplayImage;

        [Header("UI Panels")]
        [SerializeField] private GameObject _controlsPanel;
        [SerializeField] private CanvasGroup _controlsCanvasGroup;
        [SerializeField] private GameObject _welcomeStandbyPanel;

        [Header("Playback UI Controls")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Slider _timeSlider;
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private Button _playPauseButton;
        [SerializeField] private TextMeshProUGUI _playPauseText;
        [SerializeField] private Button _browseButton;
        [SerializeField] private Button _stopButton;
        [SerializeField] private Button _stepBackButton;
        [SerializeField] private Button _stepForwardButton;
        [SerializeField] private Button _muteButton;
        [SerializeField] private TextMeshProUGUI _muteText;

        [Header("Background Playback Toggle")]
        [SerializeField] private Button _bgPlaybackButton;
        [SerializeField] private TextMeshProUGUI _bgPlaybackText;
        [SerializeField] private Image _bgPlaybackImage;

        [Header("Playlist Container")]
        [SerializeField] private Transform _playlistContainer;

        private bool _isDraggingSlider = false;
        private bool _isMuted = false;
        private bool _playInBackground = true;
        private float _lastUserInteractionTime = 0f;
        private float _previousVolume = 1.0f;
        private string _currentFilePath = "";
        private List<string> _detectedLocalVideos = new List<string>();

        public bool PlayInBackground
        {
            get => _playInBackground;
            set
            {
                _playInBackground = value;
                PlayerPrefs.SetInt("Theater_PlayInBackground", _playInBackground ? 1 : 0);
                PlayerPrefs.Save();
                UpdateBgPlaybackButtonVisuals();
            }
        }

        #region Windows OpenFileName Native P/Invoke
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter = null;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public string file = null;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _playInBackground = PlayerPrefs.GetInt("Theater_PlayInBackground", 1) == 1;

            TheaterCinemaController.EnsureEventSystem();
            EnsureComponents();
            HookUIEvents();
            ScanStreamingAssetsVideos();
        }

        private void Start()
        {
            TheaterCinemaController.EnsureEventSystem();
            SetControlsVisible(true);
            UpdateStatus("STANDBY | NO MEDIA LOADED");
            if (_screenDisplayImage != null) _screenDisplayImage.gameObject.SetActive(false);
            if (_welcomeStandbyPanel != null) _welcomeStandbyPanel.SetActive(true);
            UpdateBgPlaybackButtonVisuals();
        }

        public void EnsureComponents()
        {
            if (_videoPlayer == null)
            {
                _videoPlayer = GetComponent<VideoPlayer>();
                if (_videoPlayer == null) _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }

            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Configure AudioSource for Dolby Atmos theater acoustics
            _audioSource.spatialBlend = 0.45f;
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.volume = 0.95f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.minDistance = 2.0f;
            _audioSource.maxDistance = 18.0f;

            // Configure VideoPlayer
            _videoPlayer.playOnAwake = false;
            _videoPlayer.waitForFirstFrame = true;
            _videoPlayer.skipOnDrop = true;
            _videoPlayer.isLooping = true; // Video plays continuously forever until user changes it
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, _audioSource);

            // Create crisp 1080p projection RenderTexture if not assigned
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Theater_Projection_RT",
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                _renderTexture.Create();
            }

            _videoPlayer.targetTexture = _renderTexture;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;

            if (_screenDisplayImage != null)
            {
                _screenDisplayImage.texture = _renderTexture;
            }

            // Video events
            _videoPlayer.prepareCompleted -= OnVideoPrepared;
            _videoPlayer.prepareCompleted += OnVideoPrepared;

            _videoPlayer.loopPointReached -= OnVideoEnded;
            _videoPlayer.loopPointReached += OnVideoEnded;

            _videoPlayer.errorReceived -= OnVideoError;
            _videoPlayer.errorReceived += OnVideoError;
        }

        private void HookUIEvents()
        {
            if (_screenDisplayImage != null)
            {
                var screenBtn = _screenDisplayImage.GetComponent<Button>();
                if (screenBtn == null) screenBtn = _screenDisplayImage.gameObject.AddComponent<Button>();
                screenBtn.transition = Selectable.Transition.None;
                screenBtn.onClick.RemoveAllListeners();
                screenBtn.onClick.AddListener(() =>
                {
                    WakeControls();
                    TogglePlayPause();
                });
            }

            if (_browseButton != null) _browseButton.onClick.AddListener(OpenFileBrowser);
            if (_playPauseButton != null) _playPauseButton.onClick.AddListener(TogglePlayPause);
            if (_stopButton != null) _stopButton.onClick.AddListener(StopVideo);
            if (_stepBackButton != null) _stepBackButton.onClick.AddListener(() => StepVideo(-10f));
            if (_stepForwardButton != null) _stepForwardButton.onClick.AddListener(() => StepVideo(10f));
            if (_muteButton != null) _muteButton.onClick.AddListener(ToggleMute);

            if (_bgPlaybackButton == null && _controlsPanel != null)
            {
                var t = _controlsPanel.transform.Find("Btn_BgPlayback");
                if (t != null)
                {
                    _bgPlaybackButton = t.GetComponent<Button>();
                    _bgPlaybackText = t.GetComponentInChildren<TextMeshProUGUI>();
                    _bgPlaybackImage = t.GetComponent<Image>();
                }
                else
                {
                    _bgPlaybackButton = CreateRuntimeBgPlaybackButton();
                }
            }

            if (_bgPlaybackButton != null)
            {
                _bgPlaybackButton.onClick.RemoveAllListeners();
                _bgPlaybackButton.onClick.AddListener(ToggleBackgroundPlayback);
            }

            UpdateBgPlaybackButtonVisuals();

            if (_timeSlider != null)
            {
                _timeSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (_volumeSlider != null)
            {
                _volumeSlider.value = _audioSource != null ? _audioSource.volume : 0.95f;
                _volumeSlider.onValueChanged.AddListener(SetVolume);
            }
        }

        private void Update()
        {
            bool inTheater = TheaterCinemaController.Instance != null && TheaterCinemaController.Instance.IsInCinemaMode;

            // Handle hotkeys & mouse activity when in theater mode
            if (inTheater)
            {
                HandleSeatedHotkeys();

                var mouse = Mouse.current;
                if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0.1f)
                {
                    WakeControls();
                }
            }

            // Update time slider and timer text
            if (_videoPlayer != null && _videoPlayer.isPlaying && !_isDraggingSlider)
            {
                if (_videoPlayer.length > 0)
                {
                    float normalized = (float)(_videoPlayer.time / _videoPlayer.length);
                    if (_timeSlider != null) _timeSlider.SetValueWithoutNotify(normalized);
                }

                UpdateTimerText();
            }

            // Auto-hide controls after 4 seconds of playback with no user interaction
            if (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                if (Time.time - _lastUserInteractionTime > 4.0f)
                {
                    SetControlsVisible(false);
                }
            }
            else
            {
                SetControlsVisible(true);
            }
        }

        private void HandleSeatedHotkeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // [O] - Open Local Video Browser
            if (kb.oKey.wasPressedThisFrame)
            {
                WakeControls();
                OpenFileBrowser();
            }

            // [Space] - Play / Pause
            if (kb.spaceKey.wasPressedThisFrame)
            {
                WakeControls();
                TogglePlayPause();
            }

            // [S] - Stop
            if (kb.sKey.wasPressedThisFrame)
            {
                WakeControls();
                StopVideo();
            }

            // [Left Arrow] - Rewind 10s
            if (kb.leftArrowKey.wasPressedThisFrame)
            {
                WakeControls();
                StepVideo(-10f);
            }

            // [Right Arrow] - Fast Forward 10s
            if (kb.rightArrowKey.wasPressedThisFrame)
            {
                WakeControls();
                StepVideo(10f);
            }

            // [Up Arrow] - Volume Up
            if (kb.upArrowKey.wasPressedThisFrame)
            {
                WakeControls();
                SetVolume(Mathf.Clamp01((_audioSource != null ? _audioSource.volume : 0.5f) + 0.1f));
                if (_volumeSlider != null) _volumeSlider.value = _audioSource.volume;
            }

            // [Down Arrow] - Volume Down
            if (kb.downArrowKey.wasPressedThisFrame)
            {
                WakeControls();
                SetVolume(Mathf.Clamp01((_audioSource != null ? _audioSource.volume : 0.5f) - 0.1f));
                if (_volumeSlider != null) _volumeSlider.value = _audioSource.volume;
            }

            // [M] - Mute
            if (kb.mKey.wasPressedThisFrame)
            {
                WakeControls();
                ToggleMute();
            }

            // [B] - Toggle Background Playback / Watch Only Mode
            if (kb.bKey.wasPressedThisFrame)
            {
                WakeControls();
                ToggleBackgroundPlayback();
            }
        }

        public void WakeControls()
        {
            _lastUserInteractionTime = Time.time;
            SetControlsVisible(true);
        }

        public void SetControlsVisible(bool visible)
        {
            if (_controlsCanvasGroup != null)
            {
                _controlsCanvasGroup.alpha = visible ? 1f : 0f;
                _controlsCanvasGroup.interactable = visible;
                _controlsCanvasGroup.blocksRaycasts = visible;
            }
            else if (_controlsPanel != null)
            {
                _controlsPanel.SetActive(visible);
            }
        }

        #region Video Loading & Playback
        /// <summary>
        /// Opens native Windows file dialog to pick any video file (.mp4, .webm, .mov, .m4v, .avi, .mkv).
        /// </summary>
        public void OpenFileBrowser()
        {
            WakeControls();

#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel(
                "Select Downloaded Video to Play in VIP Theater",
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos),
                "mp4,webm,mov,m4v,avi,mkv"
            );

            if (!string.IsNullOrEmpty(path))
            {
                LoadVideo(path);
            }
#else
            OpenWindowsFileDialog();
#endif
        }

        private void OpenWindowsFileDialog()
        {
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(typeof(OpenFileName));
            ofn.filter = "Video Files (*.mp4;*.webm;*.mov;*.m4v;*.avi;*.mkv)\0*.mp4;*.webm;*.mov;*.m4v;*.avi;*.mkv\0All Files (*.*)\0*.*\0\0";
            ofn.file = new string(new char[512]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[128]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyVideos);
            ofn.title = "Select Downloaded Video to Play in VIP Theater";
            ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008; // OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR

            if (GetOpenFileName(ofn))
            {
                string selectedPath = ofn.file.TrimEnd('\0');
                if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
                {
                    LoadVideo(selectedPath);
                }
            }
        }

        /// <summary>
        /// Loads any local video file from an absolute or relative path and begins preparation.
        /// </summary>
        public void LoadVideo(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                UpdateStatus("<color=#ff4040>ERROR: File not found!</color>");
                return;
            }

            EnsureComponents();

            _currentFilePath = filePath;
            string fileName = Path.GetFileName(filePath);

            Debug.Log($"[TheaterVideoPlayer] Loading local video: {filePath}");

            if (_titleText != null)
            {
                _titleText.text = $"<color=#00e5ff>NOW PLAYING:</color> {fileName.ToUpper()}";
            }

            UpdateStatus("<color=#ffd700>PREPARING VIDEO STREAM...</color>");

            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.url = filePath;
            _videoPlayer.isLooping = true; // Loop continuously forever

            // Switch UI views: show video surface, hide standby welcome text
            if (_screenDisplayImage != null) _screenDisplayImage.gameObject.SetActive(true);
            if (_welcomeStandbyPanel != null) _welcomeStandbyPanel.SetActive(false);

            _videoPlayer.Prepare();
        }

        public bool HasActiveVideo => !string.IsNullOrEmpty(_currentFilePath) && _videoPlayer != null && (_videoPlayer.isPrepared || _videoPlayer.isPlaying);

        public void ResumeOrEnsurePlaying()
        {
            if (_screenDisplayImage != null) _screenDisplayImage.gameObject.SetActive(true);
            if (_welcomeStandbyPanel != null) _welcomeStandbyPanel.SetActive(false);

            if (_videoPlayer != null)
            {
                _videoPlayer.isLooping = true;
                if (!_videoPlayer.isPlaying)
                {
                    _videoPlayer.Play();
                }
                if (_audioSource != null && !_audioSource.isPlaying)
                {
                    _audioSource.UnPause();
                    if (!_audioSource.isPlaying) _audioSource.Play();
                }
                UpdatePlayPauseButtonText(true);
            }
            WakeControls();
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            Debug.Log($"[TheaterVideoPlayer] Video Prepared: {vp.width}x{vp.height} @ {vp.frameRate}fps, Duration: {vp.length:F1}s");
            UpdateStatus($"<color=#00ff88>PLAYING</color> | {vp.width}x{vp.height} @ {vp.frameRate:F0}FPS | DOLBY ATMOS 7.1");

            vp.isLooping = true;
            vp.Play();
            if (_audioSource != null && !_audioSource.isPlaying)
            {
                _audioSource.Play();
            }

            UpdatePlayPauseButtonText(true);
            WakeControls();
        }

        public void Play()
        {
            if (_videoPlayer != null && _videoPlayer.isPrepared)
            {
                _videoPlayer.Play();
                if (_audioSource != null) _audioSource.UnPause();
                UpdatePlayPauseButtonText(true);
                UpdateStatus("<color=#00ff88>PLAYING</color>");
                WakeControls();
                RoomLightingController.Instance?.ApplyZenTranquilLighting();
            }
            else if (!string.IsNullOrEmpty(_currentFilePath))
            {
                LoadVideo(_currentFilePath);
                RoomLightingController.Instance?.ApplyZenTranquilLighting();
            }
            else
            {
                // Play relaxing music through room speakers from S:\arvr project\songs
                RoomSpeakerController.Instance?.PlayNextSong();
                RoomLightingController.Instance?.ApplyZenTranquilLighting();
            }
        }

        public void Pause()
        {
            if (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                _videoPlayer.Pause();
                if (_audioSource != null) _audioSource.Pause();
                UpdatePlayPauseButtonText(false);
                UpdateStatus("<color=#ffd700>PAUSED</color>");
                WakeControls();
            }
            else if (RoomSpeakerController.Instance != null && RoomSpeakerController.Instance.IsPlaying)
            {
                RoomSpeakerController.Instance.PauseMusic();
                UpdatePlayPauseButtonText(false);
                UpdateStatus("<color=#ffd700>MUSIC PAUSED</color>");
                WakeControls();
            }
        }

        public void TogglePlayPause()
        {
            if (_videoPlayer != null && _videoPlayer.isPlaying)
            {
                Pause();
            }
            else if (RoomSpeakerController.Instance != null && RoomSpeakerController.Instance.IsPlaying)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        /// <summary>
        /// Displays track info from RoomSpeakerController on the cinema screen banner.
        /// </summary>
        public void DisplayAudioTrackInfo(string songTitle)
        {
            if (_titleText != null)
            {
                _titleText.text = $"<color=#00e5ff>NOW PLAYING:</color> {songTitle.ToUpper()}";
            }

            UpdateStatus("<color=#00ff88>PLAYING RELAXING MUSIC</color> | HIFI DOLBY SURROUND");
            UpdatePlayPauseButtonText(true);

            if (_screenDisplayImage != null) _screenDisplayImage.gameObject.SetActive(true);
            if (_welcomeStandbyPanel != null) _welcomeStandbyPanel.SetActive(false);

            WakeControls();
        }

        public void StopVideo()
        {
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                if (_audioSource != null) _audioSource.Stop();
            }

            UpdatePlayPauseButtonText(false);
            UpdateStatus("STOPPED | STANDBY");

            if (_timeSlider != null) _timeSlider.SetValueWithoutNotify(0f);
            if (_timeText != null) _timeText.text = "00:00 / 00:00";

            // Return to welcome standby screen
            if (_screenDisplayImage != null) _screenDisplayImage.gameObject.SetActive(false);
            if (_welcomeStandbyPanel != null) _welcomeStandbyPanel.SetActive(true);
            SetControlsVisible(true);
        }

        public void StepVideo(float seconds)
        {
            if (_videoPlayer == null || !_videoPlayer.isPrepared || _videoPlayer.length <= 0) return;

            double targetTime = Mathf.Clamp((float)_videoPlayer.time + seconds, 0f, (float)_videoPlayer.length);
            _videoPlayer.time = targetTime;
            UpdateTimerText();
            WakeControls();
        }

        private void OnSliderValueChanged(float value)
        {
            if (_videoPlayer == null || !_videoPlayer.isPrepared || _videoPlayer.length <= 0) return;

            // Only seek if user explicitly triggered slider change
            if (_isDraggingSlider || UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject == _timeSlider?.gameObject)
            {
                double targetTime = value * _videoPlayer.length;
                _videoPlayer.time = targetTime;
                UpdateTimerText();
                WakeControls();
            }
        }

        public void SetVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (_audioSource != null)
            {
                _audioSource.volume = volume;
            }

            if (_videoPlayer != null && _videoPlayer.canSetDirectAudioVolume)
            {
                _videoPlayer.SetDirectAudioVolume(0, volume);
            }

            _isMuted = (volume <= 0.001f);
            UpdateMuteButtonText();
            WakeControls();
        }

        public void ToggleMute()
        {
            if (_audioSource == null) return;

            if (!_isMuted)
            {
                _previousVolume = _audioSource.volume > 0.05f ? _audioSource.volume : 0.95f;
                SetVolume(0f);
                if (_volumeSlider != null) _volumeSlider.value = 0f;
            }
            else
            {
                SetVolume(_previousVolume);
                if (_volumeSlider != null) _volumeSlider.value = _previousVolume;
            }
        }

        private void OnVideoEnded(VideoPlayer vp)
        {
            // Continuous infinite loop: replay selected video forever until user chooses a new one
            if (!string.IsNullOrEmpty(_currentFilePath) && vp != null)
            {
                vp.time = 0;
                vp.Play();
                if (_audioSource != null && !_audioSource.isPlaying) _audioSource.Play();
                UpdatePlayPauseButtonText(true);
                return;
            }

            Debug.Log("[TheaterVideoPlayer] Video playback completed.");
            UpdateStatus("<color=#00e5ff>PLAYBACK FINISHED</color>");
            UpdatePlayPauseButtonText(false);
            SetControlsVisible(true);
        }

        private void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"[TheaterVideoPlayer] Video Error: {message}");
            UpdateStatus($"<color=#ff4040>VIDEO ERROR: {message}</color>");
            UpdatePlayPauseButtonText(false);
            SetControlsVisible(true);
        }
        #endregion

        #region Helpers & UI Updaters
        public void UpdatePlayPauseButtonText(bool isPlaying)
        {
            if (_playPauseText != null)
            {
                _playPauseText.text = isPlaying ? "<b>[||] PAUSE</b>" : "<b>> PLAY</b>";
            }
        }

        private void UpdateMuteButtonText()
        {
            if (_muteText != null)
            {
                _muteText.text = _isMuted ? "<b>[MUTE] UNMUTE</b>" : "<b>[+] MUTE</b>";
            }
        }

        private void UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }

        private void UpdateTimerText()
        {
            if (_timeText == null || _videoPlayer == null) return;

            string cur = FormatTime(_videoPlayer.time);
            string total = FormatTime(_videoPlayer.length);
            _timeText.text = $"{cur} / {total}";
        }

        private string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0) seconds = 0;
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            if (t.TotalHours >= 1)
            {
                return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)t.TotalHours, t.Minutes, t.Seconds);
            }
            return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }

        public void ScanStreamingAssetsVideos()
        {
            _detectedLocalVideos.Clear();
            string folder = Path.Combine(Application.streamingAssetsPath, "Videos");
            if (!Directory.Exists(folder))
            {
                try { Directory.CreateDirectory(folder); } catch {}
                return;
            }

            string[] extensions = new string[] { "*.mp4", "*.webm", "*.mov", "*.m4v", "*.avi", "*.mkv" };
            foreach (var ext in extensions)
            {
                try
                {
                    string[] files = Directory.GetFiles(folder, ext);
                    _detectedLocalVideos.AddRange(files);
                }
                catch {}
            }

            Debug.Log($"[TheaterVideoPlayer] Detected {_detectedLocalVideos.Count} local video(s) in StreamingAssets/Videos.");
        }

        #region Background Playback / Watch Only Control
        public void ToggleBackgroundPlayback()
        {
            PlayInBackground = !PlayInBackground;
            WakeControls();
            if (_playInBackground)
            {
                UpdateStatus("<color=#00e5ff>BACKGROUND PLAY: ON | Video keeps playing on wall after [ESC]</color>");
            }
            else
            {
                UpdateStatus("<color=#ffd700>WATCH ONLY: ON | Video pauses on [ESC]</color>");
            }
        }

        private void UpdateBgPlaybackButtonVisuals()
        {
            if (_bgPlaybackText != null)
            {
                _bgPlaybackText.text = _playInBackground ? "<b>[CINEMA] BG PLAY: ON</b>" : "<b>[||] WATCH ONLY</b>";
                _bgPlaybackText.color = _playInBackground ? Color.white : new Color(1f, 0.88f, 0.35f);
            }

            if (_bgPlaybackImage == null && _bgPlaybackButton != null)
            {
                _bgPlaybackImage = _bgPlaybackButton.GetComponent<Image>();
            }

            if (_bgPlaybackImage != null)
            {
                Color baseCol = _playInBackground ? new Color(0.06f, 0.40f, 0.48f, 0.95f) : new Color(0.28f, 0.20f, 0.12f, 0.95f);
                _bgPlaybackImage.color = baseCol;

                if (_bgPlaybackButton != null)
                {
                    ColorBlock cb = _bgPlaybackButton.colors;
                    cb.normalColor = baseCol;
                    cb.highlightedColor = baseCol * 1.35f;
                    cb.pressedColor = baseCol * 0.8f;
                    _bgPlaybackButton.colors = cb;
                }
            }
        }

        private Button CreateRuntimeBgPlaybackButton()
        {
            if (_controlsPanel == null) return null;

            var existing = _controlsPanel.transform.Find("Btn_BgPlayback");
            if (existing != null)
            {
                _bgPlaybackText = existing.GetComponentInChildren<TextMeshProUGUI>();
                _bgPlaybackImage = existing.GetComponent<Image>();
                return existing.GetComponent<Button>();
            }

            GameObject btnObj = new GameObject("Btn_BgPlayback", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(_controlsPanel.transform, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-120f, -24f);
            rt.sizeDelta = new Vector2(165f, 42f);

            Image img = btnObj.GetComponent<Image>();
            _bgPlaybackImage = img;
            img.color = new Color(0.06f, 0.40f, 0.48f, 0.95f);

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = img.color;
            cb.highlightedColor = img.color * 1.35f;
            cb.pressedColor = img.color * 0.8f;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "<b>[CINEMA] BG PLAY: ON</b>";
            tmp.fontSize = 15;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            _bgPlaybackText = tmp;

            return btn;
        }
        #endregion
        #endregion
    }
}
