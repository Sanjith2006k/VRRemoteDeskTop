using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.Environment;

namespace VRRemoteDesktop.UI.Windows
{
    /// <summary>
    /// Live CCTV Surveillance & Desktop Webcam feed application.
    /// Provides live camera switching between Room CCTV (overhead room coverage),
    /// Desktop Webcam (monitor-mounted facing player), and 3rd-Person Follow.
    /// Displays real-time avatar motion, HUD overlays, timestamps, and recording status.
    /// </summary>
    public class LiveCamApp : MonoBehaviour
    {
        [Header("UI Display Components")]
        [SerializeField] private RawImage _cameraFeed;
        [SerializeField] private TextMeshProUGUI _recBadge;
        [SerializeField] private TextMeshProUGUI _camLabel;
        [SerializeField] private TextMeshProUGUI _clockFps;
        [SerializeField] private TextMeshProUGUI _targetStatus;

        [Header("Angle Controls")]
        [SerializeField] private Button _btnCctv;
        [SerializeField] private Button _btnWebcam;
        [SerializeField] private Button _btnThirdPerson;

        private SurveillanceCameraMode _currentMode = SurveillanceCameraMode.RoomCCTV;
        private float _fpsUpdateTimer = 0f;
        private int _fpsCount = 60;

        public void Setup(
            RawImage feed,
            TextMeshProUGUI recBadge,
            TextMeshProUGUI camLabel,
            TextMeshProUGUI clockFps,
            TextMeshProUGUI targetStatus,
            Button btnCctv,
            Button btnWebcam,
            Button btnThirdPerson)
        {
            _cameraFeed = feed;
            _recBadge = recBadge;
            _camLabel = camLabel;
            _clockFps = clockFps;
            _targetStatus = targetStatus;
            _btnCctv = btnCctv;
            _btnWebcam = btnWebcam;
            _btnThirdPerson = btnThirdPerson;

            if (_btnCctv != null)
                _btnCctv.onClick.AddListener(() => SwitchMode(SurveillanceCameraMode.RoomCCTV));
            if (_btnWebcam != null)
                _btnWebcam.onClick.AddListener(() => SwitchMode(SurveillanceCameraMode.DesktopWebcam));
            if (_btnThirdPerson != null)
                _btnThirdPerson.onClick.AddListener(() => SwitchMode(SurveillanceCameraMode.ThirdPersonFollow));

            UpdateModeUI();
        }

        private void OnEnable()
        {
            UpdateFeedTexture();
        }

        private void Update()
        {
            UpdateFeedTexture();
            UpdateBlinkingRec();
            UpdateClockAndFps();
        }

        private void UpdateFeedTexture()
        {
            if (_cameraFeed != null && RoomSurveillanceCamera.Instance != null)
            {
                var rt = RoomSurveillanceCamera.Instance.FeedTexture;
                if (_cameraFeed.texture != rt && rt != null)
                {
                    _cameraFeed.texture = rt;
                }
            }
        }

        private void UpdateBlinkingRec()
        {
            if (_recBadge == null) return;
            bool recBlink = (Mathf.Sin(Time.unscaledTime * 5.0f) > 0f);
            _recBadge.text = recBlink 
                ? "<color=#00ff88><b>| REC [LIVE]</b></color>" 
                : "<color=#004422><b>O REC [LIVE]</b></color>";
        }

        private void UpdateClockAndFps()
        {
            _fpsUpdateTimer += Time.unscaledDeltaTime;
            if (_fpsUpdateTimer >= 0.5f)
            {
                _fpsCount = Mathf.RoundToInt(1.0f / Mathf.Max(0.0001f, Time.unscaledDeltaTime));
                _fpsUpdateTimer = 0f;
            }

            if (_clockFps != null)
            {
                string timeStr = DateTime.Now.ToString("yyyy-MM-dd [HH:mm:ss]");
                _clockFps.text = $"{timeStr} // <color=#00e5ff>{_fpsCount} FPS</color>";
            }
        }

        public void SwitchMode(SurveillanceCameraMode mode)
        {
            _currentMode = mode;
            if (RoomSurveillanceCamera.Instance != null)
            {
                RoomSurveillanceCamera.Instance.SetCameraMode(mode);
            }
            UpdateModeUI();
        }

        private void UpdateModeUI()
        {
            if (_camLabel != null)
            {
                switch (_currentMode)
                {
                    case SurveillanceCameraMode.RoomCCTV:
                        _camLabel.text = "CAM 01 // OVERHEAD CCTV - FULL ROOM COVERAGE";
                        break;
                    case SurveillanceCameraMode.DesktopWebcam:
                        _camLabel.text = "CAM 02 // DESKTOP WEBCAM - COCKPIT OPERATOR";
                        break;
                    case SurveillanceCameraMode.ThirdPersonFollow:
                        _camLabel.text = "CAM 03 // 3RD-PERSON CHASE - ACTIVE DYNAMIC TRACKING";
                        break;
                }
            }

            if (_targetStatus != null)
            {
                _targetStatus.text = "TARGET: <color=#00ffff>PLAYER AVATAR // CYBER SUIT</color> | STATUS: <color=#00ff88>TRACKING LOCK</color>";
            }

            UpdateButtonVisual(_btnCctv, _currentMode == SurveillanceCameraMode.RoomCCTV);
            UpdateButtonVisual(_btnWebcam, _currentMode == SurveillanceCameraMode.DesktopWebcam);
            UpdateButtonVisual(_btnThirdPerson, _currentMode == SurveillanceCameraMode.ThirdPersonFollow);
        }

        private void UpdateButtonVisual(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = active 
                    ? new Color(0f, 0.85f, 1f, 0.55f) 
                    : new Color(0.12f, 0.18f, 0.28f, 0.45f);
            }
        }
    }
}
