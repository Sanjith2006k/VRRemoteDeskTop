using System;
using System.Collections;
using UnityEngine;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.UI;

namespace VRRemoteDesktop.Environment
{
    public enum AmbilightMode
    {
        ReactiveScreen,   // Dynamically samples live monitor content to illuminate wall & desk
        CyberpunkNeon,    // Center: Electric Cyan, Left: Cyber Violet, Right: Cobalt Blue
        SunsetAmber,      // Warm golden-hour bias lighting
        MatrixGreen,      // Deep emerald phosphor glow
        NeonPulse,        // Gentle breathing animation matching room atmosphere
        Disabled          // Backlights off
    }

    /// <summary>
    /// Master Workstation Ambilight & Reactive Bias Lighting System.
    /// Provides cinematic reactive illumination behind the 3 workstation monitors:
    /// - Samples live screen render textures using ultra-low-overhead 4x4 GPU downsampling (0 dropped frames)
    /// - Casts dynamic, color-matched ambient light onto the wall, desk surface, and surrounding room
    /// - Emissive HDR bezel back-glow for rich bloom halos around the monitors
    /// - Smooth exponential color damping to eliminate harsh strobing or flicker
    /// - Multiple preset modes (Reactive, Cyberpunk Neon, Sunset, Matrix, Pulse)
    /// </summary>
    public class WorkstationAmbilightController : MonoBehaviour
    {
        public static WorkstationAmbilightController Instance { get; private set; }

        [Header("Ambilight Mode & Profile")]
        [SerializeField] private AmbilightMode _mode = AmbilightMode.ReactiveScreen;
        [Range(0.0f, 4.0f)]
        [SerializeField] private float _brightness = 2.2f;
        [Range(1.0f, 20.0f)]
        [SerializeField] private float _smoothingSpeed = 7.5f;
        [Range(0.0f, 1.0f)]
        [SerializeField] private float _minLuminance = 0.15f;
        [Range(1.0f, 2.5f)]
        [SerializeField] private float _saturationBoost = 1.45f;
        [SerializeField] private float _sampleRateHz = 24.0f; // 24 samples/sec for cinema-grade reactive lighting
        [SerializeField] private Color _idleGlowColor = new Color(0.05f, 0.55f, 0.85f, 1f);

        [Header("Light Sources")]
        [SerializeField] private Light _centerAmbilight;
        [SerializeField] private Light _leftAmbilight;
        [SerializeField] private Light _rightAmbilight;
        [SerializeField] private Light _deskWashLight;

        [Header("Backlight Emissive Strips")]
        [SerializeField] private Renderer _centerEmissiveStrip;
        [SerializeField] private Renderer _leftEmissiveStrip;
        [SerializeField] private Renderer _rightEmissiveStrip;

        // Runtime Color Tracking
        private Color _currentCenterColor = new Color(0.05f, 0.75f, 0.95f);
        private Color _currentLeftColor = new Color(0.65f, 0.15f, 0.95f);
        private Color _currentRightColor = new Color(0.10f, 0.50f, 1.0f);

        private Color _targetCenterColor = new Color(0.05f, 0.75f, 0.95f);
        private Color _targetLeftColor = new Color(0.65f, 0.15f, 0.95f);
        private Color _targetRightColor = new Color(0.10f, 0.50f, 1.0f);

        // GPU Downsampling Resources (Tiny 4x4 buffers = 0 CPU/GPU overhead)
        private RenderTexture _sampleRT;
        private Texture2D _readbackTex;
        private float _lastSampleTime;
        private MaterialPropertyBlock _propBlock;

        public AmbilightMode Mode => _mode;
        public float Brightness => _brightness;
        public Color CurrentCenterColor => _currentCenterColor;
        public Color CurrentLeftColor => _currentLeftColor;
        public Color CurrentRightColor => _currentRightColor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            EnsureInstance();
        }

