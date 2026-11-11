using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Master controller for room speakers and music playback from S:\arvr project\songs.
    /// Manages:
    /// 1. Dynamic scanning and playback of MP3 songs from S:\arvr project\songs.
    /// 2. Spatial 3D audio delivery through Floor Tower Speakers (Speaker_Floor_FL & Speaker_Floor_FR).
    /// 3. Automatically sets room lighting to peaceful (Serene Jade Twilight) during playback.
    /// 4. Synchronizes with TheaterVideoPlayer UI & on-screen Play/Pause controls.
    /// 5. Visualizer pulsation of subwoofer glowing rings to music tempo.
    /// 6. Physical interaction: walking up to speakers and pressing [E] toggles playback.
    /// </summary>
    public class RoomSpeakerController : InteractiveProp
    {
        public static RoomSpeakerController Instance { get; private set; }

        public const string DefaultSongsDirectory = @"S:\arvr project\songs";

        [Header("Audio Output")]
        [SerializeField] private AudioSource _leftSpeakerAudio;
        [SerializeField] private AudioSource _rightSpeakerAudio;
        [SerializeField] private float _speakerVolume = 0.85f;

        [Header("Playlist State")]
        [SerializeField] private List<string> _playlist = new List<string>();
        [SerializeField] private int _currentSongIndex = -1;
        [SerializeField] private bool _isPlaying = false;
        [SerializeField] private bool _autoPlayOnStart = true;

        private Coroutine _loadRoutine;
        private List<Renderer> _subwooferGlowRenderers = new List<Renderer>();
        private MaterialPropertyBlock _glowPropBlock;
        private AudioClip _currentClip;

        public static event Action<string, bool> OnPlaybackChanged;

        public bool IsPlaying => _isPlaying;
        public int PlaylistCount => _playlist.Count;
        public int CurrentSongIndex => _currentSongIndex;
        public IReadOnlyList<string> Playlist => _playlist;
        public string CurrentSongName => (_currentSongIndex >= 0 && _currentSongIndex < _playlist.Count) 
            ? Path.GetFileNameWithoutExtension(_playlist[_currentSongIndex]) 
            : "No Song Loaded";

        public float SpeakerVolume
        {
            get => _speakerVolume;
            set
            {
                _speakerVolume = Mathf.Clamp01(value);
                if (_leftSpeakerAudio != null) _leftSpeakerAudio.volume = _speakerVolume;
                if (_rightSpeakerAudio != null) _rightSpeakerAudio.volume = _speakerVolume;
            }
        }

        public float CurrentTrackTime => _leftSpeakerAudio != null ? _leftSpeakerAudio.time : 0f;
        public float CurrentTrackLength => _currentClip != null ? _currentClip.length : 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureSpeakerController()
        {
            if (Instance == null)
            {
                var existing = FindAnyObjectByType<RoomSpeakerController>();
                if (existing == null)
                {
                    // Find floor speakers in scene
                    GameObject fl = GameObject.Find("Speaker_Floor_FL");
                    if (fl != null)
                    {
                        fl.AddComponent<RoomSpeakerController>();
                    }
                    else
                    {
                        GameObject go = new GameObject("RoomSpeakerController");
                        go.AddComponent<RoomSpeakerController>();
                    }
                }
            }
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
            _interactionRange = 2.5f;
            _promptOffset = new Vector3(0f, 0.45f, 0f);
            _glowPropBlock = new MaterialPropertyBlock();

            SetPromptAction("PLAY RELAXING MUSIC");
            SetupSpeakers();
            ScanSongsDirectory();
        }

        protected override void Start()
        {
            base.Start();
            if (_autoPlayOnStart && !_isPlaying && _playlist.Count > 0)
            {
                PlaySongAtIndex(0);
            }
        }

        private void SetupSpeakers()
        {
            // Setup Left Floor Speaker
            GameObject fl = GameObject.Find("Speaker_Floor_FL");
            if (fl != null)
            {
                _leftSpeakerAudio = fl.GetComponent<AudioSource>();
                if (_leftSpeakerAudio == null) _leftSpeakerAudio = fl.AddComponent<AudioSource>();
                ConfigureSpeakerAudioSource(_leftSpeakerAudio);

                // Add collider if missing for player interaction
                if (fl.GetComponent<Collider>() == null)
                {
                    var col = fl.AddComponent<BoxCollider>();
                    col.size = new Vector3(0.7f, 1.9f, 0.6f);
                    col.center = new Vector3(0f, 0f, 0f);
                }
            }

            // Setup Right Floor Speaker
            GameObject fr = GameObject.Find("Speaker_Floor_FR");
            if (fr != null)
            {
                _rightSpeakerAudio = fr.GetComponent<AudioSource>();
                if (_rightSpeakerAudio == null) _rightSpeakerAudio = fr.AddComponent<AudioSource>();
                ConfigureSpeakerAudioSource(_rightSpeakerAudio);

                if (fr.GetComponent<Collider>() == null)
                {
                    var col = fr.AddComponent<BoxCollider>();
                    col.size = new Vector3(0.7f, 1.9f, 0.6f);
                    col.center = new Vector3(0f, 0f, 0f);
                }
            }

            // Fallback audio source on this object
            if (_leftSpeakerAudio == null && _rightSpeakerAudio == null)
            {
                _leftSpeakerAudio = gameObject.AddComponent<AudioSource>();
                ConfigureSpeakerAudioSource(_leftSpeakerAudio);
            }

            // Collect subwoofer glow rings for visualizer animation
            CollectGlowRenderers();
        }

        private void ConfigureSpeakerAudioSource(AudioSource src)
        {
            src.spatialBlend = 0.35f; // Spatial 3D sound filling the room rich and clear
            src.playOnAwake = false;
            src.loop = true;
            src.volume = _speakerVolume;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 3.0f;
            src.maxDistance = 45.0f;
        }

        private void CollectGlowRenderers()
        {
            _subwooferGlowRenderers.Clear();
            var allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            foreach (var r in allRenderers)
            {
                if (r.name.Contains("GlowRing") || (r.transform.parent != null && r.transform.parent.name.Contains("Subwoofer")))
                {
                    _subwooferGlowRenderers.Add(r);
                }
            }
        }

        public void ScanSongsDirectory()
        {
            _playlist.Clear();

            string dir = DefaultSongsDirectory;
            if (!Directory.Exists(dir))
            {
                string rel = Path.GetFullPath(Path.Combine(Application.dataPath, "../../songs"));
                if (Directory.Exists(rel)) dir = rel;
            }

            if (Directory.Exists(dir))
            {
                string[] extensions = new string[] { "*.mp3", "*.wav", "*.ogg", "*.m4a" };
                foreach (var ext in extensions)
                {
                    string[] files = Directory.GetFiles(dir, ext);
                    _playlist.AddRange(files);
                }

                Debug.Log($"[RoomSpeakerController] Discovered {_playlist.Count} song(s) in {dir}");
            }
            else
            {
                Debug.LogWarning($"[RoomSpeakerController] Songs directory not found at: {dir}");
            }
        }

        protected override void OnInteract()
        {
            TogglePlayPause();
        }

        public void TogglePlayPause()
        {
            if (_isPlaying)
            {
                PauseMusic();
            }
            else
            {
                if (_currentSongIndex >= 0 && _currentClip != null)
                {
                    ResumeMusic();
                }
                else
                {
                    PlayNextSong();
                }
            }
        }

        public void PlayNextSong()
        {
            if (_playlist.Count == 0)
            {
                ScanSongsDirectory();
            }

            if (_playlist.Count == 0)
            {
                Debug.LogWarning("[RoomSpeakerController] No songs found in playlist to play.");
                return;
            }

            _currentSongIndex = (_currentSongIndex + 1) % _playlist.Count;
            PlaySongAtIndex(_currentSongIndex);
        }

        public void PlayPreviousSong()
        {
            if (_playlist.Count == 0)
            {
                ScanSongsDirectory();
            }

            if (_playlist.Count == 0)
            {
                Debug.LogWarning("[RoomSpeakerController] No songs found in playlist to play.");
                return;
            }

            _currentSongIndex = (_currentSongIndex - 1 + _playlist.Count) % _playlist.Count;
            PlaySongAtIndex(_currentSongIndex);
        }

        public string GetCleanTitleAtIndex(int index)
        {
            if (index >= 0 && index < _playlist.Count)
            {
                return CleanSongTitle(Path.GetFileNameWithoutExtension(_playlist[index]));
            }
            return "Peaceful Track";
        }

        public void PlaySongAtIndex(int index)
        {
            if (index < 0 || index >= _playlist.Count) return;

            string songPath = _playlist[index];
            if (_loadRoutine != null) StopCoroutine(_loadRoutine);
            _loadRoutine = StartCoroutine(LoadAndPlaySongRoutine(songPath));
        }

        private IEnumerator LoadAndPlaySongRoutine(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[RoomSpeakerController] Song file missing: {filePath}");
                yield break;
            }

            string songName = Path.GetFileNameWithoutExtension(filePath);
            Debug.Log($"[RoomSpeakerController] Loading and playing song: {songName}");

            string uri = "file:///" + filePath.Replace('\\', '/');
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    _currentClip = DownloadHandlerAudioClip.GetContent(www);
                    _currentClip.name = songName;

                    // Play across speakers
                    PlayClipOnSpeakers(_currentClip);

                    // Set room lighting to peaceful!
                    RoomLightingController.Instance?.ApplyZenTranquilLighting();

                    // Notify HUD and Theater Video Player
                    NotifySongStarted(songName);
                }
                else
                {
                    Debug.LogWarning($"[RoomSpeakerController] UnityWebRequest error loading {filePath}: {www.error}");
                }
            }
        }

        private void PlayClipOnSpeakers(AudioClip clip)
        {
            _isPlaying = true;
            SetPromptAction("PAUSE RELAXING MUSIC");

            if (_leftSpeakerAudio != null)
            {
                _leftSpeakerAudio.clip = clip;
                _leftSpeakerAudio.time = 0f;
                _leftSpeakerAudio.Play();
            }

            if (_rightSpeakerAudio != null)
            {
                _rightSpeakerAudio.clip = clip;
                _rightSpeakerAudio.time = 0f;
                _rightSpeakerAudio.Play();
            }

            OnPlaybackChanged?.Invoke(CleanSongTitle(clip.name), true);
        }

        public void PauseMusic()
        {
            _isPlaying = false;
            SetPromptAction("RESUME RELAXING MUSIC");

            if (_leftSpeakerAudio != null) _leftSpeakerAudio.Pause();
            if (_rightSpeakerAudio != null) _rightSpeakerAudio.Pause();

            // Sync with Theater player
            if (TheaterVideoPlayer.Instance != null)
            {
                TheaterVideoPlayer.Instance.UpdatePlayPauseButtonText(false);
            }

            OnPlaybackChanged?.Invoke(CurrentSongName, false);
        }

        public void ResumeMusic()
        {
            _isPlaying = true;
            SetPromptAction("PAUSE RELAXING MUSIC");

            if (_leftSpeakerAudio != null) _leftSpeakerAudio.UnPause();
            if (_rightSpeakerAudio != null) _rightSpeakerAudio.UnPause();

            RoomLightingController.Instance?.ApplyZenTranquilLighting();

            if (TheaterVideoPlayer.Instance != null)
            {
                TheaterVideoPlayer.Instance.UpdatePlayPauseButtonText(true);
            }

            OnPlaybackChanged?.Invoke(CurrentSongName, true);
        }

        public void StopMusic()
        {
            _isPlaying = false;
            SetPromptAction("PLAY RELAXING MUSIC");

            if (_leftSpeakerAudio != null) _leftSpeakerAudio.Stop();
            if (_rightSpeakerAudio != null) _rightSpeakerAudio.Stop();

            if (TheaterVideoPlayer.Instance != null)
            {
                TheaterVideoPlayer.Instance.UpdatePlayPauseButtonText(false);
            }

            OnPlaybackChanged?.Invoke("Stopped", false);
        }

        private void NotifySongStarted(string songName)
        {
            string cleanTitle = CleanSongTitle(songName);

            // Display on Theater Video Player if active in scene
            if (TheaterVideoPlayer.Instance != null)
            {
                TheaterVideoPlayer.Instance.DisplayAudioTrackInfo(cleanTitle);
            }

            // Display HUD notification banner
            if (DrinkEffectManager.Instance != null)
            {
                DrinkEffectManager.Instance.ShowHUDBanner(
                    $"<b><color=#55ff99>[ NOW PLAYING ]</color></b>  <size=20><color=#ffffff>{cleanTitle.ToUpper()}</color></size>\n<size=15><color=#d6ffe4>Acoustic Speakers: <b>Online</b>  |  Room Atmosphere: <b>Peaceful</b></color></size>",
                    5.0f,
                    new Color(0.15f, 0.95f, 0.55f, 0.95f)
                );
            }
        }

        private string CleanSongTitle(string raw)
        {
            // E.g. "atlasaudio-relaxing-621597" -> "Atlasaudio - Relaxing"
            string[] parts = raw.Split('-');
            if (parts.Length >= 2)
            {
                string artist = char.ToUpper(parts[0][0]) + parts[0].Substring(1);
                string title = char.ToUpper(parts[1][0]) + parts[1].Substring(1);
                return $"{artist} - {title}";
            }
            return raw;
        }

        protected override void Update()
        {
            base.Update();

            // Subtle rhythmic pulsation of speaker subwoofer glowing rings to music tempo
            if (_isPlaying && _subwooferGlowRenderers.Count > 0)
            {
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 3.5f);
                Color c = Color.Lerp(new Color(0.1f, 0.7f, 0.4f), new Color(0.2f, 1.0f, 0.6f), pulse) * 2.8f;

                for (int i = 0; i < _subwooferGlowRenderers.Count; i++)
                {
                    var r = _subwooferGlowRenderers[i];
                    if (r == null) continue;
                    r.GetPropertyBlock(_glowPropBlock);
                    _glowPropBlock.SetColor("_EmissionColor", c);
                    r.SetPropertyBlock(_glowPropBlock);
                }
            }
        }
    }
}
