using System;
using System.Collections;
using UnityEngine;
using VRRemoteDesktop.Remote;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.UI.Windows;

namespace VRRemoteDesktop.Audio
{
    /// <summary>
    /// Master Audio Manager fulfilling Section 25 & Phase 11 of Master Requirements.
    /// Procedurally synthesizes and plays subtle futuristic cybernetic sound effects
    /// (Button hover, click, window open/close, keyboard taps, interaction chimes, connection alert)
    /// without relying on missing external audio asset files.
    /// 
    /// Automatically ensures an active AudioListener exists on the main camera,
    /// guarantees unmuted master volume, and plays atmospheric cyber room audio.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioManager_AutoCreated");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Volume Controls")]
        [Range(0f, 1f)] [SerializeField] private float _masterVolume = 0.85f;
        [Range(0f, 1f)] [SerializeField] private float _uiVolume = 0.80f;
        [Range(0f, 1f)] [SerializeField] private float _keyboardVolume = 0.65f;
        [Range(0f, 1f)] [SerializeField] private float _ambientVolume = 0.35f;

        private AudioSource _uiSource;
        private AudioSource _keyboardSource;
        private AudioSource _ambientSource;
        private AudioSource _musicSource;

        private AudioClip _hoverClip;
        private AudioClip _clickClip;
        private AudioClip _windowOpenClip;
        private AudioClip _windowCloseClip;
        private AudioClip _keyPressClip;
        private AudioClip _promptClip;
        private AudioClip _connectClip;
        private AudioClip _disconnectClip;
        private AudioClip _ambientClip;
        private AudioClip _slurpClip;
        private AudioClip _zenMusicClip;
        private Coroutine _musicFadeRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoadedInit()
        {
            EnsureAudioListener();
            if (Instance == null)
            {
                var mgr = Instance; // Forces creation
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            EnsureAudioListener();
            SetupAudioSources();
            GenerateProceduralAudioClips();
            StartAmbientAudio();
        }

        private void Start()
        {
            EnsureAudioListener();
            HookSystemEvents();
        }

        private void Update()
        {
            if (AudioListener.volume < 0.99f || AudioListener.pause)
            {
                AudioListener.volume = 1.0f;
                AudioListener.pause = false;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            UnhookSystemEvents();
        }

        public static void EnsureAudioListener()
        {
            AudioListener.volume = 1.0f;
            AudioListener.pause = false;

            var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
            if (listeners == null || listeners.Length == 0)
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    var allCams = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
                    foreach (var c in allCams)
                    {
                        if (!c.name.Contains("UI") && !c.name.Contains("Surveillance"))
                        {
                            cam = c;
                            break;
                        }
                    }
                }

                if (cam != null)
                {
                    cam.gameObject.AddComponent<AudioListener>();
                    Debug.Log($"[AudioManager] Attached AudioListener to {cam.name}. Audio system is active!");
                }
                else
                {
                    GameObject listenerObj = new GameObject("GlobalAudioListener", typeof(AudioListener));
                    DontDestroyOnLoad(listenerObj);
                    Debug.Log("[AudioManager] Created GlobalAudioListener in scene.");
                }
            }
            else if (listeners.Length > 1)
            {
                Camera mainCam = Camera.main;
                bool kept = false;
                foreach (var l in listeners)
                {
                    if (mainCam != null && l.gameObject == mainCam.gameObject && !kept)
                    {
                        l.enabled = true;
                        kept = true;
                    }
                    else if (!kept)
                    {
                        l.enabled = true;
                        kept = true;
                    }
                    else
                    {
                        l.enabled = false;
                    }
                }
            }
        }