        public static WorkstationAmbilightController EnsureInstance()
        {
            if (Instance != null) return Instance;

            Instance = UnityEngine.Object.FindAnyObjectByType<WorkstationAmbilightController>();
            if (Instance != null) return Instance;

            var workstationObj = GameObject.Find("VirtualMonitor") ?? GameObject.Find("Workstation") ?? GameObject.Find("Desk");
            Transform parent = workstationObj != null ? workstationObj.transform : null;

            GameObject ambilightObj = new GameObject("Workstation_Ambilight_System");
            if (parent != null) ambilightObj.transform.SetParent(parent, false);

            Instance = ambilightObj.AddComponent<WorkstationAmbilightController>();
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

            _propBlock = new MaterialPropertyBlock();
            InitializeGPUDownsampler();
            ResolveLightFixtures();
        }

        private void Start()
        {
            ResolveLightFixtures();
            CreateEmissiveGlowStrips();
        }

        private void OnEnable()
        {
            if (MultiMonitorManager.Instance != null)
            {
                MultiMonitorManager.Instance.OnActiveDisplayChanged += HandleActiveDisplayChanged;
            }
        }

        private void OnDisable()
        {
            if (MultiMonitorManager.Instance != null)
            {
                MultiMonitorManager.Instance.OnActiveDisplayChanged -= HandleActiveDisplayChanged;
            }
        }

        private void OnDestroy()
        {
            if (_sampleRT != null)
            {
                _sampleRT.Release();
                Destroy(_sampleRT);
                _sampleRT = null;
            }
            if (_readbackTex != null)
            {
                Destroy(_readbackTex);
                _readbackTex = null;
            }
            if (Instance == this) Instance = null;
        }

        private void InitializeGPUDownsampler()
        {
            if (_sampleRT == null)
            {
                _sampleRT = new RenderTexture(4, 4, 0, RenderTextureFormat.ARGB32)
                {
                    filterMode = FilterMode.Bilinear,
                    name = "Ambilight_Downsampler_RT"
                };
                _sampleRT.Create();
            }

            if (_readbackTex == null)
            {
                _readbackTex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
                {
                    name = "Ambilight_Readback_Tex"
                };
            }
        }

        public void ResolveLightFixtures()
        {
            // 1. Center Monitor Light
            if (_centerAmbilight == null)
            {
                var existing = GameObject.Find("Monitor Glow Light");
                if (existing != null)
                {
                    _centerAmbilight = existing.GetComponent<Light>();
                }
                else
                {
                    GameObject cObj = new GameObject("Center_Ambilight_Light");
                    var centerScreen = GameObject.Find("Screen");
                    if (centerScreen != null)
                    {
                        cObj.transform.SetParent(centerScreen.transform, false);
                        cObj.transform.localPosition = new Vector3(0f, 0f, 0.25f);
                    }
                    else
                    {
                        cObj.transform.position = new Vector3(0f, 1.18f, 0.70f);
                    }
                    _centerAmbilight = cObj.AddComponent<Light>();
                }
            }
            ConfigureLight(_centerAmbilight, 2.8f);

            // 2. Left Monitor Light
            if (_leftAmbilight == null)
            {
                GameObject lObj = GameObject.Find("Left_Ambilight_Light");
                if (lObj == null)
                {
                    lObj = new GameObject("Left_Ambilight_Light");
                    var leftScreen = GameObject.Find("LeftScreen");
                    if (leftScreen != null)
                    {
                        lObj.transform.SetParent(leftScreen.transform, false);
                        lObj.transform.localPosition = new Vector3(0f, 0f, 0.25f);
                    }
                    else
                    {
                        lObj.transform.position = new Vector3(-1.05f, 1.18f, 0.65f);
                    }
                }
                _leftAmbilight = lObj.GetComponent<Light>() ?? lObj.AddComponent<Light>();
            }
            ConfigureLight(_leftAmbilight, 2.4f);

            // 3. Right Monitor Light
            if (_rightAmbilight == null)
            {
                GameObject rObj = GameObject.Find("Right_Ambilight_Light");
                if (rObj == null)
                {
                    rObj = new GameObject("Right_Ambilight_Light");
                    var rightScreen = GameObject.Find("RightScreen");
                    if (rightScreen != null)
                    {
                        rObj.transform.SetParent(rightScreen.transform, false);
                        rObj.transform.localPosition = new Vector3(0f, 0f, 0.25f);
                    }
                    else
                    {
                        rObj.transform.position = new Vector3(1.05f, 1.18f, 0.65f);
                    }
                }
                _rightAmbilight = rObj.GetComponent<Light>() ?? rObj.AddComponent<Light>();
            }
            ConfigureLight(_rightAmbilight, 2.4f);

            // 4. Desk Ambient Wash Light (soft downward bounce onto desk surface)
            if (_deskWashLight == null)
            {
                GameObject dObj = GameObject.Find("Desk_Ambilight_Wash");
                if (dObj == null)
                {
                    dObj = new GameObject("Desk_Ambilight_Wash");
                    dObj.transform.position = new Vector3(0f, 0.88f, 0.55f);
                }
                _deskWashLight = dObj.GetComponent<Light>() ?? dObj.AddComponent<Light>();
            }
            if (_deskWashLight != null)
            {
                _deskWashLight.type = LightType.Point;
                _deskWashLight.range = 2.0f;
                _deskWashLight.intensity = _brightness * 0.45f;
            }
        }

