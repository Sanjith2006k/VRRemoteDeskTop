using System.Collections;
using UnityEngine;
using TMPro;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Interactive Central Holographic Projection fulfilling Section 7 (Prop D).
    /// Provides smooth orbital rotations, vertical floating bob, dynamic mode switching
    /// (Planetary Solar Core, Cyber Tesseract, Spatial Satellite Grid), and interactive color shift.
    /// </summary>
    public class HologramController : InteractiveProp
    {
        public static HologramController Instance { get; private set; }

        [Header("Hologram Elements")]
        [SerializeField] private Transform _corePivot;
        [SerializeField] private Transform _orbitRing1;
        [SerializeField] private Transform _orbitRing2;
        [SerializeField] private Transform _satellitePivot;
        [SerializeField] private Renderer _coreRenderer;
        [SerializeField] private TextMeshProUGUI _modeLabelText;

        public enum HologramMode
        {
            OrbitalCore,
            CyberTesseract,
            SpatialSatellite
        }

        private HologramMode _currentMode = HologramMode.OrbitalCore;
        private float _baseY = 0f;
        private MaterialPropertyBlock _propBlock;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _interactionRange = 2.8f;
            _promptOffset = new Vector3(0f, 0.40f, 0f);
            SetPromptAction("CYCLE HOLOGRAM");
            _baseY = _corePivot != null ? _corePivot.localPosition.y : 1.2f;
            _propBlock = new MaterialPropertyBlock();
            UpdateModeVisuals();
        }

        protected override void Update()
        {
            base.Update();

            // Continuous futuristic orbits and rotations
            if (_corePivot != null)
            {
                _corePivot.Rotate(Vector3.up, 30f * Time.deltaTime, Space.Self);
                float bob = Mathf.Sin(Time.time * 1.8f) * 0.05f;
                Vector3 p = _corePivot.localPosition;
                p.y = _baseY + bob;
                _corePivot.localPosition = p;
            }

            if (_orbitRing1 != null)
            {
                _orbitRing1.Rotate(Vector3.forward, 45f * Time.deltaTime, Space.Self);
                _orbitRing1.Rotate(Vector3.up, 15f * Time.deltaTime, Space.Self);
            }

            if (_orbitRing2 != null)
            {
                _orbitRing2.Rotate(Vector3.right, -35f * Time.deltaTime, Space.Self);
            }

            if (_satellitePivot != null)
            {
                _satellitePivot.Rotate(Vector3.up, -60f * Time.deltaTime, Space.Self);
            }
        }

        protected override void OnInteract()
        {
            CycleMode();
        }

        public void CycleMode()
        {
            _currentMode = (HologramMode)(((int)_currentMode + 1) % 3);
            UpdateModeVisuals();

            AudioManager.Instance?.PlaySweepPrompt();
            AchievementManager.Instance?.UnlockAchievement("Hologram Master", "Explored spatial holographic visualization modes.");
        }

        public void SetMode(HologramMode mode)
        {
            _currentMode = mode;
            UpdateModeVisuals();
        }

        private void UpdateModeVisuals()
        {
            Color c = Color.cyan;
            string label = "HOLOGRAM // ORBITAL CORE";

            switch (_currentMode)
            {
                case HologramMode.OrbitalCore:
                    c = new Color(0f, 1f, 1f);
                    label = "HOLOGRAM // ORBITAL CORE [CYAN]";
                    break;

                case HologramMode.CyberTesseract:
                    c = new Color(1f, 0.1f, 0.7f);
                    label = "HOLOGRAM // CYBER TESSERACT [MAGENTA]";
                    break;

                case HologramMode.SpatialSatellite:
                    c = new Color(0.1f, 1f, 0.5f);
                    label = "HOLOGRAM // SPATIAL SATELLITE [EMERALD]";
                    break;
            }

            if (_coreRenderer != null)
            {
                _coreRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", c);
                _coreRenderer.SetPropertyBlock(_propBlock);
            }

            if (_modeLabelText != null)
            {
                _modeLabelText.text = label;
                _modeLabelText.color = c;
            }
        }
    }
}