        private void SetupAudioSources()
        {
            _uiSource = gameObject.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;
            _uiSource.spatialBlend = 0f; // 2D Direct UI audio
            _uiSource.volume = _uiVolume * _masterVolume;

            _keyboardSource = gameObject.AddComponent<AudioSource>();
            _keyboardSource.playOnAwake = false;
            _keyboardSource.spatialBlend = 0.1f;
            _keyboardSource.volume = _keyboardVolume * _masterVolume;

            _ambientSource = gameObject.AddComponent<AudioSource>();
            _ambientSource.playOnAwake = false;
            _ambientSource.loop = true;
            _ambientSource.spatialBlend = 0.2f;
            _ambientSource.volume = _ambientVolume * _masterVolume;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f; // 2D clean stereo music
            _musicSource.volume = _masterVolume * 0.70f;
            _musicSource.loop = false;
        }

        private void StartAmbientAudio()
        {
            if (_ambientSource != null && _ambientClip != null)
            {
                _ambientSource.clip = _ambientClip;
                _ambientSource.Play();
            }
        }

        private void GenerateProceduralAudioClips()
        {
            _hoverClip = CreateToneClip("HoverTone", 880f, 0.04f, 0.12f, Waveform.Sine);
            _clickClip = CreateToneClip("ClickTone", 1450f, 0.06f, 0.28f, Waveform.SineTriangle);
            _windowOpenClip = CreateSweepClip("WinOpen", 520f, 1150f, 0.12f, 0.32f);
            _windowCloseClip = CreateSweepClip("WinClose", 1150f, 480f, 0.10f, 0.28f);
            _keyPressClip = CreateToneClip("KeyTap", 1200f, 0.035f, 0.22f, Waveform.Square);
            _promptClip = CreateSweepClip("PromptChime", 650f, 980f, 0.16f, 0.35f);
            _connectClip = CreateChordClip("ConnectChime", new float[] { 523.25f, 659.25f, 783.99f }, 0.28f, 0.35f);
            _disconnectClip = CreateChordClip("DisconnectChime", new float[] { 440f, 370f, 311.13f }, 0.28f, 0.32f);

            // Ambient Room Hum Drone (Warm 55Hz bass with gentle harmonic)
            _ambientClip = CreateAmbientHumClip("CyberRoomHum", 55f, 4.0f, 0.18f);

            // Realistic Liquid Drinking Slurp & Gulp
            _slurpClip = CreateSlurpClip("DrinkSlurp");
        }

        private void HookSystemEvents()
        {
            if (ComputerInteractionManager.Instance != null)
            {
                ComputerInteractionManager.Instance.OnModeChanged += HandleModeChanged;
            }
            if (RemoteDesktopManager.Instance != null)
            {
                RemoteDesktopManager.Instance.OnConnectionStateChanged += HandleConnectionState;
            }
            if (DesktopWindowManager.Instance != null)
            {
                DesktopWindowManager.Instance.OnWindowOpened += HandleWindowOpened;
                DesktopWindowManager.Instance.OnWindowClosed += HandleWindowClosed;
            }
        }

        private void UnhookSystemEvents()
        {
            if (ComputerInteractionManager.Instance != null)
            {
                ComputerInteractionManager.Instance.OnModeChanged -= HandleModeChanged;
            }
            if (RemoteDesktopManager.Instance != null)
            {
                RemoteDesktopManager.Instance.OnConnectionStateChanged -= HandleConnectionState;
            }
            if (DesktopWindowManager.Instance != null)
            {
                DesktopWindowManager.Instance.OnWindowOpened -= HandleWindowOpened;
                DesktopWindowManager.Instance.OnWindowClosed -= HandleWindowClosed;
            }
        }

        public void PlayHover()
        {
            PlayClip(_uiSource, _hoverClip, _uiVolume * _masterVolume * 0.6f, UnityEngine.Random.Range(0.98f, 1.02f));
        }

        public void PlayClick()
        {
            PlayClip(_uiSource, _clickClip, _uiVolume * _masterVolume, UnityEngine.Random.Range(0.95f, 1.05f));
        }

        public void PlayKeyboardTap()
        {
            PlayClip(_keyboardSource, _keyPressClip, _keyboardVolume * _masterVolume, UnityEngine.Random.Range(0.92f, 1.08f));
        }

        public void PlayWindowOpen()
        {
            PlayClip(_uiSource, _windowOpenClip, _uiVolume * _masterVolume, 1f);
        }

