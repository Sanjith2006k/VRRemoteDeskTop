using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VRRemoteDesktop.Audio;
using VRRemoteDesktop.UI;

namespace VRRemoteDesktop.Environment
{
    public enum EnvironmentTheme
    {
        CyberpunkSciFi = 0,    // High-tech cyber command deck, neon cyan/magenta, data core hologram
        DeepSpaceStation = 1,  // Deep space orbital observatory, cosmic stars & nebula vista, zero-g drone
        NordicForestCabin = 2, // Cozy alpine timber cabin, snow/pine vista, warm hearth fireplace glow & crackling embers
        SunsetPenthouse = 3    // Golden hour dusk horizon, autumn foliage, sunset amber lighting, twilight acoustic calm
    }

    /// <summary>
    /// Master Multi-Environment System fulfilling Phase 3.
    /// Seamlessly switches the entire workstation atmosphere, outdoor vista, room lighting,
    /// wall trim emissives, holographic core mode, Ambilight bias lighting, and 3D spatial soundscapes
    /// between 4 fully realized virtual environments:
    /// 1. Cyberpunk Sci-Fi Workstation (Default high-tech neon aesthetic)
    /// 2. Deep Space Orbital Station (Cosmic stars, nebula, zero-g drone, planetary core)
    /// 3. Nordic Forest Cabin (Cozy wood textures, crackling fireplace embers, mountain pines, warm hearth)
    /// 4. Sunset Penthouse (Golden hour dusk, drifting autumn foliage, amber ambilight, lo-fi twilight calm)
    ///
    /// Controls:
    /// - [F6] Hotkey anywhere in desktop / VR mode
    /// - Physical Tactile Button on the workstation desk console
    /// - Dynamic Screen-Space HUD feedback banner and bottom control strip
    /// </summary>
    public class EnvironmentThemeController : MonoBehaviour
    {
        public static EnvironmentThemeController Instance { get; private set; }

        [Header("Active Environment Theme")]
        [SerializeField] private EnvironmentTheme _currentTheme = EnvironmentTheme.CyberpunkSciFi;
        [SerializeField] private float _transitionDuration = 1.2f;

        [Header("Audio Soundscape Engine")]
        [SerializeField] private AudioSource _themeAudioSource;
        [SerializeField] private float _soundscapeVolume = 0.55f;

        // Scene References
        private GameObject _natureLandscape;
        private List<GameObject> _terrestrialObjects = new List<GameObject>();
        private GameObject _cosmicBackdrop;
        private Light _sunLight;
        private WeatherController _weatherController;
        private Coroutine _transitionRoutine;

        // Procedural Audio Clips Cache
        private AudioClip _cyberpunkClip;
        private AudioClip _deepSpaceClip;
        private AudioClip _cabinClip;
        private AudioClip _sunsetClip;

        public EnvironmentTheme CurrentTheme => _currentTheme;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureInstance();
        }

        public static EnvironmentThemeController EnsureInstance()
        {
            if (Instance != null) return Instance;
            Instance = FindAnyObjectByType<EnvironmentThemeController>();
            if (Instance != null) return Instance;

            GameObject go = new GameObject("EnvironmentTheme_MasterController");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<EnvironmentThemeController>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupAudio();
            ResolveSceneReferences();
        }

        private void Start()
        {
            ResolveSceneReferences();
            ApplyThemeImmediate(_currentTheme);
        }

        private void SetupAudio()
        {
            _themeAudioSource = gameObject.GetComponent<AudioSource>();
            if (_themeAudioSource == null) _themeAudioSource = gameObject.AddComponent<AudioSource>();

            _themeAudioSource.loop = true;
            _themeAudioSource.playOnAwake = false;
            _themeAudioSource.spatialBlend = 0.35f;
            _themeAudioSource.volume = _soundscapeVolume;
            _themeAudioSource.rolloffMode = AudioRolloffMode.Linear;
            _themeAudioSource.minDistance = 2f;
            _themeAudioSource.maxDistance = 15f;
        }

