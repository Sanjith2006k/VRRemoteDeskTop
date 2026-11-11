using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using VRRemoteDesktop.Remote;
using VRRemoteDesktop.UI.Windows;

namespace VRRemoteDesktop.UI.Keyboard
{
    /// <summary>
    /// Master Virtual Keyboard component attached to DesktopCanvas.
    /// Features a complete cyberpunk styled QWERTY layout, dynamic character shifting,
    /// seamless routing to active TMP_InputFields, and bidirectional integration
    /// with the RemoteInput abstraction and physical PC keyboard.
    /// </summary>
    public class VirtualKeyboard : MonoBehaviour, IPointerDownHandler
    {
        public static VirtualKeyboard Instance { get; private set; }

        [Header("Target & Container")]
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private TMP_InputField _activeInputField;

        [Header("State")]
        [SerializeField] private bool _isOpen = false;
        [SerializeField] private bool _isShiftActive = false;
        [SerializeField] private bool _isCapsLockActive = false;

        [Header("Visual References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _keyboardRect;
        [SerializeField] private TextMeshProUGUI _targetFieldIndicator;

        private List<VirtualKey> _keys = new List<VirtualKey>();
        private TMP_FontAsset _defaultFont;
        private Vector3 _targetScale = Vector3.one;
        private float _targetAlpha = 0f;

        public bool IsOpen => _isOpen;
        public TMP_InputField ActiveInputField => _activeInputField;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _canvasGroup = GetComponent<CanvasGroup>();
            _keyboardRect = GetComponent<RectTransform>();

            ResolveCanvas();
            LoadDefaultFont();
            BuildKeyboardLayout();

            // Start closed
            CloseImmediate();
        }

        private void Start()
        {
            // Auto-detect newly focused input fields
            HookExistingInputFields();
        }

        private void Update()
        {
            // Smooth fade & scale
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, Time.deltaTime * 14f);
                _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0.8f;
                _canvasGroup.interactable = _canvasGroup.alpha > 0.8f;

                if (_targetAlpha == 0f && _canvasGroup.alpha <= 0.01f && gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
            }

            if (_keyboardRect != null)
            {
                _keyboardRect.localScale = Vector3.Lerp(_keyboardRect.localScale, _targetScale, Time.deltaTime * 14f);
            }

            // Route physical keyboard keys when keyboard is open or target is focused
            ProcessPhysicalKeyboardInput();
        }

        private void ResolveCanvas()
        {
            if (_canvasRect == null)
            {
                var canvasObj = GameObject.Find("DesktopCanvas");
                if (canvasObj != null) _canvasRect = canvasObj.GetComponent<RectTransform>();
            }
        }

        private void LoadDefaultFont()
        {
#if UNITY_EDITOR
            _defaultFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
#endif
            if (_defaultFont == null)
            {
                _defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
            if (_defaultFont == null)
            {
                var anyTmp = FindAnyObjectByType<TextMeshProUGUI>();
                if (anyTmp != null) _defaultFont = anyTmp.font;
            }
        }

        public void HookExistingInputFields()
        {
            var fields = Object.FindObjectsByType<TMP_InputField>();
            foreach (var f in fields)
            {
                f.onSelect.RemoveListener(HandleFieldSelected);
                f.onSelect.AddListener(HandleFieldSelected);
            }
        }

        private void HandleFieldSelected(string value)
        {
            var es = EventSystem.current;
            if (es != null && es.currentSelectedGameObject != null)
            {
                var input = es.currentSelectedGameObject.GetComponent<TMP_InputField>();
                if (input != null)
                {
                    SetTargetInputField(input);
                }
            }
        }

        public void SetTargetInputField(TMP_InputField field)
        {
            _activeInputField = field;
            if (_targetFieldIndicator != null)
            {
                _targetFieldIndicator.text = field != null ? $"ACTIVE FIELD: {field.transform.parent.name} / {field.name}" : "NO TARGET FIELD (REMOTE STREAMING)";
            }
        }

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        public void Open(TMP_InputField targetField = null)
        {
            gameObject.SetActive(true);
            _isOpen = true;
            _targetAlpha = 1.0f;
            _targetScale = Vector3.one;

            if (targetField != null)
            {
                SetTargetInputField(targetField);
            }
            else if (_activeInputField == null)
            {
                // Auto-pick the first active window's input field
                var anyField = Object.FindAnyObjectByType<TMP_InputField>();
                if (anyField != null) SetTargetInputField(anyField);
            }

            transform.SetAsLastSibling();

            // Ensure cursor stays above keyboard
            if (_canvasRect != null)
            {
                Transform cursorTr = _canvasRect.Find("VRCursor");
                if (cursorTr != null) cursorTr.SetAsLastSibling();
            }
        }

        public void Close()
        {
            _isOpen = false;
            _targetAlpha = 0f;
            _targetScale = Vector3.one * 0.85f;
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }

        private void CloseImmediate()
        {
            _isOpen = false;
            _targetAlpha = 0f;
            _targetScale = Vector3.one * 0.85f;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
        }

        private void ProcessPhysicalKeyboardInput()
        {
            if (!_isOpen && _activeInputField == null) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            // Shift
            bool shiftHeld = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            if (shiftHeld != _isShiftActive)
            {
                _isShiftActive = shiftHeld;
                UpdateAllKeysDisplay();
            }

            // Enter
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                HandleEnter();
            }

            // Backspace
            if (kb.backspaceKey.wasPressedThisFrame)
            {
                HandleBackspace();
            }

            // Space
            if (kb.spaceKey.wasPressedThisFrame)
            {
                InsertCharacter(" ");
            }
        }

