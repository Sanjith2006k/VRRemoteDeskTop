using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRRemoteDesktop.UI.Windows
{
    public class FileItemData
    {
        public string Name;
        public string Extension;
        public string Size;
        public string Type;
        public string Modified;
        public string ContentSnippet;
        public Color AccentColor;
    }

    /// <summary>
    /// File Manager application for the virtual desktop.
    /// Provides folder navigation, search, file listing with cyber aesthetics, and file detail preview.
    /// </summary>
    public class FileManagerApp : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _pathText;
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private Transform _fileListContainer;
        [SerializeField] private TextMeshProUGUI _selectedFileName;
        [SerializeField] private TextMeshProUGUI _selectedFileInfo;
        [SerializeField] private TextMeshProUGUI _selectedFileContent;
        [SerializeField] private Button _openButton;

        private List<FileItemData> _allFiles = new List<FileItemData>();
        private List<FileItemData> _currentFiles = new List<FileItemData>();
        private FileItemData _selectedFile;
        private readonly Dictionary<FileItemData, Image> _rowBackgrounds = new Dictionary<FileItemData, Image>();
        private string _activeCategory = "All Files";

        private static readonly Color RowNormalColor = new Color(0.04f, 0.07f, 0.12f, 0.85f);
        private static readonly Color RowSelectedColor = new Color(0.10f, 0.24f, 0.38f, 0.95f);

        private void Awake()
        {
            PopulateMockFiles();
        }

        private void Start()
        {
            if (_fileListContainer != null && _fileListContainer.childCount == 0)
            {
                RenderFiles();
            }
        }

        public void Setup(
            Transform fileContainer,
            TextMeshProUGUI nameText,
            TextMeshProUGUI infoText,
            TextMeshProUGUI contentText,
            Button openBtn = null)
        {
            _fileListContainer = fileContainer;
            _selectedFileName = nameText;
            _selectedFileInfo = infoText;
            _selectedFileContent = contentText;
            _openButton = openBtn;

            if (_openButton != null)
            {
                _openButton.onClick.RemoveAllListeners();
                _openButton.onClick.AddListener(OpenSelectedFile);
            }

            if (_allFiles == null || _allFiles.Count == 0)
            {
                PopulateMockFiles();
            }

            RenderFiles();
        }

        private void PopulateMockFiles()
        {
            _allFiles = new List<FileItemData>
            {
                new FileItemData {
                    Name = "quantum_core.sys",
                    Extension = "sys",
                    Size = "14.2 MB",
                    Type = "System Driver",
                    Modified = "Today, 14:22",
                    ContentSnippet = "[KERNEL MODULE 0x8841F]\nSTATUS: ACTIVE\nDRIVER: Spatial Compositor v6.8\nBUFFER_ADDR: 0x7FFF99402\nENCRYPTION: HARDWARE ACCELERATED\nFRAME_SYNC: LOCKED 60.0 FPS",
                    AccentColor = new Color(0f, 1f, 1f)
                },
                new FileItemData {
                    Name = "remote_display.hlsl",
                    Extension = "hlsl",
                    Size = "28.4 KB",
                    Type = "Shader Program",
                    Modified = "Yesterday, 19:40",
                    ContentSnippet = "#pragma vertex Vert\n#pragma fragment Frag\n\nTexture2D _RemoteBuffer;\nSamplerState sampler_RemoteBuffer;\n\nfloat4 Frag(Varyings i) : SV_Target {\n    return _RemoteBuffer.Sample(sampler_RemoteBuffer, i.uv);\n}",
                    AccentColor = new Color(0.65f, 0.30f, 1.0f)
                },
                new FileItemData {
                    Name = "host_credentials.key",
                    Extension = "key",
                    Size = "4.0 KB",
                    Type = "Security Key",
                    Modified = "Oct 01, 10:15",
                    ContentSnippet = "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA3Q2kZ7y5v9kM1xP4sQ8...[ENCRYPTED HOST TOKEN]...\nvr_token_hash=d9a8c1f9b02e77b42c4a9\n-----END RSA PRIVATE KEY-----",
                    AccentColor = new Color(1f, 0.05f, 0.55f)
                },
                new FileItemData {
                    Name = "workstation_config.json",
                    Extension = "json",
                    Size = "8.6 KB",
                    Type = "JSON Config",
                    Modified = "Today, 09:12",
                    ContentSnippet = "{\n  \"host\": \"DESKTOP-VR-HOST\",\n  \"protocol\": \"WebRTC-H265\",\n  \"target_fps\": 60,\n  \"latency_ms\": 18,\n  \"resolution\": \"1920x1080\",\n  \"vsync\": true\n}",
                    AccentColor = new Color(0.2f, 0.9f, 0.4f)
                },
                new FileItemData {
                    Name = "telemetry_stream.log",
                    Extension = "log",
                    Size = "1.2 MB",
                    Type = "Log File",
                    Modified = "Today, 15:05",
                    ContentSnippet = "[15:00:01] NET: Handshake successful (10 Gbps LAN)\n[15:00:02] GPU: Hardware decode ready (NVIDIA NVDEC)\n[15:00:05] FPS: Stable 60.0 fps (Jitter: 0.4ms)\n[15:02:11] AUDIO: Spatial binaural pipeline connected",
                    AccentColor = new Color(1f, 0.8f, 0.1f)
                },
                new FileItemData {
                    Name = "readme_workspace.txt",
                    Extension = "txt",
                    Size = "2.1 KB",
                    Type = "Text Document",
                    Modified = "Sep 28, 16:30",
                    ContentSnippet = "VR REMOTE WORKSTATION v2.4\n================================\nWelcome to your spatial computing workstation.\nUse WASD to navigate the physical room.\nPress [E] to seat and interact with the remote desktop.\nPress [ESC] at any time to return to exploration.",
                    AccentColor = new Color(0.1f, 0.8f, 1f)
                }
            };

            _currentFiles = new List<FileItemData>(_allFiles);
        }

        public void FilterCategory(string category)
        {
            _activeCategory = category;
            if (string.IsNullOrEmpty(category) || category == "All Files" || category == "Root")
            {
                _currentFiles = new List<FileItemData>(_allFiles);
            }
            else if (category.Contains("Driver"))
            {
                _currentFiles = _allFiles.FindAll(f => f.Extension == "sys");
            }
            else if (category.Contains("Config") || category.Contains("Logs"))
            {
                _currentFiles = _allFiles.FindAll(f => f.Extension == "json" || f.Extension == "log");
            }
            else if (category.Contains("Security") || category.Contains("Key"))
            {
                _currentFiles = _allFiles.FindAll(f => f.Extension == "key");
            }
            else if (category.Contains("Doc") || category.Contains("Text") || category.Contains("Shader"))
            {
                _currentFiles = _allFiles.FindAll(f => f.Extension == "txt" || f.Extension == "hlsl");
            }
            else
            {
                _currentFiles = new List<FileItemData>(_allFiles);
            }

            RenderFiles();
        }

        public void RenderFiles()
        {
            if (_fileListContainer == null) return;

            _rowBackgrounds.Clear();

            // Clear old children
            for (int i = _fileListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_fileListContainer.GetChild(i).gameObject);
            }

            foreach (var file in _currentFiles)
            {
                CreateFileRow(file);
            }

            if (_currentFiles.Count > 0)
            {
                SelectFile(_currentFiles[0]);
            }
        }

        private void CreateFileRow(FileItemData data)
        {
            GameObject rowObj = new GameObject("FileRow_" + data.Name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rowObj.transform.SetParent(_fileListContainer, false);

            LayoutElement le = rowObj.GetComponent<LayoutElement>();
            le.minHeight = 38f;
            le.preferredHeight = 38f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            Image img = rowObj.GetComponent<Image>();
            img.color = RowNormalColor;
            _rowBackgrounds[data] = img;

            Button btn = rowObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = RowNormalColor;
            cb.highlightedColor = new Color(0.12f, 0.22f, 0.35f, 0.95f);
            cb.pressedColor = new Color(0.18f, 0.32f, 0.50f, 1.0f);
            btn.colors = cb;

            btn.onClick.AddListener(() => SelectFile(data));

            // Icon indicator
            GameObject iconObj = new GameObject("FileIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(rowObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = new Vector2(16f, 0f);
            iconRt.sizeDelta = new Vector2(14f, 14f);

            Image iconImg = iconObj.GetComponent<Image>();
            iconImg.color = data.AccentColor;
            iconImg.raycastTarget = false;

            // Name Text (0% to 48%)
            GameObject nameObj = new GameObject("FileName", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameObj.transform.SetParent(rowObj.transform, false);
            RectTransform nameRt = nameObj.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(0.48f, 1f);
            nameRt.offsetMin = new Vector2(34f, 0f);
            nameRt.offsetMax = new Vector2(-4f, 0f);

            TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
            nameTmp.text = data.Name;
            nameTmp.fontSize = 18;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = Color.white;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.raycastTarget = false;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Type Text (48% to 68%)
            GameObject typeObj = new GameObject("FileType", typeof(RectTransform), typeof(TextMeshProUGUI));
            typeObj.transform.SetParent(rowObj.transform, false);
            RectTransform typeRt = typeObj.GetComponent<RectTransform>();
            typeRt.anchorMin = new Vector2(0.48f, 0f);
            typeRt.anchorMax = new Vector2(0.68f, 1f);
            typeRt.offsetMin = new Vector2(4f, 0f);
            typeRt.offsetMax = new Vector2(-4f, 0f);

            TextMeshProUGUI typeTmp = typeObj.GetComponent<TextMeshProUGUI>();
            typeTmp.text = data.Type;
            typeTmp.fontSize = 16;
            typeTmp.color = new Color(0.65f, 0.78f, 0.90f);
            typeTmp.alignment = TextAlignmentOptions.MidlineLeft;
            typeTmp.raycastTarget = false;

            // Size Text (68% to 83%)
            GameObject sizeObj = new GameObject("FileSize", typeof(RectTransform), typeof(TextMeshProUGUI));
            sizeObj.transform.SetParent(rowObj.transform, false);
            RectTransform sizeRt = sizeObj.GetComponent<RectTransform>();
            sizeRt.anchorMin = new Vector2(0.68f, 0f);
            sizeRt.anchorMax = new Vector2(0.83f, 1f);
            sizeRt.offsetMin = new Vector2(4f, 0f);
            sizeRt.offsetMax = new Vector2(-4f, 0f);

            TextMeshProUGUI sizeTmp = sizeObj.GetComponent<TextMeshProUGUI>();
            sizeTmp.text = data.Size;
            sizeTmp.fontSize = 16;
            sizeTmp.color = new Color(0.80f, 0.85f, 0.92f);
            sizeTmp.alignment = TextAlignmentOptions.MidlineLeft;
            sizeTmp.raycastTarget = false;

            // Modified Text (83% to 100%)
            GameObject modObj = new GameObject("FileMod", typeof(RectTransform), typeof(TextMeshProUGUI));
            modObj.transform.SetParent(rowObj.transform, false);
            RectTransform modRt = modObj.GetComponent<RectTransform>();
            modRt.anchorMin = new Vector2(0.83f, 0f);
            modRt.anchorMax = new Vector2(1f, 1f);
            modRt.offsetMin = new Vector2(4f, 0f);
            modRt.offsetMax = new Vector2(-12f, 0f);

            TextMeshProUGUI modTmp = modObj.GetComponent<TextMeshProUGUI>();
            modTmp.text = data.Modified;
            modTmp.fontSize = 15;
            modTmp.color = new Color(0.55f, 0.65f, 0.75f);
            modTmp.alignment = TextAlignmentOptions.MidlineRight;
            modTmp.raycastTarget = false;
        }

        public void SelectFile(FileItemData data)
        {
            if (data == null) return;
            _selectedFile = data;

            // Highlight selected row
            foreach (var kvp in _rowBackgrounds)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.color = (kvp.Key == data) ? RowSelectedColor : RowNormalColor;
                }
            }

            if (_selectedFileName != null)
            {
                _selectedFileName.text = $"<color=#{ColorUtility.ToHtmlStringRGB(data.AccentColor)}>[*]</color> {data.Name}";
            }

            if (_selectedFileInfo != null)
            {
                _selectedFileInfo.text = $"<b>{data.Type}</b>  |  {data.Size}  |  Modified {data.Modified}";
            }

            if (_selectedFileContent != null)
            {
                _selectedFileContent.text = data.ContentSnippet;
            }
        }

        public void OpenSelectedFile()
        {
            if (_selectedFile == null && _currentFiles.Count > 0)
            {
                _selectedFile = _currentFiles[0];
            }

            if (_selectedFile == null) return;

            // Launch Notepad and pass file content
            if (DesktopWindowManager.Instance != null)
            {
                DesktopWindowManager.Instance.OpenWindow("Notepad");
            }

            NotepadApp notepad = UnityEngine.Object.FindAnyObjectByType<NotepadApp>();
            if (notepad != null)
            {
                notepad.OpenCustomDocument(_selectedFile.Name, _selectedFile.ContentSnippet);
            }
        }
    }
}