        public void PlayWindowClose()
        {
            PlayClip(_uiSource, _windowCloseClip, _uiVolume * _masterVolume, 1f);
        }

        public void PlayPromptChime()
        {
            PlayClip(_uiSource, _promptClip, _uiVolume * _masterVolume, 1f);
        }

        public void PlayConnectChime()
        {
            PlayClip(_uiSource, _connectClip, _uiVolume * _masterVolume, 1f);
        }

        public void PlayDisconnect()
        {
            PlayClip(_uiSource, _disconnectClip, _uiVolume * _masterVolume, 1f);
        }

        public void PlayDisconnectTone()
        {
            PlayDisconnect();
        }

        public void PlayClickTone()
        {
            PlayClick();
        }

        public void PlayPromptTone()
        {
            PlayPromptChime();
        }

        public void PlaySweepPrompt()
        {
            PlayPromptChime();
        }

        public void PlayKeyClick()
        {
            PlayKeyboardTap();
        }

        public void PlaySlurp()
        {
            PlayClip(_uiSource, _slurpClip, _uiVolume * _masterVolume * 1.4f, UnityEngine.Random.Range(0.96f, 1.04f));
        }

        public void PlayPeacefulMusic(float duration = 30.0f)
        {
            if (_zenMusicClip == null)
            {
                _zenMusicClip = CreatePeacefulZenMusicClip("ZenPeacefulMusic", duration);
            }

            if (_musicSource != null && _zenMusicClip != null)
            {
                if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
                _musicFadeRoutine = StartCoroutine(FadeInMusicRoutine(_zenMusicClip, 2.0f));
            }
        }

        public void StopPeacefulMusic(float fadeDuration = 2.0f)
        {
            if (_musicSource != null && _musicSource.isPlaying)
            {
                if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
                _musicFadeRoutine = StartCoroutine(FadeOutMusicRoutine(fadeDuration));
            }
        }

