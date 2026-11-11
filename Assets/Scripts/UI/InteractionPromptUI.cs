using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRRemoteDesktop.UI
{
    /// <summary>
    /// Handles the futuristic holographic interaction prompt when approaching the workstation
    /// ("USE COMPUTER - Press [E]") and the subtle exit prompt when active ("PRESS [ESC] TO EXIT").
    /// Includes billboarding to face the camera, smooth CanvasGroup alpha fading, and subtle pulsing glow.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("Canvas Groups")]
        [Tooltip("CanvasGroup containing the 'Press E to Use Computer' prompt.")]
        [SerializeField] private CanvasGroup _enterPromptGroup;

        [Tooltip("CanvasGroup containing the 'Press ESC to Exit' prompt.")]
        [SerializeField] private CanvasGroup _exitPromptGroup;

        [Header("Text & Visual Elements")]
        [SerializeField] private TextMeshProUGUI _enterTitleText;
        [SerializeField] private TextMeshProUGUI _enterKeyText;
        [SerializeField] private Image _enterKeyBadge;
        [SerializeField] private Image _enterBorderGlow;
        [SerializeField] private TextMeshProUGUI _exitTitleText;
        [SerializeField] private TextMeshProUGUI _exitKeyText;

        [Header("Billboarding & Positioning")]
        [Tooltip("Billboard this UI to face the active player camera.")]
        [SerializeField] private bool _enableBillboarding = true;

        [Tooltip("Camera reference to billboard towards. Defaults to Camera.main.")]
        [SerializeField] private Camera _targetCamera;

        [Header("Fading & Animation")]
        [Tooltip("Speed of alpha fade in/out transitions.")]
        [SerializeField] private float _fadeSpeed = 8.0f;

        [Tooltip("Enable subtle futuristic breathing/pulse on the key badge.")]
        [SerializeField] private bool _enablePulse = true;

        private float _targetEnterAlpha = 0f;
        private float _targetExitAlpha = 0f;
        private Color _badgeInitialColor;

        private void Awake()
        {
            if (_enterPromptGroup != null) _enterPromptGroup.alpha = 0f;
            if (_exitPromptGroup != null) _exitPromptGroup.alpha = 0f;

            if (_enterTitleText != null) { _enterTitleText.fontSize = Mathf.Max(_enterTitleText.fontSize, 20f); _enterTitleText.fontStyle = FontStyles.Bold; }
            if (_enterKeyText != null) { _enterKeyText.fontSize = Mathf.Max(_enterKeyText.fontSize, 18f); _enterKeyText.fontStyle = FontStyles.Bold; }
            if (_exitTitleText != null) { _exitTitleText.fontSize = Mathf.Max(_exitTitleText.fontSize, 18f); _exitTitleText.fontStyle = FontStyles.Bold; }
            if (_exitKeyText != null) { _exitKeyText.fontSize = Mathf.Max(_exitKeyText.fontSize, 16f); _exitKeyText.fontStyle = FontStyles.Bold; }

            if (_enterKeyBadge != null)
            {
                _badgeInitialColor = _enterKeyBadge.color;
            }
        }

        private void Start()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            // Billboard to face player camera
            if (_enableBillboarding && _targetCamera != null)
            {
                Vector3 toCamera = _targetCamera.transform.position - transform.position;
                if (toCamera.sqrMagnitude > 0.001f)
                {
                    // Invert so UI canvas faces the camera directly without mirroring
                    transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
                }
            }

            // Smooth Alpha Lerp for Enter Prompt
            if (_enterPromptGroup != null)
            {
                _enterPromptGroup.alpha = Mathf.MoveTowards(_enterPromptGroup.alpha, _targetEnterAlpha, _fadeSpeed * Time.deltaTime);
                _enterPromptGroup.blocksRaycasts = _enterPromptGroup.alpha > 0.1f;
            }

            // Smooth Alpha Lerp for Exit Prompt
            if (_exitPromptGroup != null)
            {
                _exitPromptGroup.alpha = Mathf.MoveTowards(_exitPromptGroup.alpha, _targetExitAlpha, _fadeSpeed * Time.deltaTime);
                _exitPromptGroup.blocksRaycasts = _exitPromptGroup.alpha > 0.1f;
            }

            // Subtle pulsing animation on the interaction badge
            if (_enablePulse && _enterKeyBadge != null && _targetEnterAlpha > 0.1f)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 4.0f);
                _enterKeyBadge.color = new Color(_badgeInitialColor.r, _badgeInitialColor.g, _badgeInitialColor.b, _badgeInitialColor.a * pulse);
            }
        }

        /// <summary>
        /// Shows or hides the 'USE COMPUTER [E]' proximity prompt.
        /// </summary>
        public void ShowEnterPrompt(bool show)
        {
            _targetEnterAlpha = show ? 1.0f : 0.0f;
            if (!show && _enterPromptGroup != null)
            {
                _enterPromptGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Shows or hides the 'PRESS [ESC] TO EXIT' interaction mode prompt via DesktopModeHUD.
        /// </summary>
        public void ShowExitPrompt(bool show)
        {
            _targetExitAlpha = 0f; // World space prompt is never shown in front of the monitor
            if (_exitPromptGroup != null) _exitPromptGroup.alpha = 0f;

            if (DesktopModeHUD.Instance != null)
            {
                DesktopModeHUD.Instance.Show(show);
            }
        }

        /// <summary>
        /// Instantly hides all prompts without fading.
        /// </summary>
        public void HideAllImmediate()
        {
            _targetEnterAlpha = 0f;
            _targetExitAlpha = 0f;
            if (_enterPromptGroup != null) _enterPromptGroup.alpha = 0f;
            if (_exitPromptGroup != null) _exitPromptGroup.alpha = 0f;
            if (DesktopModeHUD.Instance != null) DesktopModeHUD.Instance.Show(false);
        }
    }
}