        public void ResolveSceneReferences()
        {
            if (_natureLandscape == null)
            {
                _natureLandscape = GameObject.Find("Nature3DLandscape");
            }

            if (_cosmicBackdrop == null)
            {
                _cosmicBackdrop = GameObject.Find("DistantHorizonBackdrop");
            }

            if (_sunLight == null)
            {
                var sunObj = GameObject.Find("NaturalSunlight");
                if (sunObj != null) _sunLight = sunObj.GetComponent<Light>();
                if (_sunLight == null)
                {
                    var lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
                    foreach (var l in lights)
                    {
                        if (l.type == LightType.Directional)
                        {
                            _sunLight = l;
                            break;
                        }
                    }
                }
            }

            if (_weatherController == null)
            {
                _weatherController = FindAnyObjectByType<WeatherController>();
            }

            // Cache terrestrial alpine objects under Nature3DLandscape
            if (_natureLandscape != null && _terrestrialObjects.Count == 0)
            {
                string[] terrestrialNames = new string[]
                {
                    "ValleyTerrain",
                    "WindingRiver",
                    "AlpineMountainRange",
                    "PineForest",
                    "AlpineCottages",
                    "WeatherParticles",
                    "WeatherParticles_Rain",
                    "WeatherParticles_Snow",
                    "WeatherParticles_Autumn"
                };

                foreach (var name in terrestrialNames)
                {
                    Transform t = _natureLandscape.transform.Find(name);
                    if (t != null && !t.gameObject.name.Contains("DistantHorizonBackdrop"))
                    {
                        _terrestrialObjects.Add(t.gameObject);
                    }
                    else
                    {
                        var go = GameObject.Find(name);
                        if (go != null && !_terrestrialObjects.Contains(go))
                        {
                            _terrestrialObjects.Add(go);
                        }
                    }
                }
            }
        }

        private void Update()
        {
            // Keyboard Shortcut [F6] to cycle environment themes
            var kb = Keyboard.current;
            if (kb != null && kb.f6Key.wasPressedThisFrame)
            {
                CycleTheme();
            }
        }

        public void CycleTheme()
        {
            int total = Enum.GetValues(typeof(EnvironmentTheme)).Length;
            int next = ((int)_currentTheme + 1) % total;
            SetTheme((EnvironmentTheme)next);
        }

        public void SetTheme(EnvironmentTheme theme)
        {
            _currentTheme = theme;
            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(TransitionThemeRoutine(theme));

            // Notify Desktop HUD & HUD help strip
            string name = GetThemeDisplayName(theme);
            string desc = GetThemeSubtitle(theme);
            Color accent = GetThemeAccentColor(theme);

            DesktopModeHUD.Instance?.ShowNotificationBanner($"ENVIRONMENT: {name.ToUpper()}", desc, accent);
            UpdateHUDHelpStrip();

            // Audio prompt chime
            AudioManager.Instance?.PlayConnectChime();
        }

        public void ApplyThemeImmediate(EnvironmentTheme theme)
        {
            _currentTheme = theme;
            ApplyThemeVisuals(theme, 1.0f);
            PlayThemeSoundscape(theme, false);
            UpdateHUDHelpStrip();
        }

        private IEnumerator TransitionThemeRoutine(EnvironmentTheme theme)
        {
            ResolveSceneReferences();

            // Crossfade audio soundscape
            PlayThemeSoundscape(theme, true);

            float elapsed = 0f;
            while (elapsed < _transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _transitionDuration);
                ApplyThemeVisuals(theme, t);
                yield return null;
            }