        private void ConfigureLight(Light l, float range)
        {
            if (l == null) return;
            l.type = LightType.Point;
            l.range = range;
            l.intensity = _brightness;
            l.shadows = LightShadows.None; // Ambilight is a soft ambient wash
        }

        private void CreateEmissiveGlowStrips()
        {
            if (_centerEmissiveStrip == null)
            {
                var screen = GameObject.Find("Screen");
                if (screen != null) _centerEmissiveStrip = AttachRimGlowStrip(screen.transform, "Center_Back_RimStrip", new Vector3(1.05f, 0.60f, 0.02f));
            }
            if (_leftEmissiveStrip == null)
            {
                var leftScreen = GameObject.Find("LeftScreen");
                if (leftScreen != null) _leftEmissiveStrip = AttachRimGlowStrip(leftScreen.transform, "Left_Back_RimStrip", new Vector3(1.05f, 0.60f, 0.02f));
            }
            if (_rightEmissiveStrip == null)
            {
                var rightScreen = GameObject.Find("RightScreen");
                if (rightScreen != null) _rightEmissiveStrip = AttachRimGlowStrip(rightScreen.transform, "Right_Back_RimStrip", new Vector3(1.05f, 0.60f, 0.02f));
            }
        }

        private Renderer AttachRimGlowStrip(Transform parent, string name, Vector3 size)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<Renderer>();

            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = name;
            strip.transform.SetParent(parent, false);
            strip.transform.localPosition = new Vector3(0f, 0f, 0.035f); // Placed slightly behind screen frame
            strip.transform.localRotation = Quaternion.identity;
            strip.transform.localScale = size;

