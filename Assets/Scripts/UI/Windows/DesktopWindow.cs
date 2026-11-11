using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace VRRemoteDesktop.UI.Windows
{
    /// <summary>
    /// Reusable desktop window controller supporting draggable title bar,
    /// minimize, maximize/restore, close, focus depth ordering, and smooth scale/fade animations.
    /// </summary>
    public class DesktopWindow : MonoBehaviour, IPointerDownHandler
    {
        [Header("Window Identity")]
        [SerializeField] private string _appName = "App";
        [SerializeField] private string _windowTitle = "Application";

        [Header("UI Component References")]
        [SerializeField] private RectTransform _windowRect;
        [SerializeField] private RectTransform _titleBarRect;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _minimizeButton;
        [SerializeField] private Button _maximizeButton;
        [SerializeField] private RectTransform _contentArea;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Window Dimensions")]
        [SerializeField] private Vector2 _defaultSize = new Vector2(760, 480);
        [SerializeField] private Vector2 _defaultPosition = Vector2.zero;

        [Header("Animation")]
        [SerializeField] private float _animationSpeed = 14.0f;

        private bool _isOpen = false;
        private bool _isMaximized = false;
        private bool _isMinimized = false;
        private Vector2 _savedPosition;
        private Vector2 _savedSize;

        private Vector3 _targetScale = Vector3.one;
        private float _targetAlpha = 0f;

        public string AppName => _appName;
        public bool IsOpen => _isOpen;
        public bool IsMaximized => _isMaximized;
        public bool IsMinimized => _isMinimized;
        public RectTransform WindowRect => _windowRect;
        public RectTransform ContentArea => _contentArea;

        public event Action<DesktopWindow> OnWindowFocused;
        public event Action<DesktopWindow> OnWindowClosed;

        private void Awake()
        {
            if (_windowRect == null) _windowRect = GetComponent<RectTransform>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            _savedPosition = _defaultPosition;
            _savedSize = _defaultSize;

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
            }

            if (_minimizeButton != null)
            {
                _minimizeButton.onClick.AddListener(Minimize);
            }

            if (_maximizeButton != null)
            {
                _maximizeButton.onClick.AddListener(ToggleMaximize);
            }

            // Setup draggable title bar if assigned
            if (_titleBarRect != null)
            {
                var dragHandler = _titleBarRect.gameObject.GetComponent<WindowTitleBarDragHandler>();
                if (dragHandler == null)
                {
                    dragHandler = _titleBarRect.gameObject.AddComponent<WindowTitleBarDragHandler>();
                }
                dragHandler.Setup(this);
            }
        }

        private void Update()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, Time.deltaTime * _animationSpeed);
                _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0.8f;
                _canvasGroup.interactable = _canvasGroup.alpha > 0.8f;

                if (_targetAlpha == 0f && _canvasGroup.alpha <= 0.01f && gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
            }

            if (_windowRect != null)
            {
                _windowRect.localScale = Vector3.Lerp(_windowRect.localScale, _targetScale, Time.deltaTime * _animationSpeed);
            }
        }

        public void Setup(string appName, string title, Vector2 size, Vector2 position)
        {
            _appName = appName;
            _windowTitle = title;
            _defaultSize = size;
            _defaultPosition = position;
            _savedSize = size;
            _savedPosition = position;

            if (_titleText != null) _titleText.text = title;
            if (_windowRect != null)
            {
                _windowRect.sizeDelta = size;
                _windowRect.anchoredPosition = position;
            }
        }

        public void SetupButtons(Button closeBtn, Button minBtn, Button maxBtn)
        {
            _closeButton = closeBtn;
            _minimizeButton = minBtn;
            _maximizeButton = maxBtn;

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Close);
            }

            if (_minimizeButton != null)
            {
                _minimizeButton.onClick.RemoveAllListeners();
                _minimizeButton.onClick.AddListener(Minimize);
            }

            if (_maximizeButton != null)
            {
                _maximizeButton.onClick.RemoveAllListeners();
                _maximizeButton.onClick.AddListener(ToggleMaximize);
            }
        }

        public void Open()
        {
            gameObject.SetActive(true);
            _isOpen = true;
            _isMinimized = false;

            if (_windowRect != null)
            {
                _windowRect.localScale = Vector3.one;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1.0f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            _targetScale = Vector3.one;
            _targetAlpha = 1.0f;

            Focus();
        }

        public void Close()
        {
            _isOpen = false;
            _targetAlpha = 0f;
            _targetScale = Vector3.one * 0.92f;

            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            else
            {
                gameObject.SetActive(false);
            }

            OnWindowClosed?.Invoke(this);
        }

        public void Minimize()
        {
            _isMinimized = true;
            _targetAlpha = 0f;
            _targetScale = Vector3.one * 0.6f;

            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }

        public void Restore()
        {
            gameObject.SetActive(true);
            _isMinimized = false;
            _targetAlpha = 1.0f;
            _targetScale = Vector3.one;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1.0f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
            Focus();
        }

        public void ToggleMaximize()
        {
            if (_windowRect == null) return;

            RectTransform parentRect = _windowRect.parent as RectTransform;
            float parentW = parentRect != null && parentRect.rect.width > 0 ? parentRect.rect.width : 1920f;
            float parentH = parentRect != null && parentRect.rect.height > 0 ? parentRect.rect.height : 1080f;

            _isMaximized = !_isMaximized;

            if (_isMaximized)
            {
                _savedPosition = _windowRect.anchoredPosition;
                _savedSize = _windowRect.sizeDelta;

                // Maximize filling parent with margins for top bar (40px) and dock (60px)
                _windowRect.anchoredPosition = new Vector2(0f, -5f);
                _windowRect.sizeDelta = new Vector2(parentW - 20f, parentH - 95f);
            }
            else
            {
                _windowRect.anchoredPosition = _savedPosition;
                _windowRect.sizeDelta = _savedSize;
            }

            Focus();
        }

        public void Drag(Vector2 delta)
        {
            if (_isMaximized)
            {
                // De-maximize when dragged
                ToggleMaximize();
            }

            if (_windowRect == null) return;

            _windowRect.anchoredPosition += delta;
            ClampToScreen();
        }

        private void ClampToScreen()
        {
            if (_windowRect == null) return;
            RectTransform parentRect = _windowRect.parent as RectTransform;
            if (parentRect == null) return;

            float halfParentW = parentRect.rect.width * 0.5f;
            float halfParentH = parentRect.rect.height * 0.5f;

            float minX = -halfParentW + 80f;
            float maxX = halfParentW - 80f;
            float minY = -halfParentH + 80f;
            float maxY = halfParentH - 45f;

            Vector2 pos = _windowRect.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            _windowRect.anchoredPosition = pos;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Focus();
        }

        public void Focus()
        {
            transform.SetAsLastSibling();
            OnWindowFocused?.Invoke(this);
        }
    }

    /// <summary>
    /// Helper drag proxy attached to the window title bar.
    /// </summary>
    public class WindowTitleBarDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        private DesktopWindow _window;

        public void Setup(DesktopWindow window)
        {
            _window = window;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_window != null) _window.Focus();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_window != null) _window.Focus();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_window != null)
            {
                _window.Drag(eventData.delta);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }
    }
}
