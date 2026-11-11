using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Remote;
using VRRemoteDesktop.UI;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Coordinates real-time screen sharing from the workstation's Middle Monitor
    /// directly onto the massive VIP Theater IMAX projection screen.
    /// 
    /// Supports both:
    /// - Live Host Windows PC Stream (upright 60 FPS desktop capture with live cursor)
    /// - Simulated Workstation VR OS & Applications (CenterRenderTexture)
    /// 
    /// Features:
    /// - Automatic dual-canvas reference resolution (Theater_WelcomeCanvas / TheaterWelcomeCanvas)
    /// - Dynamic texture sync between Middle Monitor and 70mm IMAX projector array
    /// - High-intensity projector beam lighting & emissive cinema canvas
    /// - Auto-projection on scene start so all stream output presents immediately in the IMAX screen
    /// - One-touch [F8] hotkey and Right Dashboard UI button integration
    /// </summary>
    public class ScreenShareToImaxManager : MonoBehaviour
    {
        public static ScreenShareToImaxManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private bool _isScreenSharing = true;

        [Header("IMAX Screen References")]
        [SerializeField] private Renderer _projectorScreenCanvas;
        [SerializeField] private GameObject _theaterWelcomeCanvas;
        [SerializeField] private RawImage _screenDisplayImage;
        [SerializeField] private GameObject _standbyPanel;
        [SerializeField] private GameObject _controlsPanel;
        [SerializeField] private Light _projectorBeamLight;

        private Material _screenMaterialInstance;
        private Texture _originalBaseMap;
        private Texture _originalEmissionMap;
        private Color _originalEmissionColor;
        private Texture _lastAppliedTexture;

        public bool IsScreenSharing => _isScreenSharing;
        public event Action<bool> OnScreenShareChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ResolveReferences();
            StartCoroutine(AutoStartScreenShareRoutine());
        }

        private IEnumerator AutoStartScreenShareRoutine()
        {
            yield return null;
            SetScreenSharing(true);
        }

        private void Update()
        {
            // Keyboard shortcut [F8] to toggle IMAX Projection
            var kb = Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame)
            {
                ToggleScreenShare();
            }

            // Keep projection texture synchronized with active center screen
            if (_isScreenSharing)
            {
                Texture activeTex = GetActiveCenterTexture();
                if (activeTex != null && activeTex != _lastAppliedTexture)
                {
                    ApplyTextureToImax(activeTex);
                }
            }
        }

        public void ResolveReferences()
        {
            if (_projectorScreenCanvas == null)
            {
                var canvasObj = GameObject.Find("ProjectorScreen_Canvas");
                if (canvasObj != null) _projectorScreenCanvas = canvasObj.GetComponent<Renderer>();
            }

            if (_theaterWelcomeCanvas == null)
            {
                _theaterWelcomeCanvas = GameObject.Find("Theater_WelcomeCanvas") ?? GameObject.Find("TheaterWelcomeCanvas");
            }

            if (_theaterWelcomeCanvas == null)
            {
                var cinemaCtrl = FindAnyObjectByType<TheaterCinemaController>();
                if (cinemaCtrl != null)
                {
                    var field = typeof(TheaterCinemaController).GetField("_theaterWelcomeCanvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) _theaterWelcomeCanvas = field.GetValue(cinemaCtrl) as GameObject;
                }
            }

            if (_theaterWelcomeCanvas != null)
            {
                if (_screenDisplayImage == null)
                {
                    var vs = _theaterWelcomeCanvas.transform.Find("VideoDisplaySurface") ?? 
                             _theaterWelcomeCanvas.transform.Find("VideoSurface");
                    if (vs != null) _screenDisplayImage = vs.GetComponent<RawImage>();
                }

                if (_standbyPanel == null)
                {
                    var sp = _theaterWelcomeCanvas.transform.Find("StandbyWelcomePanel") ?? 
                             _theaterWelcomeCanvas.transform.Find("StandbyPanel");
                    if (sp != null) _standbyPanel = sp.gameObject;
                }

                if (_controlsPanel == null)
                {
                    var cp = _theaterWelcomeCanvas.transform.Find("CinemaControlsPanel") ?? 
                             _theaterWelcomeCanvas.transform.Find("ControlsPanel");
                    if (cp != null) _controlsPanel = cp.gameObject;
                }
            }

            if (_projectorBeamLight == null)
            {
                var pbl = GameObject.Find("ProjectorBeamLight");
                if (pbl != null) _projectorBeamLight = pbl.GetComponent<Light>();
            }

            if (_projectorScreenCanvas != null && _screenMaterialInstance == null)
            {
                _screenMaterialInstance = _projectorScreenCanvas.material;
                if (_screenMaterialInstance.HasProperty("_BaseMap"))
                    _originalBaseMap = _screenMaterialInstance.GetTexture("_BaseMap");
                if (_screenMaterialInstance.HasProperty("_EmissionMap"))
                    _originalEmissionMap = _screenMaterialInstance.GetTexture("_EmissionMap");
                if (_screenMaterialInstance.HasProperty("_EmissionColor"))
                    _originalEmissionColor = _screenMaterialInstance.GetColor("_EmissionColor");
            }
        }

        public Texture GetActiveCenterTexture()
        {
            // 1. If CenterRenderTexture exists, it contains the full desktop output (including cursor and host stream!)
            if (MultiMonitorManager.Instance != null && MultiMonitorManager.Instance.CenterRenderTexture != null)
            {
                return MultiMonitorManager.Instance.CenterRenderTexture;
            }

            // 2. Fallback to Screen's current texture
            var centerScreen = GameObject.Find("Screen")?.GetComponent<RemoteDesktopScreen>();
            if (centerScreen != null && centerScreen.CurrentTexture != null)
            {
                return centerScreen.CurrentTexture;
            }

            // 3. Fallback to Host Screen Capture texture if host streaming is active
            if (HostDesktopStreamManager.Instance != null && HostDesktopStreamManager.Instance.IsHostStreamActive)
            {
                if (HostDesktopStreamManager.Instance.ScreenCapture != null && HostDesktopStreamManager.Instance.ScreenCapture.HostTexture != null)
                {
                    return HostDesktopStreamManager.Instance.ScreenCapture.HostTexture;
                }
            }

            // 4. Fallback to UICamera target
            var cam = GameObject.Find("UICamera")?.GetComponent<Camera>();
            if (cam != null && cam.targetTexture != null)
            {
                return cam.targetTexture;
            }

            return null;
        }

        /// <summary>
        /// Toggles mirroring of the middle monitor screen onto the IMAX theater screen.
        /// </summary>
        public bool ToggleScreenShare()
        {
            SetScreenSharing(!_isScreenSharing);
            return _isScreenSharing;
        }

        public void SetScreenSharing(bool enable)
        {
            ResolveReferences();

            _isScreenSharing = enable;

            if (_isScreenSharing)
            {
                StartScreenShare();
                DesktopModeHUD.Instance?.ShowNotificationBanner(
                    "IMAX PROJECTION ACTIVE",
                    "Workstation Middle Monitor Desktop mirrored to 70mm Theater Screen [F8 to Exit]",
                    new Color(0f, 1f, 1f, 1f));
            }
            else
            {
                StopScreenShare();
                DesktopModeHUD.Instance?.ShowNotificationBanner(
                    "IMAX PROJECTION STANDBY",
                    "Theater Projector Array returned to standby mode [Press F8 to Project]",
                    new Color(0.6f, 0.75f, 0.9f, 1f));
            }

            OnScreenShareChanged?.Invoke(_isScreenSharing);
            HostDesktopStreamManager.Instance?.UpdateHUDHelp();
        }

        public void UpdateSharedTexture()
        {
            if (!_isScreenSharing) return;
            Texture centerTex = GetActiveCenterTexture();
            if (centerTex != null)
            {
                ApplyTextureToImax(centerTex);
            }
        }

        private void StartScreenShare()
        {
            Texture centerTex = GetActiveCenterTexture();
            if (centerTex == null)
            {
                Debug.LogWarning("[ScreenShareToImaxManager] Could not find Middle Screen texture to share!");
                return;
            }

            ApplyTextureToImax(centerTex);
            Debug.Log($"[ScreenShareToImaxManager] Middle Monitor successfully mirrored to VIP Theater IMAX Screen! Texture: {centerTex.name}");
        }

        private void ApplyTextureToImax(Texture tex)
        {
            if (tex == null) return;
            _lastAppliedTexture = tex;

            // 1. Apply to 3D Quad Projector Canvas
            if (_projectorScreenCanvas != null)
            {
                if (_screenMaterialInstance == null) _screenMaterialInstance = _projectorScreenCanvas.material;

                _screenMaterialInstance.SetTexture("_BaseMap", tex);
                _screenMaterialInstance.SetTexture("_EmissionMap", tex);
                _screenMaterialInstance.SetColor("_EmissionColor", Color.white * 1.45f);
                _screenMaterialInstance.EnableKeyword("_EMISSION");
            }

            // 2. Apply to TheaterWelcomeCanvas UI (Right-side up VideoDisplaySurface)
            if (_theaterWelcomeCanvas != null)
            {
                _theaterWelcomeCanvas.SetActive(true);

                if (_screenDisplayImage != null)
                {
                    _screenDisplayImage.gameObject.SetActive(true);
                    _screenDisplayImage.texture = tex;
                    _screenDisplayImage.color = Color.white;
                }

                if (_standbyPanel != null)
                {
                    _standbyPanel.SetActive(false);
                }

                if (_controlsPanel != null)
                {
                    _controlsPanel.SetActive(false);
                }
            }

            // 3. Intensify Ceiling Projector Beam
            if (_projectorBeamLight != null)
            {
                _projectorBeamLight.intensity = 25f;
            }
        }

        private void StopScreenShare()
        {
            _lastAppliedTexture = null;

            // 1. Revert 3D Quad Projector Canvas
            if (_projectorScreenCanvas != null && _screenMaterialInstance != null)
            {
                _screenMaterialInstance.SetTexture("_BaseMap", _originalBaseMap);
                _screenMaterialInstance.SetTexture("_EmissionMap", _originalEmissionMap);
                _screenMaterialInstance.SetColor("_EmissionColor", _originalEmissionColor);
            }

            // 2. Revert TheaterWelcomeCanvas UI
            if (_theaterWelcomeCanvas != null)
            {
                bool inCinema = TheaterCinemaController.Instance != null && TheaterCinemaController.Instance.IsInCinemaMode;

                if (!inCinema)
                {
                    _theaterWelcomeCanvas.SetActive(false);
                }
                else
                {
                    if (_standbyPanel != null) _standbyPanel.SetActive(true);
                    if (_controlsPanel != null) _controlsPanel.SetActive(true);
                    if (_screenDisplayImage != null) _screenDisplayImage.gameObject.SetActive(false);
                }
            }

            // 3. Dim Ceiling Projector Beam if not in active movie playback
            if (_projectorBeamLight != null)
            {
                bool inCinema = TheaterCinemaController.Instance != null && TheaterCinemaController.Instance.IsInCinemaMode;
                _projectorBeamLight.intensity = inCinema ? 16f : 0f;
            }

            Debug.Log("[ScreenShareToImaxManager] IMAX Screen Sharing stopped. Reverted to standard mode.");
        }
    }
}
