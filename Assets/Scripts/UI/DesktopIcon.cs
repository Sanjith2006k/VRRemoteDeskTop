using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace VRRemoteDesktop.UI
{
    /// <summary>
    /// Interactive desktop application shortcut or dock item with hover and click animations.
    /// </summary>
    public class DesktopIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Application Info")]
        [SerializeField] private string _appName = "App";
        [SerializeField] private string _appDescription = "";

        [Header("UI Visual References")]
        [SerializeField] private Image _iconBackground;
        [SerializeField] private Image _iconBorder;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _subText;
        [SerializeField] private GameObject _activeIndicator;

        [Header("Hover Animation")]
        [SerializeField] private Vector3 _hoverScale = new Vector3(1.08f, 1.08f, 1f);
        [SerializeField] private Color _highlightColor = new Color(0f, 1f, 1f, 1f);

        private Vector3 _originalScale;
        private Color _originalBorderColor;
        private bool _isHovered;

        public string AppName => _appName;

        private void Awake()
        {
            _originalScale = transform.localScale;
            if (_iconBorder != null)
            {
                _originalBorderColor = _iconBorder.color;
            }

            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(HandleButtonClick);
                btn.onClick.AddListener(HandleButtonClick);
            }
        }

        private void HandleButtonClick()
        {
            OnPointerClick(null);
        }

        public void Setup(string appName, string description, Color accentColor)
        {
            _appName = appName;
            _appDescription = description;
            _highlightColor = accentColor;

            if (_titleText != null)
            {
                _titleText.text = appName;
                _titleText.fontSize = Mathf.Max(_titleText.fontSize, 20f);
                _titleText.fontStyle = FontStyles.Bold;
            }
            if (_subText != null)
            {
                _subText.text = description;
                _subText.fontSize = Mathf.Max(_subText.fontSize, 16f);
            }
            if (_iconBorder != null) _iconBorder.color = accentColor;
        }

        public void SetRunning(bool isRunning)
        {
            if (_activeIndicator != null)
            {
                _activeIndicator.SetActive(isRunning);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            transform.localScale = Vector3.Scale(_originalScale, _hoverScale);
            if (_iconBorder != null)
            {
                _iconBorder.color = _highlightColor * 1.3f;
            }
            if (VRRemoteDesktop.Audio.AudioManager.Instance != null)
            {
                VRRemoteDesktop.Audio.AudioManager.Instance.PlayHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            transform.localScale = _originalScale;
            if (_iconBorder != null)
            {
                _iconBorder.color = _highlightColor;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = Vector3.Scale(_originalScale, new Vector3(0.95f, 0.95f, 1f));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = _isHovered ? Vector3.Scale(_originalScale, _hoverScale) : _originalScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[DesktopIcon] Clicked application: {_appName}");
            if (VRRemoteDesktop.Audio.AudioManager.Instance != null)
            {
                VRRemoteDesktop.Audio.AudioManager.Instance.PlayClick();
            }

            string app = _appName;
            if (string.IsNullOrEmpty(app) || app == "App")
            {
                app = gameObject.name.Replace("Icon_", "").Replace("Dock_", "").Trim();
            }

            if (DesktopUIManager.Instance != null)
            {
                DesktopUIManager.Instance.LaunchApp(app);
                return;
            }

            var winMgr = Windows.DesktopWindowManager.Instance ?? FindAnyObjectByType<Windows.DesktopWindowManager>();
            if (winMgr != null)
            {
                winMgr.OpenWindow(app);
            }
            else
            {
                var uiMgr = FindAnyObjectByType<DesktopUIManager>();
                if (uiMgr != null) uiMgr.LaunchApp(app);
            }
        }
    }
}