            ApplyThemeVisuals(theme, 1.0f);
        }

        private void ApplyThemeVisuals(EnvironmentTheme theme, float progress)
        {
            switch (theme)
            {
                case EnvironmentTheme.CyberpunkSciFi:
                    ApplyCyberpunkTheme(progress);
                    break;

                case EnvironmentTheme.DeepSpaceStation:
                    ApplyDeepSpaceTheme(progress);
                    break;

                case EnvironmentTheme.NordicForestCabin:
                    ApplyCabinTheme(progress);
                    break;

                case EnvironmentTheme.SunsetPenthouse:
                    ApplySunsetTheme(progress);
                    break;
            }
        }

        #region Theme Implementations

        private void ApplyCyberpunkSciFi(float t)
        {
            ApplyCyberpunkTheme(t);
        }

        private void ApplyCyberpunkTheme(float t)
        {
            // 1. Show Terrestrial Alpine Mountain Landscape
            SetTerrestrialObjectsActive(true);

            // 2. Weather & Sun
            if (_weatherController != null && t >= 0.5f)
            {
                _weatherController.SetWeather(WeatherType.Sunny);
            }

            if (_sunLight != null)
            {
                _sunLight.color = Color.Lerp(_sunLight.color, new Color(0.85f, 0.95f, 1.0f), t);
                _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, 18.0f, t);
            }

            // 3. Room Lighting (Cyberpunk Neon)
            if (RoomLightingController.Instance != null && t >= 0.9f)
            {
                RoomLightingController.Instance.ApplyPreset(LightingPreset.CyberpunkNeon);
            }

            // 4. Hologram Core
            if (HologramController.Instance != null && t >= 0.8f)
            {
                HologramController.Instance.SetMode(HologramController.HologramMode.CyberTesseract);
            }

            // 5. Workstation Ambilight
            if (WorkstationAmbilightController.Instance != null && t >= 0.8f)
            {
                WorkstationAmbilightController.Instance.SetMode(AmbilightMode.ReactiveScreen);
            }
        }

        private void ApplyDeepSpaceTheme(float t)
        {
            // 1. Hide Terrestrial Meshes to unveil the infinite Cosmic Vista
            SetTerrestrialObjectsActive(false);

            // 2. Sun becomes deep space cosmic starlight
            if (_sunLight != null)
            {
                _sunLight.color = Color.Lerp(_sunLight.color, new Color(0.25f, 0.50f, 0.95f), t);
                _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, 6.0f, t);
            }

            // 3. Room Lighting (Deep Space Blue)
            if (RoomLightingController.Instance != null && t >= 0.9f)
            {
                RoomLightingController.Instance.ApplyPreset(LightingPreset.DeepSpaceBlue);
            }

            // 4. Hologram Core (Orbital Planetary Simulation)
            if (HologramController.Instance != null && t >= 0.8f)
            {
                HologramController.Instance.SetMode(HologramController.HologramMode.OrbitalCore);
            }

            // 5. Workstation Ambilight (Deep Space Blue Ambient Glow)
            if (WorkstationAmbilightController.Instance != null && t >= 0.8f)
            {
                WorkstationAmbilightController.Instance.SetMode(AmbilightMode.CyberpunkNeon);
            }
        }

        private void ApplyCabinTheme(float t)
        {
            // 1. Show Terrestrial Alpine Mountain Landscape
            SetTerrestrialObjectsActive(true);

            // 2. Weather: Snowy mountain pines & warm sun
            if (_weatherController != null && t >= 0.5f)
            {
                _weatherController.SetWeather(WeatherType.Snow);
            }

            if (_sunLight != null)
            {
                _sunLight.color = Color.Lerp(_sunLight.color, new Color(1.0f, 0.82f, 0.60f), t);
                _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, 15.0f, t);
            }

            // 3. Room Lighting (Warm Hearth Fireplace Sunset Amber)
            if (RoomLightingController.Instance != null && t >= 0.9f)
            {
                RoomLightingController.Instance.ApplyPreset(LightingPreset.SunsetAmber);
            }

            // 4. Hologram Core (Subtle gentle orbit)
            if (HologramController.Instance != null && t >= 0.8f)
            {
                HologramController.Instance.SetMode(HologramController.HologramMode.OrbitalCore);
            }

            // 5. Workstation Ambilight (Warm Fireplace Amber Bias Glow)
            if (WorkstationAmbilightController.Instance != null && t >= 0.8f)
            {
                WorkstationAmbilightController.Instance.SetMode(AmbilightMode.SunsetAmber);
            }
        }

        private void ApplySunsetTheme(float t)
        {
            // 1. Show Terrestrial Alpine Landscape
            SetTerrestrialObjectsActive(true);

            // 2. Weather: Golden Autumn leaves & golden hour lighting
            if (_weatherController != null && t >= 0.5f)
            {
                _weatherController.SetWeather(WeatherType.Autumn);
            }

            if (_sunLight != null)
            {
                _sunLight.color = Color.Lerp(_sunLight.color, new Color(1.0f, 0.60f, 0.28f), t);
                _sunLight.intensity = Mathf.Lerp(_sunLight.intensity, 14.0f, t);
            }

            // 3. Room Lighting (Sunset Amber / Dusk Rose)
            if (RoomLightingController.Instance != null && t >= 0.9f)
            {
                RoomLightingController.Instance.ApplyPreset(LightingPreset.SunsetAmber);
            }

            // 4. Hologram Core (Spatial Satellite Array)
            if (HologramController.Instance != null && t >= 0.8f)
            {
                HologramController.Instance.SetMode(HologramController.HologramMode.SpatialSatellite);
            }

            // 5. Workstation Ambilight (Sunset Amber)
            if (WorkstationAmbilightController.Instance != null && t >= 0.8f)
            {
                WorkstationAmbilightController.Instance.SetMode(AmbilightMode.SunsetAmber);
            }
        }

        private void SetTerrestrialObjectsActive(bool active)
        {
            if (_terrestrialObjects == null) return;
            for (int i = 0; i < _terrestrialObjects.Count; i++)
            {
                if (_terrestrialObjects[i] != null && _terrestrialObjects[i].activeSelf != active)
                {
                    _terrestrialObjects[i].SetActive(active);
                }
            }

            if (_cosmicBackdrop != null && !_cosmicBackdrop.activeSelf)
            {
                _cosmicBackdrop.SetActive(true);
            }
        }

        #endregion

        #region Procedural Soundscape Engine

        private void PlayThemeSoundscape(EnvironmentTheme theme, bool crossfade)
        {
            if (_themeAudioSource == null) SetupAudio();

            AudioClip targetClip = GetThemeAudioClip(theme);
            if (targetClip == null) return;

            if (crossfade && _themeAudioSource.isPlaying)
            {
                StartCoroutine(CrossfadeAudioRoutine(targetClip));
            }
            else
            {
                _themeAudioSource.clip = targetClip;
                _themeAudioSource.volume = _soundscapeVolume;
                _themeAudioSource.Play();
            }
        }

        private IEnumerator CrossfadeAudioRoutine(AudioClip newClip)
        {
            float half = _transitionDuration * 0.5f;
            float elapsed = 0f;

            // Fade Out
            float startVol = _themeAudioSource.volume;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                _themeAudioSource.volume = Mathf.Lerp(startVol, 0.02f, elapsed / half);
                yield return null;
            }

            // Switch Clip
            _themeAudioSource.clip = newClip;
            _themeAudioSource.Play();

            // Fade In
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                _themeAudioSource.volume = Mathf.Lerp(0.02f, _soundscapeVolume, elapsed / half);
                yield return null;
            }
            _themeAudioSource.volume = _soundscapeVolume;
        }

        private AudioClip GetThemeAudioClip(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.CyberpunkSciFi:
                    if (_cyberpunkClip == null) _cyberpunkClip = GenerateCyberLabHum();
                    return _cyberpunkClip;

                case EnvironmentTheme.DeepSpaceStation:
                    if (_deepSpaceClip == null) _deepSpaceClip = GenerateDeepSpaceDrone();
                    return _deepSpaceClip;

                case EnvironmentTheme.NordicForestCabin:
                    if (_cabinClip == null) _cabinClip = GenerateFireplaceWoodEmbers();
                    return _cabinClip;

                case EnvironmentTheme.SunsetPenthouse:
                    if (_sunsetClip == null) _sunsetClip = GenerateSunsetTwilight();
                    return _sunsetClip;
            }
            return null;
        }

        private AudioClip GenerateCyberLabHum()
        {
            int sampleRate = 44100;
            float duration = 12.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;

                // 60 Hz cyber transformer hum + 120 Hz overtone
                float hum60 = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.12f;
                float hum120 = Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.05f;

                // Subtle resonant pulse (data frequency sweep)
                float pulseLfo = Mathf.Sin(t * 0.8f) * 0.025f;
                float pulse = Mathf.Sin(2f * Mathf.PI * 440f * t + pulseLfo) * 0.015f;

                // Atmospheric cyber white noise hiss
                float noise = (Mathf.Sin(i * 15.234f + 21.4f) * 41235.1f);
                noise = (noise - Mathf.Floor(noise)) * 2f - 1f;

                samples[i] = Mathf.Clamp((hum60 + hum120 + pulse + noise * 0.015f) * 0.75f, -0.9f, 0.9f);
            }

            AudioClip clip = AudioClip.Create("Soundscape_Cyberpunk", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateDeepSpaceDrone()
        {
            int sampleRate = 44100;
            float duration = 14.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;

                // 55 Hz sub-bass foundation (deep space zero-g resonance)
                float drone55 = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.16f;
                float drone110 = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.08f * (Mathf.Sin(t * 0.35f) * 0.3f + 0.7f);

                // Cosmic stellar frequency sweep
                float sweep = Mathf.Sin(2f * Mathf.PI * (165f + Mathf.Sin(t * 0.25f) * 12f) * t) * 0.04f;

                // Crystalline pulsar telemetry pings at 4s and 10s
                float ping = 0f;
                if ((t > 4.0f && t < 4.8f) || (t > 10.0f && t < 10.8f))
                {
                    float pT = (t > 7f) ? (t - 10.0f) : (t - 4.0f);
                    ping = Mathf.Sin(2f * Mathf.PI * 2150f * t) * Mathf.Exp(-pT * 5f) * 0.06f;
                }

                samples[i] = Mathf.Clamp((drone55 + drone110 + sweep + ping) * 0.75f, -0.9f, 0.9f);
            }

            AudioClip clip = AudioClip.Create("Soundscape_DeepSpace", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateFireplaceWoodEmbers()
        {
            int sampleRate = 44100;
            float duration = 14.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float rF1 = 0f, rF2 = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (Mathf.Sin(i * 13.918f + 81.2f) * 37819.3f);
                noise = (noise - Mathf.Floor(noise)) * 2f - 1f;

                // Low wood hearth warmth rumble
                rF1 += (noise - rF1) * 0.12f;
                rF2 += (rF1 - rF2) * 0.15f;
                float rumble = rF2 * 0.15f;

                // Crackling fireplace pops and ember snaps
                float popProb = Mathf.Sin(i * 87.234f) * 23891.2f;
                popProb = popProb - Mathf.Floor(popProb);
                float pop = 0f;
                if (popProb > 0.9984f)
                {
                    float snapFreq = 1600f + popProb * 800f;
                    pop = (popProb - 0.9984f) * 240f * Mathf.Sin(2f * Mathf.PI * snapFreq * t);
                }

                // Gentle mountain pine wind blowing past cabin eaves
                float windLfo = Mathf.Sin(t * 0.40f) * 0.3f + 0.7f;
                float wind = Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.035f * windLfo;

                samples[i] = Mathf.Clamp((rumble + pop + wind) * 0.80f, -0.9f, 0.9f);
            }

            AudioClip clip = AudioClip.Create("Soundscape_FireplaceCabin", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateSunsetTwilight()
        {
            int sampleRate = 44100;
            float duration = 14.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;

                // Soft evening dusk breeze
                float breezeLfo = Mathf.Sin(t * 0.5f) * 0.35f + 0.65f;
                float breeze = Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.04f * breezeLfo;

                // Twilight cricket chirp pulses at 2.5s, 6.0s, 9.5s
                float cricket = 0f;
                if ((t > 2.5f && t < 3.2f) || (t > 6.0f && t < 6.7f) || (t > 9.5f && t < 10.2f))
                {
                    float pulse = Mathf.Sin(2f * Mathf.PI * 4500f * t) * (Mathf.Sin(t * 80f) > 0f ? 1f : 0f) * 0.035f;
                    cricket = pulse;
                }

                // Low warm acoustic resonance
                float acoustic = Mathf.Sin(2f * Mathf.PI * 92f * t) * 0.03f;

                samples[i] = Mathf.Clamp((breeze + cricket + acoustic) * 0.75f, -0.9f, 0.9f);
            }

            AudioClip clip = AudioClip.Create("Soundscape_SunsetTwilight", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        #endregion

        #region Helpers & Metadata

        public string GetThemeDisplayName(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.CyberpunkSciFi: return "Cyberpunk Sci-Fi";
                case EnvironmentTheme.DeepSpaceStation: return "Deep Space Orbital Station";
                case EnvironmentTheme.NordicForestCabin: return "Nordic Forest Cabin";
                case EnvironmentTheme.SunsetPenthouse: return "Sunset Skyline Penthouse";
            }
            return theme.ToString();
        }

        public string GetThemeSubtitle(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.CyberpunkSciFi:
                    return "Neon Cyan & Magenta High-Tech Command Deck";
                case EnvironmentTheme.DeepSpaceStation:
                    return "Infinite Cosmic Stars, Nebula Vista & Zero-G Telemetry";
                case EnvironmentTheme.NordicForestCabin:
                    return "Cozy Timber Cabin, Crackling Fireplace & Snowy Alpine Pines";
                case EnvironmentTheme.SunsetPenthouse:
                    return "Golden Hour Dusk Horizon, Drifting Foliage & Twilight Calm";
            }
            return "Virtual Atmosphere Profile";
        }

        public Color GetThemeAccentColor(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.CyberpunkSciFi: return new Color(0f, 1f, 1f, 1f); // Neon Cyan
                case EnvironmentTheme.DeepSpaceStation: return new Color(0.2f, 0.6f, 1f, 1f); // Cobalt Blue
                case EnvironmentTheme.NordicForestCabin: return new Color(1f, 0.7f, 0.2f, 1f); // Warm Hearth Amber
                case EnvironmentTheme.SunsetPenthouse: return new Color(1f, 0.45f, 0.2f, 1f); // Sunset Rose Gold
            }
            return Color.cyan;
        }

        private void UpdateHUDHelpStrip()
        {
            string themeName = GetThemeDisplayName(_currentTheme);
            string ambilightMode = WorkstationAmbilightController.Instance != null 
                ? WorkstationAmbilightController.Instance.CurrentMode.ToString() 
                : "Active";

            DesktopModeHUD.Instance?.UpdateBottomHelp(ambilightMode, themeName);
        }

        #endregion
    }
}

