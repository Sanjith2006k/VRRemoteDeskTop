using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace VRRemoteDesktop.UI.Windows
{
    /// <summary>
    /// Cyberpunk Notepad & Text Editor application for the VR Remote Desktop.
    /// Provides multi-line text editing, document templates, save/load simulation,
    /// live line/word/character telemetry, and seamless integration with the Virtual Keyboard.
    /// </summary>
    public class NotepadApp : MonoBehaviour
    {
        [Header("Core UI Components")]
        [SerializeField] private TMP_InputField _editorInputField;
        [SerializeField] private TextMeshProUGUI _statusBarText;
        [SerializeField] private TextMeshProUGUI _documentNameText;

        [Header("Toolbar Action Buttons")]
        [SerializeField] private Button _newButton;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _saveButton;
        [SerializeField] private Button _clearButton;
        [SerializeField] private Button _keyboardButton;

        private DesktopWindow _parentWindow;
        private string _currentFileName = "notes.txt";
        private bool _isDirty = false;
        private int _sampleDocIndex = 0;

        private readonly string[] _sampleDocs = new[]
        {
@"# VR SPATIAL WORKSTATION v2.4
=====================================================
Operator: root@vr-desktop
Location: Station-01 (LAN 10 Gbps)
Protocol: Ultra-Low Latency WebRTC / H.265 Ultra

[SYSTEM LOGS]
- Holographic desktop environment initialized.
- Multi-window multitasking operational.
- Cyber Terminal (vsh) & Quantum Browser online.
- Virtual QWERTY Keyboard connected to remote input.

[PROJECT MILESTONES]
[x] Zero-latency plane raycast pointer
[x] Window drag, minimize, maximize & close controls
[x] Terminal shell with command execution
[x] Cyber Notepad text editor with live metrics
[ ] Deploy neural gesture shortcuts
=====================================================",

@"// CONFIGURATION PROFILE: workstation_config.json
{
  ""environment"": ""VR_CYBERPUNK_SPATIAL"",
  ""display"": {
    ""resolution"": ""1920x1080"",
    ""refreshRate"": 60,
    ""vsync"": true
  },
  ""remoteStreaming"": {
    ""protocol"": ""WebRTC-H265"",
    ""targetHost"": ""192.168.1.100"",
    ""port"": 8080,
    ""encryption"": ""ChaCha20-Poly1305"",
    ""hardwareAcceleration"": true
  },
  ""keyboard"": {
    ""layout"": ""QWERTY_CYBER"",
    ""hapticFeedback"": true
  }
}",

@"=== README_WORKSPACE.TXT ===
Welcome to the Cyberpunk VR Remote Desktop System!

Key Features:
1. Double click or click any app icon to open applications.
2. Drag windows by clicking and holding the Title Bar.
3. Use header buttons to Minimize (-), Maximize ([ ]), or Close (X).
4. Terminal: Type 'help' for available terminal commands.
5. Notepad: Full multi-line text editor with live word and line counter.
6. Keyboard: Click input field or press KEYBOARD to toggle on-screen keys."
        };

        private void Awake()
        {
            _parentWindow = GetComponentInParent<DesktopWindow>();
            HookEvents();
        }

        private void Start()
        {
            if (_editorInputField != null && string.IsNullOrEmpty(_editorInputField.text))
            {
                LoadDocument(0);
            }
            UpdateStatusMetrics();
        }

        public void Setup(
            TMP_InputField inputField,
            TextMeshProUGUI statusText,
            TextMeshProUGUI docNameText,
            Button newBtn,
            Button openBtn,
            Button saveBtn,
            Button clearBtn,
            Button kbBtn,
            DesktopWindow parentWin = null)
        {
            _editorInputField = inputField;
            if (_editorInputField != null)
            {
                _editorInputField.onFocusSelectAll = false;
            }
            _statusBarText = statusText;
            _documentNameText = docNameText;
            _newButton = newBtn;
            _openButton = openBtn;
            _saveButton = saveBtn;
            _clearButton = clearBtn;
            _keyboardButton = kbBtn;

            if (parentWin != null)
            {
                _parentWindow = parentWin;
            }
            else if (_parentWindow == null)
            {
                _parentWindow = GetComponentInParent<DesktopWindow>();
            }

            if (_parentWindow != null)
            {
                _parentWindow.OnWindowFocused += HandleWindowFocused;
            }

            HookEvents();

            if (_editorInputField != null && string.IsNullOrEmpty(_editorInputField.text))
            {
                LoadDocument(0);
            }
            UpdateStatusMetrics();
        }

        private void OnDestroy()
        {
            if (_parentWindow != null)
            {
                _parentWindow.OnWindowFocused -= HandleWindowFocused;
            }
        }

        private void HandleWindowFocused(DesktopWindow win)
        {
            FocusEditor();
        }

        private void HookEvents()
        {
            if (_editorInputField != null)
            {
                _editorInputField.onValueChanged.RemoveListener(HandleTextChanged);
                _editorInputField.onValueChanged.AddListener(HandleTextChanged);
                _editorInputField.onSelect.RemoveListener(HandleFieldSelected);
                _editorInputField.onSelect.AddListener(HandleFieldSelected);
            }

            if (_newButton != null)
            {
                _newButton.onClick.RemoveAllListeners();
                _newButton.onClick.AddListener(CreateNewDocument);
            }

            if (_openButton != null)
            {
                _openButton.onClick.RemoveAllListeners();
                _openButton.onClick.AddListener(CycleOpenSampleDocument);
            }

            if (_saveButton != null)
            {
                _saveButton.onClick.RemoveAllListeners();
                _saveButton.onClick.AddListener(SaveCurrentDocument);
            }

            if (_clearButton != null)
            {
                _clearButton.onClick.RemoveAllListeners();
                _clearButton.onClick.AddListener(ClearEditor);
            }

            if (_keyboardButton != null)
            {
                _keyboardButton.onClick.RemoveAllListeners();
                _keyboardButton.onClick.AddListener(ToggleVirtualKeyboard);
            }
        }

        public void FocusEditor()
        {
            if (_editorInputField != null)
            {
                _editorInputField.onFocusSelectAll = false;
                _editorInputField.ActivateInputField();
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(_editorInputField.gameObject);
                }
                if (Keyboard.VirtualKeyboard.Instance != null && Keyboard.VirtualKeyboard.Instance.IsOpen)
                {
                    Keyboard.VirtualKeyboard.Instance.SetTargetInputField(_editorInputField);
                }
            }
        }

        private void HandleFieldSelected(string val)
        {
            if (Keyboard.VirtualKeyboard.Instance != null)
            {
                Keyboard.VirtualKeyboard.Instance.SetTargetInputField(_editorInputField);
            }
        }

        private void HandleTextChanged(string text)
        {
            _isDirty = true;
            UpdateStatusMetrics();
        }

        public void CreateNewDocument()
        {
            _currentFileName = "untitled.txt";
            if (_editorInputField != null)
            {
                _editorInputField.text = "";
                _editorInputField.ActivateInputField();
            }
            _isDirty = false;
            UpdateDocTitle();
            SetStatusToast("<color=#00ff88>New document created.</color>");
        }

        public void CycleOpenSampleDocument()
        {
            _sampleDocIndex = (_sampleDocIndex + 1) % _sampleDocs.Length;
            LoadDocument(_sampleDocIndex);
        }

        public void LoadDocument(int index)
        {
            _sampleDocIndex = Mathf.Clamp(index, 0, _sampleDocs.Length - 1);
            switch (_sampleDocIndex)
            {
                case 0: _currentFileName = "notes.txt"; break;
                case 1: _currentFileName = "workstation_config.json"; break;
                case 2: _currentFileName = "readme_workspace.txt"; break;
            }

            if (_editorInputField != null)
            {
                _editorInputField.onFocusSelectAll = false;
                _editorInputField.text = _sampleDocs[_sampleDocIndex];
                _editorInputField.caretPosition = 0;
                _editorInputField.selectionAnchorPosition = 0;
                _editorInputField.selectionStringAnchorPosition = 0;
                _editorInputField.selectionStringFocusPosition = 0;
            }
            _isDirty = false;
            UpdateDocTitle();
            SetStatusToast($"<color=#00ffff>Loaded {_currentFileName}</color>");
        }

        public void OpenCustomDocument(string fileName, string content)
        {
            _currentFileName = string.IsNullOrEmpty(fileName) ? "untitled.txt" : fileName;
            if (_editorInputField != null)
            {
                _editorInputField.onFocusSelectAll = false;
                _editorInputField.text = content ?? "";
                _editorInputField.caretPosition = 0;
                _editorInputField.selectionAnchorPosition = 0;
                _editorInputField.selectionStringAnchorPosition = 0;
                _editorInputField.selectionStringFocusPosition = 0;
            }
            _isDirty = false;
            UpdateDocTitle();
            SetStatusToast($"<color=#00ff88>[OK] Opened {_currentFileName} in Notepad</color>");
        }

        public void SaveCurrentDocument()
        {
            _isDirty = false;
            UpdateDocTitle();
            SetStatusToast($"<color=#00ff88>[OK] Saved to /workspace/{_currentFileName} [UTF-8]</color>");
        }

        public void ClearEditor()
        {
            if (_editorInputField != null)
            {
                _editorInputField.text = "";
                _editorInputField.ActivateInputField();
            }
            _isDirty = true;
            UpdateStatusMetrics();
            SetStatusToast("<color=#ffaa00>Editor buffer cleared.</color>");
        }

        public void ToggleVirtualKeyboard()
        {
            if (Keyboard.VirtualKeyboard.Instance != null)
            {
                if (Keyboard.VirtualKeyboard.Instance.IsOpen)
                {
                    Keyboard.VirtualKeyboard.Instance.Close();
                }
                else
                {
                    Keyboard.VirtualKeyboard.Instance.Open(_editorInputField);
                }
            }
            else
            {
                DesktopWindowManager.Instance?.EnsureVirtualKeyboard();
                Keyboard.VirtualKeyboard.Instance?.Open(_editorInputField);
            }
        }

        private void UpdateDocTitle()
        {
            if (_documentNameText != null)
            {
                string tag = _isDirty ? "<color=#ffaa00> [*] [MODIFIED]</color>" : "<color=#00ff88> [SAVED]</color>";
                _documentNameText.text = $"<color=#00ffff><b>{_currentFileName}</b></color> <color=#b0c8e8>[UTF-8]</color>{tag}";
            }
        }

        private void UpdateStatusMetrics()
        {
            UpdateDocTitle();

            if (_statusBarText == null) return;

            string text = _editorInputField != null ? _editorInputField.text : "";
            int charCount = text.Length;
            int lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
            int wordCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

            _statusBarText.text = $"<color=#00ffff>Lines:</color> <color=#ffffff>{lineCount}</color>  |  <color=#00ffff>Words:</color> <color=#ffffff>{wordCount}</color>  |  <color=#00ffff>Chars:</color> <color=#ffffff>{charCount}</color>  |  <color=#00ff88>READY</color>";
        }

        public void SetStatusToast(string message)
        {
            if (_statusBarText != null)
            {
                string text = _editorInputField != null ? _editorInputField.text : "";
                int charCount = text.Length;
                int lineCount = string.IsNullOrEmpty(text) ? 1 : text.Split('\n').Length;
                int wordCount = string.IsNullOrWhiteSpace(text) ? 0 : text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

                _statusBarText.text = $"L:{lineCount} W:{wordCount} C:{charCount}  |  {message}";
            }
        }
    }
}