        private IEnumerator FadeInMusicRoutine(AudioClip clip, float fadeDuration)
        {
            _musicSource.clip = clip;
            _musicSource.volume = 0f;
            _musicSource.Play();

            float targetVol = _masterVolume * 0.72f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(0f, targetVol, elapsed / fadeDuration);
                yield return null;
            }
            _musicSource.volume = targetVol;
            _musicFadeRoutine = null;
        }

        private IEnumerator FadeOutMusicRoutine(float fadeDuration)
        {
            float startVol = _musicSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
            _musicSource.volume = 0f;
            _musicSource.Stop();
            _musicFadeRoutine = null;
        }

        private void PlayClip(AudioSource source, AudioClip clip, float volume, float pitch)
        {
            if (source == null || clip == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        private void HandleModeChanged(WorkstationMode mode)
        {
            if (mode == WorkstationMode.ComputerInteraction)
            {
                PlayConnectChime();
            }
            else if (mode == WorkstationMode.Exploration)
            {
                PlayDisconnect();
            }
        }

        private void HandleConnectionState(RemoteConnectionState state)
        {
            if (state == RemoteConnectionState.Connected)
            {
                PlayConnectChime();
            }
            else if (state == RemoteConnectionState.Disconnected || state == RemoteConnectionState.Error)
            {
                PlayDisconnect();
            }
        }

        private void HandleWindowOpened(DesktopWindow window)
        {
            PlayWindowOpen();
        }

        private void HandleWindowClosed(DesktopWindow window)
        {
            PlayWindowClose();
        }

        // =========================================================================
        // PROCEDURAL AUDIO SYNTHESIZERS
        // =========================================================================

        private enum Waveform { Sine, SineTriangle, Square }

        private AudioClip CreateToneClip(string name, float freq, float duration, float gain, Waveform wave)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (float)i / totalSamples); // Linear decay
                envelope = envelope * envelope; // Exponential decay curve

                float val = 0f;
                switch (wave)
                {
                    case Waveform.Sine:
                        val = Mathf.Sin(2f * Mathf.PI * freq * t);
                        break;
                    case Waveform.SineTriangle:
                        val = 0.7f * Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.PingPong(t * freq * 4f, 1f);
                        break;
                    case Waveform.Square:
                        val = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.4f;
                        break;
                }

                samples[i] = Mathf.Clamp(val * envelope * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSweepClip(string name, float startFreq, float endFreq, float duration, float gain)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float phase = 0f;
            for (int i = 0; i < totalSamples; i++)
            {
                float progress = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, progress);
                phase += 2f * Mathf.PI * currentFreq / sampleRate;

                float envelope = Mathf.Sin(progress * Mathf.PI); // Bell curve envelope
                samples[i] = Mathf.Clamp(Mathf.Sin(phase) * envelope * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateChordClip(string name, float[] freqs, float duration, float gain)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01(1f - (float)i / totalSamples);
                envelope = envelope * envelope;

                float sum = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * t);
                }
                sum /= freqs.Length;

                samples[i] = Mathf.Clamp(sum * envelope * gain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateAmbientHumClip(string name, float baseFreq, float duration, float gain)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float f1 = Mathf.Sin(2f * Mathf.PI * baseFreq * t);
                float f2 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 2.0f) * t) * 0.4f;
                float f3 = Mathf.Sin(2f * Mathf.PI * (baseFreq * 3.01f) * t) * 0.15f;

                samples[i] = (f1 + f2 + f3) * gain;
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateSlurpClip(string name)
        {
            int sampleRate = 44100;
            float duration = 0.58f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float val = 0f;

                if (t < 0.40f)
                {
                    // Phase 1: Rapid bubbling liquid suction / slurp
                    float suctionProgress = t / 0.40f;
                    float freq = Mathf.Lerp(280f, 720f, suctionProgress);
                    float hiss = (Mathf.PerlinNoise(t * 1400f, 0f) - 0.5f) * 0.40f;
                    float tone = Mathf.Sin(2f * Mathf.PI * freq * t);
                    float flutter = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 34f * t); // Bubble AM flutter
                    val = (tone * 0.55f + hiss) * flutter * Mathf.SmoothStep(0f, 1f, suctionProgress * 2f);
                }
                else
                {
                    // Phase 2: Satisfying liquid gulp / swallow
                    float gulpTime = t - 0.40f;
                    float gulpDuration = 0.18f;
                    float gulpProgress = gulpTime / gulpDuration;
                    float freq = Mathf.Lerp(260f, 105f, gulpProgress);
                    float env = Mathf.Exp(-14f * gulpTime);
                    val = Mathf.Sin(2f * Mathf.PI * freq * gulpTime) * env * 0.90f;
                }

                samples[i] = Mathf.Clamp(val * 0.85f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreatePeacefulZenMusicClip(string name, float duration)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // 4 Pentatonic harmony phases (Hz) across 30 seconds:
            // Phase 1 (0-8s): Eb major 9
            float[] chord1 = new float[] { 155.56f, 196.00f, 233.08f, 349.23f };
            // Phase 2 (8-16s): C minor 9
            float[] chord2 = new float[] { 130.81f, 196.00f, 233.08f, 311.13f };
            // Phase 3 (16-24s): Ab maj7#11
            float[] chord3 = new float[] { 103.83f, 155.56f, 261.63f, 392.00f };
            // Phase 4 (24-30s): Eb add9 resolving peacefully
            float[] chord4 = new float[] { 155.56f, 233.08f, 293.66f, 349.23f };

            // Tibetan singing bowl strikes (time in sec, freq in Hz)
            float[] bowlTimes = new float[] { 0.8f, 8.0f, 16.0f, 24.0f };
            float[] bowlFreqs = new float[] { 587.33f, 523.25f, 659.25f, 587.33f };

            // Soft koto / bamboo harp droplet plucks (time in sec, freq in Hz)
            float[] pluckTimes = new float[] { 3.2f, 5.6f, 10.4f, 13.2f, 18.5f, 21.0f, 25.8f, 27.5f };
            float[] pluckFreqs = new float[] { 587.33f, 783.99f, 659.25f, 880.00f, 783.99f, 987.77f, 659.25f, 587.33f };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;

                // 1. Warm Ambient Drone & Chords with smooth morphing
                float[] currentChord = chord1;
                float[] nextChord = chord2;
                float blend = 0f;

                if (t < 8f)
                {
                    currentChord = chord1;
                    nextChord = chord2;
                    blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 6.5f) / 1.5f));
                }
                else if (t < 16f)
                {
                    currentChord = chord2;
                    nextChord = chord3;
                    blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 14.5f) / 1.5f));
                }
                else if (t < 24f)
                {
                    currentChord = chord3;
                    nextChord = chord4;
                    blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 22.5f) / 1.5f));
                }
                else
                {
                    currentChord = chord4;
                    nextChord = chord4;
                    blend = 0f;
                }

                float pad = 0f;
                for (int c = 0; c < 4; c++)
                {
                    float f1 = currentChord[c];
                    float f2 = nextChord[c];
                    float freq = Mathf.Lerp(f1, f2, blend);

                    // Dual detuned oscillators for lush chorus shimmer
                    float oscA = Mathf.Sin(2f * Mathf.PI * (freq - 0.35f) * t);
                    float oscB = Mathf.Sin(2f * Mathf.PI * (freq + 0.35f) * t);
                    float harm2 = Mathf.Sin(2f * Mathf.PI * freq * 2.0f * t) * 0.28f;
                    float sub = Mathf.Sin(2f * Mathf.PI * freq * 0.5f * t) * 0.15f;

                    pad += (oscA + oscB + harm2 + sub) * 0.25f;
                }
                pad *= 0.22f;

                // Slow breathing tremolo modulation (0.2 Hz)
                float breathTremolo = 0.82f + 0.18f * Mathf.Sin(2f * Mathf.PI * 0.20f * t);
                pad *= breathTremolo;

                // 2. Singing Bowl Gong Resonances
                float bells = 0f;
                for (int b = 0; b < bowlTimes.Length; b++)
                {
                    float bt = bowlTimes[b];
                    if (t >= bt)
                    {
                        float age = t - bt;
                        if (age < 7.0f)
                        {
                            float bf = bowlFreqs[b];
                            float env = Mathf.Exp(-0.70f * age);
                            float flutter = 1.0f + 0.16f * Mathf.Sin(2f * Mathf.PI * 5.0f * age);
                            float bTone = Mathf.Sin(2f * Mathf.PI * bf * age) * 0.65f +
                                          Mathf.Sin(2f * Mathf.PI * (bf * 2.76f) * age) * 0.35f;
                            bells += bTone * env * flutter * 0.32f;
                        }
                    }
                }

                // 3. Delicate Koto / Bamboo Pluck Droplets
                float plucks = 0f;
                for (int p = 0; p < pluckTimes.Length; p++)
                {
                    float pt = pluckTimes[p];
                    if (t >= pt)
                    {
                        float age = t - pt;
                        if (age < 2.5f)
                        {
                            float pf = pluckFreqs[p];
                            float env = Mathf.Exp(-3.4f * age);
                            float pTone = Mathf.Sin(2f * Mathf.PI * pf * age) * 0.70f +
                                          Mathf.Sin(2f * Mathf.PI * pf * 2.0f * age) * 0.30f;
                            plucks += pTone * env * 0.22f;
                        }
                    }
                }

                // 4. Global Envelope Fade In and Fade Out
                float masterEnv = 1f;
                if (t < 2.0f)
                {
                    masterEnv = Mathf.SmoothStep(0f, 1f, t / 2.0f);
                }
                else if (t > duration - 2.5f)
                {
                    masterEnv = Mathf.SmoothStep(1f, 0f, (t - (duration - 2.5f)) / 2.5f);
                }

                float mixed = (pad + bells + plucks) * masterEnv * 0.80f;
                samples[i] = Mathf.Clamp(mixed, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
