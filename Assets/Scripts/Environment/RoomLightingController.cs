using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.Environment
{
    public enum LightingPreset
    {
        CyberpunkNeon,
        DeepSpaceBlue,
        CleanLabWhite,
        SunsetAmber,
        DiscoParty
    }

    /// <summary>
    /// Master lighting and atmosphere controller fulfilling Section 5 & 7.
    /// Supports dynamic ambient transitions, preset toggling, and the surprise Disco Party Mode.
    /// </summary>
    public class RoomLightingController : MonoBehaviour
    {
        public static RoomLightingController Instance { get; private set; }

        [Header("Room Lights")]
        [SerializeField] private Light _directionalLight;
        [SerializeField] private Light _workstationLight;
        [SerializeField] private Light _ambientFillLight;
        [SerializeField] private List<Renderer> _cyanTrimRenderers = new List<Renderer>();
        [SerializeField] private List<Renderer> _violetTrimRenderers = new List<Renderer>();

        private LightingPreset _currentPreset = LightingPreset.CyberpunkNeon;
        private Coroutine _discoRoutine;
        private MaterialPropertyBlock _propBlock;

        public LightingPreset CurrentPreset => _currentPreset;
        public bool IsDiscoActive => _discoRoutine != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _propBlock = new MaterialPropertyBlock();

            if (_directionalLight == null)
            {
                var lights = FindObjectsByType<Light>(FindObjectsInactive.Include);
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        _directionalLight = l;
                        break;
                    }
                }
            }
            if (_workstationLight == null)
            {
                var obj = GameObject.Find("Workstation Light");
                if (obj != null) _workstationLight = obj.GetComponent<Light>();
            }
        }

        private void Start()
        {
            ApplyPreset(_currentPreset);
        }

        public void CyclePreset()
        {
            if (IsDiscoActive)
            {
                StopDiscoMode();
            }

            int next = ((int)_currentPreset + 1) % 4; // Cycles through normal presets 0-3
            ApplyPreset((LightingPreset)next);
        }

        public void ApplyPreset(LightingPreset preset)
        {
            if (IsDiscoActive && preset != LightingPreset.DiscoParty)
            {
                StopDiscoMode();
            }

            _currentPreset = preset;

            switch (preset)
            {
                case LightingPreset.CyberpunkNeon:
                    SetLightingValues(new Color(0.18f, 0.22f, 0.35f), 0.20f,
                                      new Color(0.85f, 0.92f, 1.0f), 1.2f,
                                      new Color(0.0f, 1.0f, 1.0f) * 3.5f,
                                      new Color(0.6f, 0.1f, 1.0f) * 3.5f);
                    break;

                case LightingPreset.DeepSpaceBlue:
                    SetLightingValues(new Color(0.05f, 0.10f, 0.25f), 0.15f,
                                      new Color(0.65f, 0.80f, 1.0f), 1.0f,
                                      new Color(0.1f, 0.4f, 1.0f) * 3.0f,
                                      new Color(0.2f, 0.1f, 0.7f) * 2.5f);
                    break;

                case LightingPreset.CleanLabWhite:
                    SetLightingValues(new Color(0.65f, 0.70f, 0.80f), 0.40f,
                                      new Color(0.95f, 0.98f, 1.0f), 1.5f,
                                      new Color(0.7f, 0.85f, 1.0f) * 2.0f,
                                      new Color(0.5f, 0.6f, 0.8f) * 2.0f);
                    break;

                case LightingPreset.SunsetAmber:
                    SetLightingValues(new Color(0.40f, 0.20f, 0.10f), 0.25f,
                                      new Color(1.0f, 0.75f, 0.45f), 1.2f,
                                      new Color(1.0f, 0.55f, 0.05f) * 3.5f,
                                      new Color(0.9f, 0.2f, 0.1f) * 3.0f);
                    break;

                case LightingPreset.DiscoParty:
                    StartDiscoMode();
                    break;
            }
        }

        public void ApplyZenTranquilLighting()
        {
            if (IsDiscoActive) StopDiscoMode();
            _currentPreset = LightingPreset.DeepSpaceBlue;
            SetLightingValues(new Color(0.08f, 0.22f, 0.16f), 0.18f,
                              new Color(0.68f, 0.98f, 0.82f), 0.95f,
                              new Color(0.12f, 0.88f, 0.45f) * 2.2f,
                              new Color(0.08f, 0.65f, 0.52f) * 1.8f);
        }

        public void ToggleDiscoMode()
        {
            if (IsDiscoActive)
            {
                StopDiscoMode();
                ApplyPreset(LightingPreset.CyberpunkNeon);
            }
            else
            {
                StartDiscoMode();
            }
        }

        public void StartDiscoMode()
        {
            if (_discoRoutine != null) StopCoroutine(_discoRoutine);
            _discoRoutine = StartCoroutine(DiscoRoutine());
            AchievementManager.Instance?.UnlockAchievement("Party Animal", "Activated the room's secret disco party mode!");
        }

        public void StopDiscoMode()
        {
            if (_discoRoutine != null)
            {
                StopCoroutine(_discoRoutine);
                _discoRoutine = null;
            }
        }

        private IEnumerator DiscoRoutine()
        {
            float elapsed = 0f;
            float maxDuration = 14f;

            if (AudioManager.Instance != null)
            {
                // Play connect chord as upbeat jingle
                AudioManager.Instance.PlayConnectChime();
            }

            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;

                float hue1 = (Time.time * 0.8f) % 1.0f;
                float hue2 = (hue1 + 0.5f) % 1.0f;

                Color c1 = Color.HSVToRGB(hue1, 0.9f, 1.0f) * 4.0f;
                Color c2 = Color.HSVToRGB(hue2, 0.9f, 1.0f) * 4.0f;

                UpdateEmissives(_cyanTrimRenderers, c1);
                UpdateEmissives(_violetTrimRenderers, c2);

                if (_workstationLight != null)
                {
                    _workstationLight.color = Color.HSVToRGB(hue1, 0.8f, 1.0f);
                    _workstationLight.intensity = 1.0f + 0.6f * Mathf.Sin(Time.time * 8.0f);
                }

                yield return null;
            }

            _discoRoutine = null;
            ApplyPreset(LightingPreset.CyberpunkNeon);
        }

        private void SetLightingValues(Color dirColor, float dirInt, Color workColor, float workInt, Color trim1, Color trim2)
        {
            float dirShadow = 0.75f;
            float workShadow = 0.75f;

            switch (_currentPreset)
            {
                case LightingPreset.CleanLabWhite:
                    dirShadow = 0.90f;
                    workShadow = 0.85f;
                    break;
                case LightingPreset.SunsetAmber:
                    dirShadow = 0.95f;
                    workShadow = 0.70f;
                    break;
                case LightingPreset.DeepSpaceBlue:
                    dirShadow = 0.40f;
                    workShadow = 0.60f;
                    break;
                case LightingPreset.CyberpunkNeon:
                default:
                    dirShadow = 0.65f;
                    workShadow = 0.80f;
                    break;
            }

            if (_directionalLight != null)
            {
                _directionalLight.color = dirColor;
                _directionalLight.intensity = dirInt;
                _directionalLight.shadows = LightShadows.Soft;
                _directionalLight.shadowStrength = dirShadow;
            }
            if (_workstationLight != null)
            {
                _workstationLight.color = workColor;
                _workstationLight.intensity = workInt;
                _workstationLight.shadows = LightShadows.Soft;
                _workstationLight.shadowStrength = workShadow;
            }

            UpdateEmissives(_cyanTrimRenderers, trim1);
            UpdateEmissives(_violetTrimRenderers, trim2);
        }

        private void UpdateEmissives(List<Renderer> renderers, Color emissiveColor)
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", emissiveColor * 0.2f);
                _propBlock.SetColor("_EmissionColor", emissiveColor);
                r.SetPropertyBlock(_propBlock);
            }
        }

        public void RegisterTrims(List<Renderer> cyanTrims, List<Renderer> violetTrims)
        {
            _cyanTrimRenderers = cyanTrims;
            _violetTrimRenderers = violetTrims;
            ApplyPreset(_currentPreset);
        }
    }
}