        private void HandleKeyPress(VirtualKey key)
        {
            switch (key.ActionType)
            {
                case KeyActionType.Character:
                    bool upper = _isShiftActive ^ _isCapsLockActive;
                    string ch = upper ? key.ShiftChar : key.PrimaryChar;
                    InsertCharacter(ch);
                    if (_isShiftActive)
                    {
                        _isShiftActive = false;
                        UpdateAllKeysDisplay();
                    }
                    break;

                case KeyActionType.Space:
                    InsertCharacter(" ");
                    break;

                case KeyActionType.Backspace:
                    HandleBackspace();
                    break;

                case KeyActionType.Enter:
                    HandleEnter();
                    break;

                case KeyActionType.Shift:
                    _isShiftActive = !_isShiftActive;
                    UpdateAllKeysDisplay();
                    break;

                case KeyActionType.CapsLock:
                    _isCapsLockActive = !_isCapsLockActive;
                    UpdateAllKeysDisplay();
                    break;

                case KeyActionType.Tab:
                    InsertCharacter("    ");
                    break;

                case KeyActionType.Clear:
                    if (_activeInputField != null)
                    {
                        _activeInputField.text = "";
                    }
                    break;

                case KeyActionType.Hide:
                    Close();
                    break;
            }
        }

        private void InsertCharacter(string text)
        {
            if (_activeInputField != null)
            {
                _activeInputField.text += text;
                _activeInputField.caretPosition = _activeInputField.text.Length;
                _activeInputField.ActivateInputField();
            }

            // Forward to Remote Provider
            if (MockRemoteInputProvider.Instance != null)
            {
                MockRemoteInputProvider.Instance.SendTextInput(text);
            }
        }

        private void HandleBackspace()
        {
            if (_activeInputField != null && !string.IsNullOrEmpty(_activeInputField.text))
            {
                _activeInputField.text = _activeInputField.text.Substring(0, _activeInputField.text.Length - 1);
                _activeInputField.caretPosition = _activeInputField.text.Length;
                _activeInputField.ActivateInputField();
            }
        }

        private void HandleEnter()
        {
            if (_activeInputField != null)
            {
                _activeInputField.onSubmit?.Invoke(_activeInputField.text);
            }
        }

        private void UpdateAllKeysDisplay()
        {
            foreach (var k in _keys)
            {
                if (k != null) k.UpdateDisplay(_isShiftActive, _isCapsLockActive);
            }
        }

        // =========================================================================
        // PROCEDURAL KEYBOARD BUILDER
        // =========================================================================

        private void BuildKeyboardLayout()
        {
            // Set keyboard panel size and position (docked bottom center: 880 x 280)
            if (_keyboardRect == null) _keyboardRect = GetComponent<RectTransform>();
            _keyboardRect.anchorMin = new Vector2(0.5f, 0f);
            _keyboardRect.anchorMax = new Vector2(0.5f, 0f);
            _keyboardRect.pivot = new Vector2(0.5f, 0f);
            _keyboardRect.anchoredPosition = new Vector2(0f, 65f); // Above the dock
            _keyboardRect.sizeDelta = new Vector2(880f, 280f);

            var bgImg = GetComponent<Image>();
            if (bgImg == null) bgImg = gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.07f, 0.12f, 0.96f);

            // Glow Border
            GameObject borderObj = new GameObject("GlowBorder", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(transform, false);
            var borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-1, -1);
            borderRt.offsetMax = new Vector2(1, 1);
            var borderImg = borderObj.GetComponent<Image>();
            borderImg.color = new Color(0.0f, 0.85f, 1.0f, 0.4f);
            borderRt.SetAsFirstSibling();

