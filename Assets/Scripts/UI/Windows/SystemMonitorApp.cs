
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRRemoteDesktop.UI.Windows
{
    public class ProcessItemData
    {
        public int Pid;
        public string Name;
        public float CpuPercent;
        public int MemoryMB;
        public string Status;
        public GameObject RowObj;
    }

    /// <summary>
    /// System Monitor application displaying live CPU/GPU/RAM/Network telemetry meters
    /// and interactive running processes table with real-time updates and task termination.
    /// </summary>
    public class SystemMonitorApp : MonoBehaviour
    {
        [Header("Gauges")]
        [SerializeField] private TextMeshProUGUI _cpuPercentText;
        [SerializeField] private Image _cpuFill;
        [SerializeField] private TextMeshProUGUI _gpuPercentText;
        [SerializeField] private Image _gpuFill;
        [SerializeField] private TextMeshProUGUI _ramPercentText;
        [SerializeField] private Image _ramFill;
        [SerializeField] private TextMeshProUGUI _netDownText;
        [SerializeField] private TextMeshProUGUI _subInfoText;

        [Header("Process Table")]
        [SerializeField] private Transform _processListContainer;

        private List<ProcessItemData> _processes = new List<ProcessItemData>();
        private float _updateTimer = 0f;

        private static readonly Color RowNormalColor = new Color(0.04f, 0.07f, 0.12f, 0.85f);

        private void Awake()
        {
            InitMockProcesses();
        }

        private void Start()
        {
            if (_processListContainer != null && _processListContainer.childCount == 0)
            {
                RenderProcessTable();
            }
            UpdateMetrics();
        }

        public void Setup(
            TextMeshProUGUI cpuText, Image cpuFill,
            TextMeshProUGUI gpuText, Image gpuFill,
            TextMeshProUGUI ramText, Image ramFill,
            TextMeshProUGUI netText,
            TextMeshProUGUI subInfoText,
            Transform processContainer)
        {
            _cpuPercentText = cpuText;
            _cpuFill = cpuFill;
            _gpuPercentText = gpuText;
            _gpuFill = gpuFill;
            _ramPercentText = ramText;
            _ramFill = ramFill;
            _netDownText = netText;
            _subInfoText = subInfoText;
            _processListContainer = processContainer;

            if (_processes == null || _processes.Count == 0)
            {
                InitMockProcesses();
            }

            RenderProcessTable();
            UpdateMetrics();
        }

        private void Update()
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= 0.9f)
            {
                _updateTimer = 0f;
                UpdateMetrics();
            }
        }

        private void InitMockProcesses()
        {
            _processes = new List<ProcessItemData>
            {
                new ProcessItemData { Pid = 1042, Name = "VRRemoteHost.exe", CpuPercent = 4.8f, MemoryMB = 412, Status = "Running" },
                new ProcessItemData { Pid = 824, Name = "DesktopCompositor.sys", CpuPercent = 3.2f, MemoryMB = 280, Status = "Running" },
                new ProcessItemData { Pid = 3401, Name = "NeuralRenderer.exe", CpuPercent = 8.5f, MemoryMB = 720, Status = "Running" },
                new ProcessItemData { Pid = 1902, Name = "AudioPipeline.dll", CpuPercent = 0.8f, MemoryMB = 64, Status = "Running" },
                new ProcessItemData { Pid = 512, Name = "NetworkTunnel.service", CpuPercent = 1.2f, MemoryMB = 98, Status = "Running" },
                new ProcessItemData { Pid = 2108, Name = "TelemetryCollector.daemon", CpuPercent = 0.5f, MemoryMB = 42, Status = "Running" }
            };
        }

        public void RenderProcessTable()
        {
            if (_processListContainer == null) return;

            for (int i = _processListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_processListContainer.GetChild(i).gameObject);
            }

            foreach (var p in _processes)
            {
                CreateProcessRow(p);
            }
        }

        private void CreateProcessRow(ProcessItemData proc)
        {
            GameObject rowObj = new GameObject("ProcRow_" + proc.Name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowObj.transform.SetParent(_processListContainer, false);
            proc.RowObj = rowObj;

            LayoutElement le = rowObj.GetComponent<LayoutElement>();
            le.minHeight = 38f;
            le.preferredHeight = 38f;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            Image img = rowObj.GetComponent<Image>();
            img.color = RowNormalColor;

            // Status Indicator Dot (Green pulse)
            GameObject dotObj = new GameObject("StatusDot", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(rowObj.transform, false);
            RectTransform dotRt = dotObj.GetComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0f, 0.5f);
            dotRt.anchorMax = new Vector2(0f, 0.5f);
            dotRt.pivot = new Vector2(0.5f, 0.5f);
            dotRt.anchoredPosition = new Vector2(14f, 0f);
            dotRt.sizeDelta = new Vector2(8f, 8f);

            Image dotImg = dotObj.GetComponent<Image>();
            dotImg.color = new Color(0f, 1f, 0.6f, 1f);
            dotImg.raycastTarget = false;

            // Name (0% to 44%)
            AddColText(rowObj.transform, proc.Name, new Vector2(0f, 0f), new Vector2(0.44f, 1f), 28f, FontStyles.Bold, Color.white);

            // PID (44% to 58%)
            AddColText(rowObj.transform, proc.Pid.ToString(), new Vector2(0.44f, 0f), new Vector2(0.58f, 1f), 4f, FontStyles.Normal, new Color(0.70f, 0.82f, 0.95f));

            // CPU % (58% to 70%)
            AddColText(rowObj.transform, $"{proc.CpuPercent:0.1}%", new Vector2(0.58f, 0f), new Vector2(0.70f, 1f), 4f, FontStyles.Bold, new Color(0f, 0.9f, 1f));

            // RAM (70% to 84%)
            AddColText(rowObj.transform, $"{proc.MemoryMB} MB", new Vector2(0.70f, 0f), new Vector2(0.84f, 1f), 4f, FontStyles.Normal, new Color(1f, 0.72f, 0.2f));

            // End Task Button (85% to 98.5%)
            GameObject btnObj = new GameObject("EndTaskBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(rowObj.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.85f, 0.5f);
            btnRt.anchorMax = new Vector2(0.985f, 0.5f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = Vector2.zero;
            btnRt.sizeDelta = new Vector2(0f, 26f);

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.80f, 0.15f, 0.32f, 0.85f);
            btnImg.raycastTarget = true;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.80f, 0.15f, 0.32f, 0.85f);
            cb.highlightedColor = new Color(1.0f, 0.25f, 0.45f, 1.0f);
            cb.pressedColor = new Color(0.60f, 0.10f, 0.25f, 1.0f);
            btn.colors = cb;

            btn.onClick.AddListener(() => EndProcess(proc));

            GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btRt = btnTextObj.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;

            TextMeshProUGUI btTmp = btnTextObj.GetComponent<TextMeshProUGUI>();
            btTmp.text = "END TASK";
            btTmp.fontSize = 14;
            btTmp.fontStyle = FontStyles.Bold;
            btTmp.alignment = TextAlignmentOptions.Center;
            btTmp.color = Color.white;
            btTmp.raycastTarget = false;
        }

        private void AddColText(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, float leftPad, FontStyles style, Color color)
        {
            GameObject col = new GameObject("ColText", typeof(RectTransform), typeof(TextMeshProUGUI));
            col.transform.SetParent(parent, false);
            RectTransform rt = col.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(leftPad, 0);
            rt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = col.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void EndProcess(ProcessItemData proc)
        {
            if (proc.RowObj != null)
            {
                Destroy(proc.RowObj);
            }
            _processes.Remove(proc);
            UpdateMetrics();
        }

        private void UpdateMetrics()
        {
            float cpu = UnityEngine.Random.Range(18f, 26f);
            float gpu = UnityEngine.Random.Range(28f, 38f);
            float ram = UnityEngine.Random.Range(36f, 41f);
            float netDown = UnityEngine.Random.Range(512f, 545f);
            float netUp = UnityEngine.Random.Range(24f, 32f);
            float fps = UnityEngine.Random.Range(59.7f, 60.1f);
            int temp = UnityEngine.Random.Range(42, 45);

            if (_cpuPercentText != null) _cpuPercentText.text = $"{cpu:0}%";
            if (_cpuFill != null) _cpuFill.fillAmount = cpu / 100f;

            if (_gpuPercentText != null) _gpuPercentText.text = $"{gpu:0}%";
            if (_gpuFill != null) _gpuFill.fillAmount = gpu / 100f;

            if (_ramPercentText != null) _ramPercentText.text = $"{ram:0}% (6.4 / 16 GB)";
            if (_ramFill != null) _ramFill.fillAmount = ram / 100f;

            if (_netDownText != null) _netDownText.text = $"DN: {netDown:0} MB/s";

            if (_subInfoText != null)
            {
                _subInfoText.text = $"<b>FPS:</b> <color=#00ff88>{fps:0.0}</color>   |   <b>GPU TEMP:</b> <color=#ffaa00>{temp} C</color>   |   <b>NET UP:</b> <color=#00ffff>UP: {netUp:0.1} MB/s</color>   |   <b>PROCESSES:</b> <color=#ffffff>{_processes.Count}</color>";
            }
        }
    }
}
