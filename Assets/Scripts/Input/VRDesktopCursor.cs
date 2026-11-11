using UnityEngine;
using UnityEngine.UI;

namespace VRRemoteDesktop.Input
{
    /// <summary>
    /// Visual and state controller for the virtual desktop cursor on DesktopCanvas.
    /// Supports arrow pointer rendering, pixel-accurate UV mapping, and dynamic visual states
    /// for Default, Hover, Click, and Drag interactions.
    /// </summary>
    public class VRDesktopCursor : MonoBehaviour
    {
        [Header("Target References")]
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private Image _cursorImage;
        [SerializeField] private Sprite _arrowSprite;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color(0f, 1f, 1f, 1f); // Neon Cyan
        [SerializeField] private Color _hoverColor = new Color(0.1f, 1.0f, 1.0f, 1.0f); // Bright Neon Cyan
        [SerializeField] private Color _clickColor = new Color(1.0f, 0.05f, 0.55f, 1.0f); // Vivid Magenta
        [SerializeField] private Color _dragColor = new Color(0.65f, 0.30f, 1.0f, 1.0f); // Electric Violet

        [Header("Animation")]
        [SerializeField] private float _animationSpeed = 22.0f;

        private RectTransform _rectTransform;
        private Vector3 _targetScale = Vector3.one;
        private Color _targetColor = Color.white;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_cursorImage == null) _cursorImage = GetComponent<Image>();

            if (_canvasRect == null)
            {
                var canvasObj = GameObject.Find("DesktopCanvas");
                if (canvasObj != null) _canvasRect = canvasObj.GetComponent<RectTransform>();
            }

            // Setup pivot at top-left tip of arrow cursor (x: 0.08, y: 0.92)
            if (_rectTransform != null)
            {
                _rectTransform.pivot = new Vector2(0.08f, 0.92f);
                _rectTransform.sizeDelta = new Vector2(72f, 72f);
            }

            // Remove any legacy center dot child so only the sleek arrow displays
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.Contains("Dot") || child.name.Contains("Center"))
                {
                    child.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(child.gameObject);
                }
            }

            if (_arrowSprite == null && _cursorImage != null && _cursorImage.sprite == null)
            {
                _arrowSprite = Resources.Load<Sprite>("Tex_CursorArrow");
                if (_arrowSprite == null)
                {
#if UNITY_EDITOR
                    _arrowSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Tex_CursorArrow.png");
#endif
                }
            }

            if (_cursorImage != null && _arrowSprite != null)
            {
                _cursorImage.sprite = _arrowSprite;
                _cursorImage.color = _normalColor;
                _cursorImage.raycastTarget = false;
            }

            // High-contrast drop shadow for razor-sharp visibility
            Transform existingShadow = transform.Find("Shadow");
            if (existingShadow == null)
            {
                GameObject shadowObj = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
                shadowObj.transform.SetParent(transform, false);
                shadowObj.transform.SetAsFirstSibling();
                RectTransform sRt = shadowObj.GetComponent<RectTransform>();
                sRt.anchorMin = Vector2.zero;
                sRt.anchorMax = Vector2.one;
                sRt.offsetMin = new Vector2(3f, -3f);
                sRt.offsetMax = new Vector2(3f, -3f);
                sRt.pivot = new Vector2(0.08f, 0.92f);
                Image sImg = shadowObj.GetComponent<Image>();
                sImg.sprite = _arrowSprite;
                sImg.color = new Color(0f, 0f, 0f, 0.85f);
                sImg.raycastTarget = false;
            }

            _targetColor = _normalColor;
        }

        private void Update()
        {
            if (_rectTransform != null)
            {
                _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, _targetScale, Time.deltaTime * _animationSpeed);
            }

            if (_cursorImage != null)
            {
                _cursorImage.color = Color.Lerp(_cursorImage.color, _targetColor, Time.deltaTime * _animationSpeed);
            }
        }

        public void Setup(RectTransform canvasRect)
        {
            _canvasRect = canvasRect;
        }

        public void UpdateCursorPosition(Vector2 uv)
        {
            if (_canvasRect == null) return;
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();

            float width = _canvasRect.rect.width;
            float height = _canvasRect.rect.height;

            // UV coordinates (0..1, 0..1). Canvas center pivot is (0.5, 0.5)
            float localX = (uv.x - 0.5f) * width;
            float localY = (uv.y - 0.5f) * height;

            _rectTransform.anchoredPosition = new Vector2(localX, localY);
        }

        public void SetState(bool isHovering, bool isClicking)
        {
            if (isClicking)
            {
                _targetColor = _clickColor;
                _targetScale = Vector3.one * 0.88f;
            }
            else if (isHovering)
            {
                _targetColor = _hoverColor;
                _targetScale = Vector3.one * 1.15f;
            }
            else
            {
                _targetColor = _normalColor;
                _targetScale = Vector3.one;
            }
        }

        public void SetDragState(bool isDragging)
        {
            if (isDragging)
            {
                _targetColor = _dragColor;
                _targetScale = Vector3.one * 0.92f;
            }
        }

        public void Show(bool show)
        {
            if (gameObject.activeSelf != show)
            {
                gameObject.SetActive(show);
            }
        }
    }
}
