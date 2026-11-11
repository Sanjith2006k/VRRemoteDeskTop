using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace VRRemoteDesktop.UI.Keyboard
{
    public enum KeyActionType
    {
        Character,
        Backspace,
        Enter,
        Space,
        Shift,
        CapsLock,
        Tab,
        Clear,
        Hide
    }

    /// <summary>
    /// Interactive button representing a single key on the VirtualKeyboard.
    /// Supports dynamic character display, shift states, hover animations, and sound/visual ripple.
    /// </summary>
    public class VirtualKey : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Key Configuration")]
        [SerializeField] private KeyActionType _actionType = KeyActionType.Character;
        [SerializeField] private string _primaryChar = "";
        [SerializeField] private string _shiftChar = "";

        [Header("UI Visuals")]
        [SerializeField] private Image _keyBackground;
        [SerializeField] private Image _keyBorder;
        [SerializeField] private TextMeshProUGUI _label;

        [Header("Colors & Styling")]
        [SerializeField] private Color _normalBg = new Color(0.08f, 0.12f, 0.20f, 0.95f);
        [SerializeField] private Color _hoverBg = new Color(0.12f, 0.20f, 0.35f, 1.0f);
        [SerializeField] private Color _pressedBg = new Color(0.0f, 0.8f, 1.0f, 0.8f);
        [SerializeField] private Color _borderColor = new Color(0.0f, 0.6f, 0.9f, 0.4f);

        private Vector3 _originalScale = Vector3.one;
        private bool _isHovered = false;

        public KeyActionType ActionType => _actionType;
        public string PrimaryChar => _primaryChar;
        public string ShiftChar => _shiftChar;

        public event Action<VirtualKey> OnKeyPressed;

        private void Awake()
        {
            _originalScale = transform.localScale;
            if (_keyBackground == null) _keyBackground = GetComponent<Image>();
            if (_label == null) _label = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void Setup(KeyActionType actionType, string primary, string shift, Vector2 size)
        {
            _actionType = actionType;
            _primaryChar = primary;
            _shiftChar = string.IsNullOrEmpty(shift) ? primary : shift;

            var rt = GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = size;

            UpdateDisplay(false, false);
        }

        public void UpdateDisplay(bool isShiftActive, bool isCapsLockActive)
        {
            if (_label == null) _label = GetComponentInChildren<TextMeshProUGUI>();
            if (_label == null) return;

            switch (_actionType)
            {
                case KeyActionType.Character:
                    bool upper = isShiftActive ^ isCapsLockActive;
                    _label.text = upper ? _shiftChar : _primaryChar;
                    break;
                case KeyActionType.Space:
                    _label.text = "SPACE";
                    break;
                case KeyActionType.Backspace:
                    _label.text = "BKSP";
                    break;
                case KeyActionType.Enter:
                    _label.text = "ENTER";
                    break;
                case KeyActionType.Shift:
                    _label.text = isShiftActive ? "[SHIFT]" : "SHIFT";
                    break;
                case KeyActionType.CapsLock:
                    _label.text = isCapsLockActive ? "[CAPS]" : "CAPS";
                    break;
                case KeyActionType.Tab:
                    _label.text = "TAB";
                    break;
                case KeyActionType.Clear:
                    _label.text = "CLEAR";
                    break;
                case KeyActionType.Hide:
                    _label.text = "HIDE";
                    break;
            }
        }

        public void SetHighlight(bool highlighted)
        {
            if (_keyBackground != null)
            {
                _keyBackground.color = highlighted ? _pressedBg : _normalBg;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            transform.localScale = _originalScale * 1.05f;
            if (_keyBackground != null) _keyBackground.color = _hoverBg;
            if (_keyBorder != null) _keyBorder.color = Color.cyan;

            if (VRRemoteDesktop.Audio.AudioManager.Instance != null)
            {
                VRRemoteDesktop.Audio.AudioManager.Instance.PlayHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            transform.localScale = _originalScale;
            if (_keyBackground != null) _keyBackground.color = _normalBg;
            if (_keyBorder != null) _keyBorder.color = _borderColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = _originalScale * 0.94f;
            if (_keyBackground != null) _keyBackground.color = _pressedBg;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = _isHovered ? _originalScale * 1.05f : _originalScale;
            if (_keyBackground != null) _keyBackground.color = _isHovered ? _hoverBg : _normalBg;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (VRRemoteDesktop.Audio.AudioManager.Instance != null)
            {
                VRRemoteDesktop.Audio.AudioManager.Instance.PlayKeyboardTap();
            }
            OnKeyPressed?.Invoke(this);
        }
    }
}