            // Header Bar
            GameObject header = new GameObject("HeaderBar", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(transform, false);
            var headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0, 32);
            header.GetComponent<Image>().color = new Color(0.06f, 0.10f, 0.18f, 0.98f);

            // Header Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(header.transform, false);
            var tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0);
            tRt.anchorMax = new Vector2(0.5f, 1);
            tRt.offsetMin = new Vector2(16, 0);
            tRt.offsetMax = Vector2.zero;
            var tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            if (_defaultFont != null) tTmp.font = _defaultFont;
            tTmp.text = "VIRTUAL KEYBOARD - Remote Input Stream";
            tTmp.fontSize = 11;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = new Color(0f, 1f, 1f, 1f);
            tTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Target Field Indicator
            GameObject indObj = new GameObject("Indicator", typeof(RectTransform), typeof(TextMeshProUGUI));
            indObj.transform.SetParent(header.transform, false);
            var iRt = indObj.GetComponent<RectTransform>();
            iRt.anchorMin = new Vector2(0.5f, 0);
            iRt.anchorMax = new Vector2(0.92f, 1);
            iRt.offsetMin = Vector2.zero;
            iRt.offsetMax = Vector2.zero;
            _targetFieldIndicator = indObj.GetComponent<TextMeshProUGUI>();
            if (_defaultFont != null) _targetFieldIndicator.font = _defaultFont;
            _targetFieldIndicator.text = "SELECT ANY INPUT FIELD TO TYPE";
            _targetFieldIndicator.fontSize = 10;
            _targetFieldIndicator.color = new Color(0.7f, 0.8f, 0.9f, 0.8f);
            _targetFieldIndicator.alignment = TextAlignmentOptions.MidlineRight;

            // Close Button
            GameObject closeBtnObj = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnObj.transform.SetParent(header.transform, false);
            var cRt = closeBtnObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.anchoredPosition = new Vector2(-16, -16);
            cRt.sizeDelta = new Vector2(24, 24);
            closeBtnObj.GetComponent<Image>().color = new Color(1f, 0.2f, 0.35f, 0.3f);
            var cBtn = closeBtnObj.GetComponent<Button>();
            cBtn.onClick.AddListener(Close);

            GameObject closeSym = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeSym.transform.SetParent(closeBtnObj.transform, false);
            var csRt = closeSym.GetComponent<RectTransform>();
            csRt.anchorMin = Vector2.zero;
            csRt.anchorMax = Vector2.one;
            csRt.offsetMin = Vector2.zero;
            csRt.offsetMax = Vector2.zero;
            var csTmp = closeSym.GetComponent<TextMeshProUGUI>();
            if (_defaultFont != null) csTmp.font = _defaultFont;
            csTmp.text = "X";
            csTmp.fontSize = 11;
            csTmp.alignment = TextAlignmentOptions.Center;
            csTmp.color = Color.white;
            csTmp.raycastTarget = false;

            // Keys Rows Container
            GameObject rowsContainer = new GameObject("RowsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rowsContainer.transform.SetParent(transform, false);
            var rcRt = rowsContainer.GetComponent<RectTransform>();
            rcRt.anchorMin = Vector2.zero;
            rcRt.anchorMax = Vector2.one;
            rcRt.offsetMin = new Vector2(12, 10);
            rcRt.offsetMax = new Vector2(-12, -38);

            var vlg = rowsContainer.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // Define QWERTY rows
            string[][] row1 = {
                new[] { "`", "~" }, new[] { "1", "!" }, new[] { "2", "@" }, new[] { "3", "#" },
                new[] { "4", "$" }, new[] { "5", "%" }, new[] { "6", "^" }, new[] { "7", "&" },
                new[] { "8", "*" }, new[] { "9", "(" }, new[] { "0", ")" }, new[] { "-", "_" },
                new[] { "=", "+" }, new[] { "BACK", "" }
            };

            string[][] row2 = {
                new[] { "TAB", "" }, new[] { "q", "Q" }, new[] { "w", "W" }, new[] { "e", "E" },
                new[] { "r", "R" }, new[] { "t", "T" }, new[] { "y", "Y" }, new[] { "u", "U" },
                new[] { "i", "I" }, new[] { "o", "O" }, new[] { "p", "P" }, new[] { "[", "{" },
                new[] { "]", "}" }, new[] { "\\", "|" }
            };

            string[][] row3 = {
                new[] { "CAPS", "" }, new[] { "a", "A" }, new[] { "s", "S" }, new[] { "d", "D" },
                new[] { "f", "F" }, new[] { "g", "G" }, new[] { "h", "H" }, new[] { "j", "J" },
                new[] { "k", "K" }, new[] { "l", "L" }, new[] { ";", ":" }, new[] { "'", "\"" },
                new[] { "ENTER", "" }
            };

            string[][] row4 = {
                new[] { "SHIFT", "" }, new[] { "z", "Z" }, new[] { "x", "X" }, new[] { "c", "C" },
                new[] { "v", "V" }, new[] { "b", "B" }, new[] { "n", "N" }, new[] { "m", "M" },
                new[] { ",", "<" }, new[] { ".", ">" }, new[] { "/", "?" }, new[] { "SHIFT", "" }
            };

            string[][] row5 = {
                new[] { "CLEAR", "" }, new[] { "SPACE", "" }, new[] { "HIDE", "" }
            };

            CreateRow(rowsContainer.transform, row1, 1);
            CreateRow(rowsContainer.transform, row2, 2);
            CreateRow(rowsContainer.transform, row3, 3);
            CreateRow(rowsContainer.transform, row4, 4);
            CreateRow(rowsContainer.transform, row5, 5);
        }

