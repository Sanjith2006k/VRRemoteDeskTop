using System;
using UnityEngine;
using UnityEngine.InputSystem;
using InputKeyboard = UnityEngine.InputSystem.Keyboard;
using InputMouse = UnityEngine.InputSystem.Mouse;
using TMPro;
using UnityEngine.UI;
using VRRemoteDesktop.Remote;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.UI.Windows;
using VRRemoteDesktop.Input;

namespace VRRemoteDesktop.UI
{
    /// <summary>
    /// Developer Debug Telemetry Panel fulfilling Section 31 & Phase 11 of Master Requirements.
    /// Toggleable via F3 or Tilde (~), displaying real-time FPS, frame timing, player positioning,
    /// stream telemetry, virtual cursor coords, active window, and interaction state.
    /// </summary>
    public class DesktopDebugPanel : MonoBehaviour
    {
        public static DesktopDebugPanel Instance { get; private set; }

        [Header("State")]
        [SerializeField] private bool _isVisible = false;

        private GameObject _panelRoot;
        private TextMeshProUGUI _debugText;
        private CanvasGroup _canvasGroup;

        // FPS calculation
        private float _deltaTime = 0.0f;
        private float _fps = 0.0f;
        private float _frameTimeMs = 0.0f;
        private float _fpsUpdateTimer = 0.0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildDebugUI();
            SetVisible(_isVisible);
        }

        private void Update()
        {
            // Toggle visibility using new Input System
            var kb = InputKeyboard.current;
            if (kb != null)
            {
                if (kb.f3Key.wasPressedThisFrame || kb.backquoteKey.wasPressedThisFrame)
                {
                    SetVisible(!_isVisible);
                }
            }

            // Calculate FPS & Frame time
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            _fpsUpdateTimer += Time.unscaledDeltaTime;

            if (_fpsUpdateTimer >= 0.2f)
            {
                _fpsUpdateTimer = 0f;
                _fps = 1.0f / _deltaTime;
                _frameTimeMs = _deltaTime * 1000.0f;

                if (_isVisible)
                {
                    RefreshTelemetry();
                }
            }
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(_isVisible);
            }
            if (_isVisible)
            {
                RefreshTelemetry();
            }
        }

        private void RefreshTelemetry()
        {
            if (_debugText == null) return;

            // Player Info
            string playerPos = "N/A";
            string playerMode = "N/A";
            var cim = ComputerInteractionManager.Instance;
            if (cim != null)
            {
                playerMode = cim.CurrentMode.ToString();
                if (cim.PlayerTransform != null)
                {
                    var p = cim.PlayerTransform.position;
                    playerPos = $"({p.x:0.1}, {p.y:0.1}, {p.z:0.1})";
                }
            }

            // Connection Info
            string connState = "Disconnected";
            string connLat = "0 ms";
            string connFps = "0";
            string connRes = "N/A";
            string connPreset = "N/A";
            string providerType = "Mock (Internal)";

            var rdm = RemoteDesktopManager.Instance;
            if (rdm != null)
            {
                connState = rdm.State.ToString();
                connLat = $"{rdm.Metrics.LatencyMs:0.0} ms";
                connFps = $"{rdm.Metrics.Fps:0.0}";
                connRes = $"{rdm.Metrics.ResolutionWidth}x{rdm.Metrics.ResolutionHeight}";
                connPreset = rdm.QualityPreset;
                if (rdm.ActiveScreenProvider != null)
                {
                    providerType = rdm.ActiveScreenProvider.GetType().Name;
                }
            }

            // Cursor Info
            string cursorCoords = "N/A";
            var raycaster = VirtualDesktopRaycaster.Instance;
            if (raycaster != null)
            {
                var uv = raycaster.CurrentUV;
                cursorCoords = $"UV: ({uv.x:0.00}, {uv.y:0.00})";
            }

            // Window Manager Info
            string activeWin = "None";
            var wm = DesktopWindowManager.Instance;
            if (wm != null && wm.FocusedWindow != null)
            {
                activeWin = wm.FocusedWindow.AppName;
            }

            var kb = InputKeyboard.current;
            var mouse = InputMouse.current;
            bool wasdActive = kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed);
            bool lmbActive = mouse != null && mouse.leftButton.isPressed;

            _debugText.text =
                $"<b><color=#00ffff>VR REMOTE DESKTOP</color> <color=#ffaa00>[DEBUG OVERLAY]</color></b> (F3 to close)\n" +
                $"------------------------------------------------------\n" +
                $"<color=#00ff88>PERFORMANCE:</color> FPS: <b>{_fps:0.0}</b>  |  Frame Time: <b>{_frameTimeMs:0.1} ms</b>\n" +
                $"<color=#00ff88>PLAYER:</color> Pos: {playerPos}  |  Mode: <b>{playerMode}</b>\n" +
                $"<color=#00ff88>CONNECTION:</color> State: <b>{connState}</b>  |  Provider: {providerType}\n" +
                $"<color=#00ff88>TELEMETRY:</color> Latency: <b>{connLat}</b>  |  Stream: <b>{connFps} FPS</b>  |  Res: <b>{connRes}</b> ({connPreset})\n" +
                $"<color=#00ff88>DESKTOP:</color> Cursor: {cursorCoords}  |  Active Window: <b>{activeWin}</b>\n" +
                $"<color=#00ff88>CONTROLS:</color> W/A/S/D: {(wasdActive ? "<color=#00ffff>ACTIVE</color>" : "IDLE")}  |  LMB: {(lmbActive ? "<color=#ff5577>DOWN</color>" : "UP")}";
        }

        private void BuildDebugUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject cObj = new GameObject("DebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = cObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
            }

            _panelRoot = new GameObject("DebugTelemetryPanel", typeof(RectTransform), typeof(Image));
            _panelRoot.transform.SetParent(canvas.transform, false);

            RectTransform prt = _panelRoot.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 1f);
            prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(0f, 1f);
            prt.anchoredPosition = new Vector2(16f, -16f);
            prt.sizeDelta = new Vector2(580f, 175f);

            Image bg = _panelRoot.GetComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.08f, 0.88f);

            // Border
            GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(_panelRoot.transform, false);
            RectTransform brt = border.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            Image bImg = border.GetComponent<Image>();
            bImg.color = new Color(0f, 1f, 1f, 0.45f);

            // Text
            GameObject textObj = new GameObject("TelemetryText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(_panelRoot.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14f, 10f);
            trt.offsetMax = new Vector2(-14f, -10f);

            _debugText = textObj.GetComponent<TextMeshProUGUI>();
            _debugText.fontSize = 13f;
            _debugText.color = Color.white;
            _debugText.lineSpacing = 16f;
            _debugText.raycastTarget = false;
        }
    }
}