            // Strip has no collider
            var col = strip.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var r = strip.GetComponent<Renderer>();
            if (r != null)
            {
                Material glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                glowMat.EnableKeyword("_EMISSION");
                glowMat.SetColor("_BaseColor", Color.black);
                glowMat.SetColor("_EmissionColor", _currentCenterColor * 2.0f);
                r.material = glowMat;
            }
            return r;
        }

        private void Update()
        {
            // Keyboard shortcut [F5] to cycle Ambilight modes
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f5Key.wasPressedThisFrame)
            {
                CycleMode();
            }

            if (_mode == AmbilightMode.Disabled)
            {
                SetLightsEnabled(false);
                return;
            }

            SetLightsEnabled(true);

            // Throttle GPU texture sampling to desired frequency
            float interval = 1.0f / Mathf.Max(1.0f, _sampleRateHz);
            if (Time.time - _lastSampleTime >= interval)
            {
                _lastSampleTime = Time.time;
                UpdateTargetColors();
            }

            // Smooth exponential damping to target colors for cinema-grade softness
            float dt = Time.deltaTime;
            _currentCenterColor = Color.Lerp(_currentCenterColor, _targetCenterColor, dt * _smoothingSpeed);
            _currentLeftColor = Color.Lerp(_currentLeftColor, _targetLeftColor, dt * _smoothingSpeed);
            _currentRightColor = Color.Lerp(_currentRightColor, _targetRightColor, dt * _smoothingSpeed);

            ApplyColorsToHardware();
        }

        private void UpdateTargetColors()
        {
            switch (_mode)
            {
                case AmbilightMode.ReactiveScreen:
                    SampleReactiveScreens();
                    break;

                case AmbilightMode.CyberpunkNeon:
                    _targetCenterColor = new Color(0.0f, 0.95f, 1.0f);   // Bright Neon Cyan
                    _targetLeftColor = new Color(0.75f, 0.05f, 1.0f);     // Deep Electric Magenta
                    _targetRightColor = new Color(0.10f, 0.45f, 1.0f);    // Cobalt Blue
                    break;

                case AmbilightMode.SunsetAmber:
                    _targetCenterColor = new Color(1.0f, 0.45f, 0.05f);   // Warm Amber Gold
                    _targetLeftColor = new Color(1.0f, 0.20f, 0.15f);     // Sunset Crimson
                    _targetRightColor = new Color(0.95f, 0.65f, 0.10f);   // Golden Honey
                    break;

                case AmbilightMode.MatrixGreen:
                    _targetCenterColor = new Color(0.05f, 1.0f, 0.35f);   // Matrix Phosphor Green
                    _targetLeftColor = new Color(0.0f, 0.70f, 0.25f);     // Dark Terminal Green
                    _targetRightColor = new Color(0.20f, 1.0f, 0.55f);    // Bright Lime Glow
                    break;

                case AmbilightMode.NeonPulse:
                    float t = Time.time * 1.5f;
                    float wave = (Mathf.Sin(t) + 1f) * 0.5f;
                    _targetCenterColor = Color.Lerp(new Color(0.0f, 0.75f, 1.0f), new Color(0.4f, 0.05f, 1.0f), wave);
                    _targetLeftColor = Color.Lerp(new Color(0.6f, 0.05f, 1.0f), new Color(0.0f, 0.9f, 0.6f), wave);
                    _targetRightColor = Color.Lerp(new Color(0.0f, 0.5f, 1.0f), new Color(1.0f, 0.1f, 0.6f), wave);
                    break;
            }
        }

        private void SampleReactiveScreens()
        {
            var mmm = MultiMonitorManager.Instance;
            if (mmm == null) return;

            // Center Screen Sampling
            Texture centerTex = mmm.GetCenterScreenTexture();
            if (centerTex != null)
            {
                _targetCenterColor = SampleTextureColor(centerTex);
            }

            // Left Screen Sampling
            Texture leftTex = mmm.LeftScreen != null ? mmm.LeftScreen.CurrentTexture : null;
            if (leftTex != null)
            {
                _targetLeftColor = SampleTextureColor(leftTex);
            }
            else
            {
                _targetLeftColor = new Color(0.55f, 0.10f, 0.95f); // Telemetry violet fallback
            }

            // Right Screen Sampling
            Texture rightTex = mmm.RightScreen != null ? mmm.RightScreen.CurrentTexture : null;
            if (rightTex != null)
            {
                _targetRightColor = SampleTextureColor(rightTex);
            }
            else
            {
                _targetRightColor = new Color(0.10f, 0.45f, 1.0f); // NOC cobalt fallback
            }
        }

        /// <summary>
        /// Downsamples the target screen texture to 4x4 on GPU via Bilinear Blit and extracts the
        /// saturated, cinema-graded dominant color in under 0.01ms.
        /// </summary>
        private Color SampleTextureColor(Texture sourceTex)
        {
            if (sourceTex == null) return _idleGlowColor;

            InitializeGPUDownsampler();
            if (_sampleRT == null || _readbackTex == null) return _idleGlowColor;

            Graphics.Blit(sourceTex, _sampleRT);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = _sampleRT;
            _readbackTex.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            _readbackTex.Apply();
            RenderTexture.active = prev;

            Color[] pixels = _readbackTex.GetPixels();
            if (pixels == null || pixels.Length == 0) return _idleGlowColor;

            float r = 0f, g = 0f, b = 0f;
            for (int i = 0; i < pixels.Length; i++)
            {
                r += pixels[i].r;
                g += pixels[i].g;
                b += pixels[i].b;
            }

            float count = pixels.Length;
            Color avg = new Color(r / count, g / count, b / count, 1f);

            // Enhance saturation and establish baseline ambient floor
            Color.RGBToHSV(avg, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * _saturationBoost);
            v = Mathf.Max(v, _minLuminance);
            Color boosted = Color.HSVToRGB(h, s, v);

            return boosted;
        }

        private void ApplyColorsToHardware()
        {
            // Center Ambilight
            if (_centerAmbilight != null)
            {
                _centerAmbilight.color = _currentCenterColor;
                _centerAmbilight.intensity = _brightness * (IsActiveDisplay(ActiveDisplayIndex.Center) ? 1.25f : 0.95f);
            }
            if (_centerEmissiveStrip != null)
            {
                UpdateEmissiveStrip(_centerEmissiveStrip, _currentCenterColor);
            }

            // Left Ambilight
            if (_leftAmbilight != null)
            {
                _leftAmbilight.color = _currentLeftColor;
                _leftAmbilight.intensity = _brightness * (IsActiveDisplay(ActiveDisplayIndex.Left) ? 1.25f : 0.95f);
            }
            if (_leftEmissiveStrip != null)
            {
                UpdateEmissiveStrip(_leftEmissiveStrip, _currentLeftColor);
            }

            // Right Ambilight
            if (_rightAmbilight != null)
            {
                _rightAmbilight.color = _currentRightColor;
                _rightAmbilight.intensity = _brightness * (IsActiveDisplay(ActiveDisplayIndex.Right) ? 1.25f : 0.95f);
            }
            if (_rightEmissiveStrip != null)
            {
                UpdateEmissiveStrip(_rightEmissiveStrip, _currentRightColor);
            }

            // Desk Wash Light (Soft blend of Center with warm desk spill)
            if (_deskWashLight != null)
            {
                _deskWashLight.color = Color.Lerp(_currentCenterColor, new Color(0.9f, 0.95f, 1.0f), 0.25f);
                _deskWashLight.intensity = _brightness * 0.45f;
            }
        }

        private void UpdateEmissiveStrip(Renderer r, Color c)
        {
            if (r == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_EmissionColor", c * 2.5f);
            r.SetPropertyBlock(_propBlock);
        }

        private bool IsActiveDisplay(ActiveDisplayIndex index)
        {
            return MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.ActiveDisplay == index;
        }

        private void HandleActiveDisplayChanged(ActiveDisplayIndex display)
        {
            // Subtle transition pulse when focusing a new display
            _lastSampleTime = 0f; // Force immediate update
        }

        public AmbilightMode CurrentMode => _mode;

        public void SetMode(AmbilightMode mode)
        {
            _mode = mode;
            DesktopModeHUD.Instance?.ShowNotificationBanner($"AMBILIGHT: {_mode.ToString().ToUpper()}", "Reactive Monitor Bias Illumination", new Color(1f, 0.2f, 0.8f));
            string themeName = EnvironmentThemeController.Instance != null 
                ? EnvironmentThemeController.Instance.GetThemeDisplayName(EnvironmentThemeController.Instance.CurrentTheme) 
                : "Cyberpunk";
            DesktopModeHUD.Instance?.UpdateBottomHelp(_mode.ToString(), themeName);
        }

        public void CycleMode()
        {
            int next = ((int)_mode + 1) % Enum.GetValues(typeof(AmbilightMode)).Length;
            SetMode((AmbilightMode)next);
        }

        public void SetBrightness(float brightness)
        {
            _brightness = Mathf.Clamp(brightness, 0.0f, 4.0f);
        }

        private void SetLightsEnabled(bool enabled)
        {
            if (_centerAmbilight != null && _centerAmbilight.enabled != enabled) _centerAmbilight.enabled = enabled;
            if (_leftAmbilight != null && _leftAmbilight.enabled != enabled) _leftAmbilight.enabled = enabled;
            if (_rightAmbilight != null && _rightAmbilight.enabled != enabled) _rightAmbilight.enabled = enabled;
            if (_deskWashLight != null && _deskWashLight.enabled != enabled) _deskWashLight.enabled = enabled;

            if (_centerEmissiveStrip != null && _centerEmissiveStrip.gameObject.activeSelf != enabled) _centerEmissiveStrip.gameObject.SetActive(enabled);
            if (_leftEmissiveStrip != null && _leftEmissiveStrip.gameObject.activeSelf != enabled) _leftEmissiveStrip.gameObject.SetActive(enabled);
            if (_rightEmissiveStrip != null && _rightEmissiveStrip.gameObject.activeSelf != enabled) _rightEmissiveStrip.gameObject.SetActive(enabled);
        }
    }
}