        private void CreateRow(Transform container, string[][] keysData, int rowIdx)
        {
            GameObject rowObj = new GameObject("Row_" + rowIdx, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObj.transform.SetParent(container, false);

            var hlg = rowObj.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            foreach (var keyPair in keysData)
            {
                string p = keyPair[0];
                string s = keyPair[1];

                KeyActionType action = KeyActionType.Character;
                float keyWidth = 52f;

                if (p == "BACK") { action = KeyActionType.Backspace; keyWidth = 75f; }
                else if (p == "TAB") { action = KeyActionType.Tab; keyWidth = 72f; }
                else if (p == "CAPS") { action = KeyActionType.CapsLock; keyWidth = 75f; }
                else if (p == "ENTER") { action = KeyActionType.Enter; keyWidth = 95f; }
                else if (p == "SHIFT") { action = KeyActionType.Shift; keyWidth = 92f; }
                else if (p == "SPACE") { action = KeyActionType.Space; keyWidth = 420f; }
                else if (p == "CLEAR") { action = KeyActionType.Clear; keyWidth = 100f; }
                else if (p == "HIDE") { action = KeyActionType.Hide; keyWidth = 100f; }

                CreateKey(rowObj.transform, action, p, s, new Vector2(keyWidth, 38f));
            }
        }

        private void CreateKey(Transform row, KeyActionType action, string primary, string shift, Vector2 size)
        {
            GameObject keyObj = new GameObject("Key_" + primary, typeof(RectTransform), typeof(Image), typeof(Button), typeof(VirtualKey));
            keyObj.transform.SetParent(row, false);

            var rt = keyObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = keyObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.12f, 0.20f, 0.95f);
            img.raycastTarget = true;

            var btn = keyObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.08f, 0.12f, 0.20f, 0.95f);
            cb.highlightedColor = new Color(0.12f, 0.20f, 0.35f, 1.0f);
            cb.pressedColor = new Color(0.0f, 0.8f, 1.0f, 0.8f);
            btn.colors = cb;

            // Border
            GameObject borderObj = new GameObject("KeyBorder", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(keyObj.transform, false);
            var bRt = borderObj.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.offsetMin = new Vector2(-1, -1);
            bRt.offsetMax = new Vector2(1, 1);
            var bImg = borderObj.GetComponent<Image>();
            bImg.color = new Color(0.0f, 0.6f, 0.9f, 0.3f);
            bImg.raycastTarget = false;
            bRt.SetAsFirstSibling();

            // Label
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(keyObj.transform, false);
            var lRt = labelObj.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;

            var tmp = labelObj.GetComponent<TextMeshProUGUI>();
            if (_defaultFont != null) tmp.font = _defaultFont;
            tmp.text = primary;
            tmp.fontSize = action == KeyActionType.Character ? 13 : 10;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = action == KeyActionType.Character ? Color.white : new Color(0.2f, 0.9f, 1.0f);
            tmp.raycastTarget = false;

            var vKey = keyObj.GetComponent<VirtualKey>();
            vKey.Setup(action, primary, shift, size);
            vKey.OnKeyPressed += HandleKeyPress;

            btn.onClick.AddListener(() => HandleKeyPress(vKey));

            _keys.Add(vKey);
        }
    }
}
