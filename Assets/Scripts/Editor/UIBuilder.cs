using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.UI;
using VRRemoteDesktop.Settings;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Input;
using VRRemoteDesktop.XR;

namespace VRRemoteDesktop.Editor
{
    public class UIBuilder : EditorWindow
    {
        [MenuItem("VR Remote Desktop/Build Phase 4 UI")]
        public static void BuildUI()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

            Debug.Log("[UIBuilder] Starting Phase 4 Desktop UI Build with pixel-perfect alignment...");

            // 1. Setup / Verify UITheme
            UITheme defaultTheme = AssetDatabase.LoadAssetAtPath<UITheme>("Assets/ScriptableObjects/Themes/DefaultTheme.asset");
            if (defaultTheme == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Themes"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects")) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
                    AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Themes");
                }
                defaultTheme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(defaultTheme, "Assets/ScriptableObjects/Themes/DefaultTheme.asset");
                AssetDatabase.SaveAssets();
            }

            // 2. Setup / Verify ThemeManager (Ensure exactly one in scene)
            ThemeManager themeManager = Object.FindAnyObjectByType<ThemeManager>();
            if (themeManager == null)
            {
                GameObject themeObj = new GameObject("ThemeManager");
                themeManager = themeObj.AddComponent<ThemeManager>();
            }
            SerializedObject tmSo = new SerializedObject(themeManager);
            tmSo.FindProperty("_currentTheme").objectReferenceValue = defaultTheme;
            tmSo.ApplyModifiedProperties();

            // 3. Setup / Verify RenderTexture (1920x1080)
            string rtPath = "Assets/Textures/UI_RenderTexture.renderTexture";
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            if (rt == null)
            {
                rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                rt.name = "UI_RenderTexture";
                AssetDatabase.CreateAsset(rt, rtPath);
                AssetDatabase.SaveAssets();
            }

            // 4. Setup / Verify UICamera (Isolated offscreen at (0, 1000, 0))
            GameObject camObj = GameObject.Find("UICamera");
            if (camObj == null)
            {
                camObj = new GameObject("UICamera");
            }
            camObj.transform.position = new Vector3(0f, 1000f, 0f);
            camObj.transform.rotation = Quaternion.identity;

            Camera uiCam = camObj.GetComponent<Camera>();
            if (uiCam == null) uiCam = camObj.AddComponent<Camera>();
            uiCam.orthographic = false;
            uiCam.fieldOfView = 60f;
            uiCam.nearClipPlane = 0.1f;
            uiCam.farClipPlane = 1000f;
            uiCam.targetTexture = rt;
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = defaultTheme.BackgroundColor;
            uiCam.cullingMask = 1 << LayerMask.NameToLayer("UI");

            // Ensure NO AudioListener on UICamera
            AudioListener al = camObj.GetComponent<AudioListener>();
            if (al != null) DestroyImmediate(al);

            // 5. Clean up old Canvas under UICamera or at root
            GameObject oldCanvasObj = GameObject.Find("DesktopCanvas");
            if (oldCanvasObj != null) DestroyImmediate(oldCanvasObj);

            // 6. Build Master DesktopCanvas
            GameObject canvasObj = new GameObject("DesktopCanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");
            canvasObj.transform.SetParent(camObj.transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCam;
            canvas.planeDistance = 100f;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            DesktopUIManager uiManager = canvasObj.AddComponent<DesktopUIManager>();

            // --- A. BACKGROUND & ACCENTS ---
            GameObject bgObj = CreateUI("Background", canvasObj.transform);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = defaultTheme.BackgroundColor;
            StretchFull(bgObj.GetComponent<RectTransform>());

            // Background Subtle Corner Framing Accents
            CreateCornerFrame(bgObj.transform, defaultTheme.PrimaryColor, defaultTheme.SecondaryColor);

            // --- B. TOP BAR (Height: 52px, perfectly aligned left, center, and right sections) ---
            GameObject topBar = CreateUI("TopBar", canvasObj.transform);
            Image topBarImg = topBar.AddComponent<Image>();
            topBarImg.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
            RectTransform topRt = topBar.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.anchoredPosition = Vector2.zero;
            topRt.sizeDelta = new Vector2(0f, 52f);

            // Slim cyan bottom accent strip on TopBar
            GameObject topBarTrim = CreateUI("TopBar_Trim", topBar.transform);
            Image trimImg = topBarTrim.AddComponent<Image>();
            trimImg.color = new Color(0f, 1f, 1f, 0.4f); // Cyan line
            RectTransform trimRt = topBarTrim.GetComponent<RectTransform>();
            trimRt.anchorMin = new Vector2(0f, 0f);
            trimRt.anchorMax = new Vector2(1f, 0f);
            trimRt.pivot = new Vector2(0.5f, 0f);
            trimRt.anchoredPosition = Vector2.zero;
            trimRt.sizeDelta = new Vector2(0f, 2f);

            // B.1 TopBar Left Section (Station Pill, Host Name, Network, Resolution)
            GameObject leftSection = CreateUI("LeftSection", topBar.transform);
            RectTransform leftRt = leftSection.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0f, 0f);
            leftRt.anchorMax = new Vector2(0f, 1f);
            leftRt.pivot = new Vector2(0f, 0.5f);
            leftRt.anchoredPosition = new Vector2(24f, 0f);
            leftRt.sizeDelta = new Vector2(620f, 0f);

            HorizontalLayoutGroup leftLayout = leftSection.AddComponent<HorizontalLayoutGroup>();
            leftLayout.spacing = 14f;
            leftLayout.childAlignment = TextAnchor.MiddleLeft;
            leftLayout.childControlWidth = false;
            leftLayout.childControlHeight = false;

            // Station Pill Badge
            GameObject pillObj = CreateUI("StationBadge", leftSection.transform);
            Image pillImg = pillObj.AddComponent<Image>();
            pillImg.color = new Color(0f, 0.6f, 0.8f, 0.2f);
            RectTransform pillRt = pillObj.GetComponent<RectTransform>();
            pillRt.sizeDelta = new Vector2(110f, 32f);
            TextMeshProUGUI pillText = CreateLabel(pillObj.transform, "StationText", "STATION-01", 13, TextAlignmentOptions.Center, defaultTheme.PrimaryColor, true);
            StretchFull(pillText.GetComponent<RectTransform>());

            // Computer Name
            TextMeshProUGUI compNameText = CreateLabel(leftSection.transform, "ComputerNameText", "DESKTOP-VR-HOST", 16, TextAlignmentOptions.Left, Color.white, true);
            compNameText.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 32f);

            // Divider Line 1
            CreateDivider(leftSection.transform);

            // Network Status
            TextMeshProUGUI networkText = CreateLabel(leftSection.transform, "NetworkStatusText", "LAN | 10 Gbps", 14, TextAlignmentOptions.Left, new Color(0.35f, 0.85f, 1f));
            networkText.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 32f);

            // Resolution Badge
            TextMeshProUGUI resolutionText = CreateLabel(leftSection.transform, "ResolutionText", "1920x1080 @ 60Hz", 13, TextAlignmentOptions.Left, new Color(0.65f, 0.72f, 0.85f));
            resolutionText.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 32f);

            // B.2 TopBar Center Title
            GameObject centerSection = CreateUI("CenterSection", topBar.transform);
            RectTransform centerRt = centerSection.GetComponent<RectTransform>();
            centerRt.anchorMin = new Vector2(0.5f, 0f);
            centerRt.anchorMax = new Vector2(0.5f, 1f);
            centerRt.pivot = new Vector2(0.5f, 0.5f);
            centerRt.anchoredPosition = Vector2.zero;
            centerRt.sizeDelta = new Vector2(340f, 0f);

            TextMeshProUGUI titleText = CreateLabel(centerSection.transform, "BrandTitle", "VR REMOTE DESKTOP OS", 16, TextAlignmentOptions.Center, new Color(0.78f, 0.84f, 0.95f), true);
            titleText.characterSpacing = 3f;
            StretchFull(titleText.GetComponent<RectTransform>());

            // B.3 TopBar Right Section (Latency, Status, Clock, Date, Settings)
            GameObject rightSection = CreateUI("RightSection", topBar.transform);
            RectTransform rightRt = rightSection.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1f, 0f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.pivot = new Vector2(1f, 0.5f);
            rightRt.anchoredPosition = new Vector2(-24f, 0f);
            rightRt.sizeDelta = new Vector2(620f, 0f);

            HorizontalLayoutGroup rightLayout = rightSection.AddComponent<HorizontalLayoutGroup>();
            rightLayout.spacing = 14f;
            rightLayout.childAlignment = TextAnchor.MiddleRight;
            rightLayout.childControlWidth = false;
            rightLayout.childControlHeight = false;

            // Latency
            TextMeshProUGUI latencyText = CreateLabel(rightSection.transform, "LatencyText", "RTT: 22 ms", 14, TextAlignmentOptions.Right, defaultTheme.SuccessColor);
            latencyText.GetComponent<RectTransform>().sizeDelta = new Vector2(90f, 32f);

            // Connection Badge
            GameObject connBadge = CreateUI("ConnectionBadge", rightSection.transform);
            Image connBg = connBadge.AddComponent<Image>();
            connBg.color = new Color(0.1f, 0.4f, 0.2f, 0.35f);
            RectTransform connRt = connBadge.GetComponent<RectTransform>();
            connRt.sizeDelta = new Vector2(200f, 30f);
            TextMeshProUGUI connectionText = CreateLabel(connBadge.transform, "ConnectionText", "CONNECTED", 13, TextAlignmentOptions.Center, defaultTheme.SuccessColor, true);
            StretchFull(connectionText.GetComponent<RectTransform>());

            // Divider Line 2
            CreateDivider(rightSection.transform);

            // Clock
            TextMeshProUGUI timeText = CreateLabel(rightSection.transform, "TimeText", "12:00:00", 17, TextAlignmentOptions.Right, Color.white, true);
            timeText.GetComponent<RectTransform>().sizeDelta = new Vector2(95f, 32f);

            // Date
            TextMeshProUGUI dateText = CreateLabel(rightSection.transform, "DateText", "FRI, OCT 02", 13, TextAlignmentOptions.Right, new Color(0.65f, 0.72f, 0.85f));
            dateText.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 32f);

            // Settings Button
            GameObject settingsBtnObj = CreateUI("SettingsButton", rightSection.transform);
            Image settingsBtnBg = settingsBtnObj.AddComponent<Image>();
            settingsBtnBg.color = new Color(0.12f, 0.16f, 0.26f, 0.9f);
            Button settingsBtn = settingsBtnObj.AddComponent<Button>();
            RectTransform sBtnRt = settingsBtnObj.GetComponent<RectTransform>();
            sBtnRt.sizeDelta = new Vector2(95f, 30f);
            TextMeshProUGUI sBtnText = CreateLabel(settingsBtnObj.transform, "Label", "SETTINGS", 12, TextAlignmentOptions.Center, Color.white, true);
            StretchFull(sBtnText.GetComponent<RectTransform>());

            // --- C. DESKTOP APPLICATION GRID (Left Column, perfectly aligned) ---
            GameObject appGrid = CreateUI("DesktopGrid", canvasObj.transform);
            RectTransform gridRt = appGrid.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0f, 1f);
            gridRt.anchorMax = new Vector2(0f, 1f);
            gridRt.pivot = new Vector2(0f, 1f);
            gridRt.anchoredPosition = new Vector2(40f, -80f);
            gridRt.sizeDelta = new Vector2(140f, 920f);

            VerticalLayoutGroup gridLayout = appGrid.AddComponent<VerticalLayoutGroup>();
            gridLayout.spacing = 14f;
            gridLayout.padding = new RectOffset(0, 0, 0, 0);
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.childControlWidth = false;
            gridLayout.childControlHeight = false;

            // Build Core Applications
            CreateAppIcon(appGrid.transform, "Terminal", "v4.2 Bash", ">_", defaultTheme.PrimaryColor, new Color(0.04f, 0.12f, 0.16f));
            CreateAppIcon(appGrid.transform, "Notepad", "Cyber Notes", "NOTE", new Color(0.85f, 0.35f, 1f), new Color(0.12f, 0.05f, 0.18f));
            CreateAppIcon(appGrid.transform, "Files", "1.8 TB SSD", "FILES", defaultTheme.SecondaryColor, new Color(0.12f, 0.05f, 0.18f));
            CreateAppIcon(appGrid.transform, "Browser", "Chromium VR", "WEB", defaultTheme.PrimaryColor, new Color(0.04f, 0.12f, 0.16f));
            CreateAppIcon(appGrid.transform, "Sys Monitor", "CPU | GPU | RAM", "MON", defaultTheme.AccentColor, new Color(0.16f, 0.04f, 0.10f));
            CreateAppIcon(appGrid.transform, "Remote Link", "H.265 Ultra", "HOST", defaultTheme.SuccessColor, new Color(0.04f, 0.14f, 0.08f));
            CreateAppIcon(appGrid.transform, "Keyboard", "On-Screen Keys", "KEYS", defaultTheme.WarningColor, new Color(0.16f, 0.10f, 0.03f));

            // --- D. DESKTOP TELEMETRY CARDS (Right Side, perfectly aligned) ---
            // D.1 System Performance Telemetry Card
            GameObject metricsCard = CreateUI("SystemMetricsCard", canvasObj.transform);
            Image metricsCardBg = metricsCard.AddComponent<Image>();
            metricsCardBg.color = new Color(0.06f, 0.09f, 0.15f, 0.9f);
            RectTransform mCardRt = metricsCard.GetComponent<RectTransform>();
            mCardRt.anchorMin = new Vector2(1f, 1f);
            mCardRt.anchorMax = new Vector2(1f, 1f);
            mCardRt.pivot = new Vector2(1f, 1f);
            mCardRt.anchoredPosition = new Vector2(-40f, -80f);
            mCardRt.sizeDelta = new Vector2(360f, 260f);

            // Card Header
            GameObject mHeader = CreateUI("Header", metricsCard.transform);
            RectTransform mhRt = mHeader.GetComponent<RectTransform>();
            mhRt.anchorMin = new Vector2(0f, 1f);
            mhRt.anchorMax = new Vector2(1f, 1f);
            mhRt.pivot = new Vector2(0.5f, 1f);
            mhRt.anchoredPosition = new Vector2(0f, -12f);
            mhRt.sizeDelta = new Vector2(-28f, 26f);
            TextMeshProUGUI mhText = CreateLabel(mHeader.transform, "Title", "SYSTEM TELEMETRY", 15, TextAlignmentOptions.Left, defaultTheme.PrimaryColor, true);
            StretchFull(mhText.GetComponent<RectTransform>());

            // CPU Metric
            TextMeshProUGUI cpuValText;
            Image cpuFill;
            CreateMetricBar(metricsCard.transform, "CPU_Metric", "CPU USAGE", "18%", -46f, defaultTheme.PrimaryColor, out cpuValText, out cpuFill);

            // RAM Metric
            TextMeshProUGUI ramValText;
            Image ramFill;
            CreateMetricBar(metricsCard.transform, "RAM_Metric", "MEMORY (RAM)", "5.8 / 16.0 GB (36%)", -98f, defaultTheme.SecondaryColor, out ramValText, out ramFill);

            // GPU Metric
            TextMeshProUGUI gpuValText;
            Image gpuFill;
            CreateMetricBar(metricsCard.transform, "GPU_Metric", "GPU COMPUTE", "27%", -150f, defaultTheme.AccentColor, out gpuValText, out gpuFill);

            // Network I/O throughput
            GameObject netIoObj = CreateUI("NetworkIO", metricsCard.transform);
            RectTransform nioRt = netIoObj.GetComponent<RectTransform>();
            nioRt.anchorMin = new Vector2(0f, 0f);
            nioRt.anchorMax = new Vector2(1f, 0f);
            nioRt.pivot = new Vector2(0.5f, 0f);
            nioRt.anchoredPosition = new Vector2(0f, 12f);
            nioRt.sizeDelta = new Vector2(-28f, 22f);
            TextMeshProUGUI netIoText = CreateLabel(netIoObj.transform, "Label", "NETWORK I/O:  UP: 1.8 MB/s   DN: 54.2 MB/s", 13, TextAlignmentOptions.Center, new Color(0.68f, 0.76f, 0.90f), true);
            StretchFull(netIoText.GetComponent<RectTransform>());

            // D.2 Remote Link Info Card
            GameObject hostCard = CreateUI("HostInfoCard", canvasObj.transform);
            Image hostCardBg = hostCard.AddComponent<Image>();
            hostCardBg.color = new Color(0.06f, 0.09f, 0.15f, 0.9f);
            RectTransform hCardRt = hostCard.GetComponent<RectTransform>();
            hCardRt.anchorMin = new Vector2(1f, 1f);
            hCardRt.anchorMax = new Vector2(1f, 1f);
            hCardRt.pivot = new Vector2(1f, 1f);
            hCardRt.anchoredPosition = new Vector2(-40f, -360f);
            hCardRt.sizeDelta = new Vector2(360f, 200f);

            // Host Card Content
            GameObject hhObj = CreateUI("HostHeader", hostCard.transform);
            RectTransform hhRt = hhObj.GetComponent<RectTransform>();
            hhRt.anchorMin = new Vector2(0f, 1f);
            hhRt.anchorMax = new Vector2(1f, 1f);
            hhRt.pivot = new Vector2(0.5f, 1f);
            hhRt.anchoredPosition = new Vector2(0f, -12f);
            hhRt.sizeDelta = new Vector2(-28f, 26f);
            TextMeshProUGUI hhText = CreateLabel(hhObj.transform, "Title", "REMOTE LINK PROFILE", 15, TextAlignmentOptions.Left, defaultTheme.SecondaryColor, true);
            StretchFull(hhText.GetComponent<RectTransform>());

            string[] hostDetails = {
                "Target Host: 192.168.1.105:8080",
                "Protocol: Low Latency WebRTC / H.265",
                "Audio Stream: 48 kHz Stereo 24-bit",
                "Encryption: TLS 1.3 / AES-256 GCM",
                "Render Sync: VSync 60 FPS (0 Frame Drop)"
            };
            for (int i = 0; i < hostDetails.Length; i++)
            {
                GameObject detailObj = CreateUI($"Detail_{i}", hostCard.transform);
                RectTransform dRt = detailObj.GetComponent<RectTransform>();
                dRt.anchorMin = new Vector2(0f, 1f);
                dRt.anchorMax = new Vector2(1f, 1f);
                dRt.pivot = new Vector2(0.5f, 1f);
                dRt.anchoredPosition = new Vector2(0f, -42f - (i * 26f));
                dRt.sizeDelta = new Vector2(-28f, 24f);
                TextMeshProUGUI dText = CreateLabel(detailObj.transform, "Text", hostDetails[i], 13, TextAlignmentOptions.Left, new Color(0.72f, 0.80f, 0.92f));
                StretchFull(dText.GetComponent<RectTransform>());
            }

            // --- E. BOTTOM FLOATING DOCK (Width: 640px, Height: 80px, perfectly centered at y = 24px) ---
            GameObject dock = CreateUI("BottomDock", canvasObj.transform);
            Image dockImg = dock.AddComponent<Image>();
            dockImg.color = new Color(0.05f, 0.08f, 0.14f, 0.94f);
            RectTransform dockRt = dock.GetComponent<RectTransform>();
            dockRt.anchorMin = new Vector2(0.5f, 0f);
            dockRt.anchorMax = new Vector2(0.5f, 0f);
            dockRt.pivot = new Vector2(0.5f, 0f);
            dockRt.anchoredPosition = new Vector2(0f, 24f);
            dockRt.sizeDelta = new Vector2(640f, 80f);

            // Glowing top rim on Dock
            GameObject dockTrim = CreateUI("Dock_Rim", dock.transform);
            Image dockTrimImg = dockTrim.AddComponent<Image>();
            dockTrimImg.color = new Color(0f, 1f, 1f, 0.5f); // Cyan glow line
            RectTransform dTrimRt = dockTrim.GetComponent<RectTransform>();
            dTrimRt.anchorMin = new Vector2(0f, 1f);
            dTrimRt.anchorMax = new Vector2(1f, 1f);
            dTrimRt.pivot = new Vector2(0.5f, 1f);
            dTrimRt.anchoredPosition = Vector2.zero;
            dTrimRt.sizeDelta = new Vector2(0f, 2f);

            HorizontalLayoutGroup dockLayout = dock.AddComponent<HorizontalLayoutGroup>();
            dockLayout.spacing = 20f;
            dockLayout.padding = new RectOffset(20, 20, 8, 8);
            dockLayout.childAlignment = TextAnchor.MiddleCenter;
            dockLayout.childControlWidth = false;
            dockLayout.childControlHeight = false;

            // Dock Apps
            string[] dockApps = { "Home", "Files", "Notepad", "Browser", "Terminal", "Settings", "Connection" };
            Color[] dockColors = {
                defaultTheme.PrimaryColor,
                defaultTheme.SecondaryColor,
                new Color(0.85f, 0.35f, 1f),
                defaultTheme.PrimaryColor,
                defaultTheme.SuccessColor,
                defaultTheme.WarningColor,
                defaultTheme.AccentColor
            };

            for (int i = 0; i < dockApps.Length; i++)
            {
                CreateDockApp(dock.transform, dockApps[i], dockColors[i], i == 0); // Home active by default
            }

            // --- F. HIGH-TECH DESKTOP CURSOR ---
            GameObject cursorObj = CreateUI("VRCursor", canvasObj.transform);
            Image cursorImg = cursorObj.AddComponent<Image>();
            cursorImg.color = defaultTheme.PrimaryColor;
            RectTransform cursorRt = cursorObj.GetComponent<RectTransform>();
            cursorRt.sizeDelta = new Vector2(28f, 28f);
            cursorRt.anchorMin = new Vector2(0.5f, 0.5f);
            cursorRt.anchorMax = new Vector2(0.5f, 0.5f);
            cursorRt.pivot = new Vector2(0.5f, 0.5f);
            cursorRt.anchoredPosition = Vector2.zero;

            // Cursor Center Dot
            GameObject dotObj = CreateUI("CenterDot", cursorObj.transform);
            Image dotImg = dotObj.AddComponent<Image>();
            dotImg.color = Color.white;
            RectTransform dotRt = dotObj.GetComponent<RectTransform>();
            dotRt.sizeDelta = new Vector2(8f, 8f);
            dotRt.anchorMin = new Vector2(0.5f, 0.5f);
            dotRt.anchorMax = new Vector2(0.5f, 0.5f);
            dotRt.pivot = new Vector2(0.5f, 0.5f);
            dotRt.anchoredPosition = Vector2.zero;

            VRDesktopCursor cursorComponent = cursorObj.AddComponent<VRDesktopCursor>();
            SerializedObject cursorSo = new SerializedObject(cursorComponent);
            cursorSo.FindProperty("_canvasRect").objectReferenceValue = canvasObj.GetComponent<RectTransform>();
            cursorSo.FindProperty("_cursorImage").objectReferenceValue = cursorImg;
            cursorSo.ApplyModifiedProperties();

            // Link cursor to XR Interactable if present
            VRDesktopInteractable interactable = Object.FindAnyObjectByType<VRDesktopInteractable>();
            if (interactable != null)
            {
                SerializedObject intSo = new SerializedObject(interactable);
                intSo.FindProperty("_cursor").objectReferenceValue = cursorComponent;
                intSo.ApplyModifiedProperties();
            }

            // --- G. LINK DESKTOP UI MANAGER SERIALIZED REFERENCES ---
            SerializedObject uiSo = new SerializedObject(uiManager);
            uiSo.FindProperty("_timeText").objectReferenceValue = timeText;
            uiSo.FindProperty("_dateText").objectReferenceValue = dateText;
            uiSo.FindProperty("_connectionText").objectReferenceValue = connectionText;
            uiSo.FindProperty("_latencyText").objectReferenceValue = latencyText;
            uiSo.FindProperty("_computerNameText").objectReferenceValue = compNameText;
            uiSo.FindProperty("_networkStatusText").objectReferenceValue = networkText;
            uiSo.FindProperty("_resolutionText").objectReferenceValue = resolutionText;
            uiSo.FindProperty("_settingsButton").objectReferenceValue = settingsBtn;

            uiSo.FindProperty("_cpuText").objectReferenceValue = cpuValText;
            uiSo.FindProperty("_cpuBarFill").objectReferenceValue = cpuFill;
            uiSo.FindProperty("_ramText").objectReferenceValue = ramValText;
            uiSo.FindProperty("_ramBarFill").objectReferenceValue = ramFill;
            uiSo.FindProperty("_gpuText").objectReferenceValue = gpuValText;
            uiSo.FindProperty("_gpuBarFill").objectReferenceValue = gpuFill;
            uiSo.FindProperty("_networkIOText").objectReferenceValue = netIoText;

            uiSo.FindProperty("_background").objectReferenceValue = bgImg;
            uiSo.FindProperty("_topBarBackground").objectReferenceValue = topBarImg;
            uiSo.FindProperty("_dockBackground").objectReferenceValue = dockImg;
            uiSo.FindProperty("_telemetryWidgetBg").objectReferenceValue = metricsCardBg;
            uiSo.FindProperty("_hostInfoWidgetBg").objectReferenceValue = hostCardBg;
            uiSo.ApplyModifiedProperties();

            // --- H. CONNECT TO MONITOR SCREEN ---
            RemoteDesktopScreen screen = Object.FindAnyObjectByType<RemoteDesktopScreen>();
            if (screen != null)
            {
                SerializedObject screenSo = new SerializedObject(screen);
                screenSo.FindProperty("_defaultTexture").objectReferenceValue = rt;
                screenSo.ApplyModifiedProperties();
                screen.SetTexture(rt);
                Debug.Log("[UIBuilder] Connected UI_RenderTexture to RemoteDesktopScreen successfully!");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            Debug.Log("[UIBuilder] Phase 4 Desktop UI constructed, pixel-aligned, and saved successfully!");
        }

        private static GameObject CreateUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Color color, bool bold = false)
        {
            GameObject go = CreateUI(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        private static void CreateDivider(Transform parent)
        {
            GameObject div = CreateUI("Divider", parent);
            Image img = div.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.2f);
            RectTransform rt = div.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1f, 22f);
        }

        private static void CreateCornerFrame(Transform parent, Color cyan, Color violet)
        {
            // Subtle framing accent line on canvas edge
            GameObject frameObj = CreateUI("FramingBorders", parent);
            StretchFull(frameObj.GetComponent<RectTransform>());

            GameObject topAccent = CreateUI("BorderTop", frameObj.transform);
            Image topImg = topAccent.AddComponent<Image>();
            topImg.color = new Color(cyan.r, cyan.g, cyan.b, 0.25f);
            RectTransform tRt = topAccent.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = Vector2.zero;
            tRt.sizeDelta = new Vector2(0f, 1.5f);

            GameObject botAccent = CreateUI("BorderBottom", frameObj.transform);
            Image botImg = botAccent.AddComponent<Image>();
            botImg.color = new Color(violet.r, violet.g, violet.b, 0.25f);
            RectTransform bRt = botAccent.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(1f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = Vector2.zero;
            bRt.sizeDelta = new Vector2(0f, 1.5f);
        }

        private static void CreateAppIcon(Transform parent, string title, string sub, string symbol, Color accentColor, Color boxBg)
        {
            GameObject item = CreateUI($"Icon_{title}", parent);
            RectTransform itemRt = item.GetComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(130f, 114f);

            Button btn = item.AddComponent<Button>();
            DesktopIcon dIcon = item.AddComponent<DesktopIcon>();

            // Icon Box
            GameObject box = CreateUI("IconBox", item.transform);
            Image boxImg = box.AddComponent<Image>();
            boxImg.color = boxBg;
            RectTransform boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 1f);
            boxRt.anchorMax = new Vector2(0.5f, 1f);
            boxRt.pivot = new Vector2(0.5f, 1f);
            boxRt.anchoredPosition = new Vector2(0f, 0f);
            boxRt.sizeDelta = new Vector2(66f, 66f);

            // Border Line
            GameObject border = CreateUI("Border", box.transform);
            Image borderImg = border.AddComponent<Image>();
            borderImg.color = accentColor;
            RectTransform borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = new Vector2(0f, 0f);
            borderRt.anchorMax = new Vector2(1f, 0f);
            borderRt.pivot = new Vector2(0.5f, 0f);
            borderRt.anchoredPosition = Vector2.zero;
            borderRt.sizeDelta = new Vector2(0f, 2.5f);

            // Symbol
            TextMeshProUGUI symText = CreateLabel(box.transform, "Symbol", symbol, 18, TextAlignmentOptions.Center, accentColor, true);
            StretchFull(symText.GetComponent<RectTransform>());

            // Title Label
            TextMeshProUGUI titleLabel = CreateLabel(item.transform, "Title", title, 15, TextAlignmentOptions.Center, Color.white, true);
            RectTransform tRt = titleLabel.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 0f);
            tRt.pivot = new Vector2(0.5f, 0f);
            tRt.anchoredPosition = new Vector2(0f, 22f);
            tRt.sizeDelta = new Vector2(0f, 22f);

            // Subtitle Label
            TextMeshProUGUI subLabel = CreateLabel(item.transform, "Sub", sub, 12, TextAlignmentOptions.Center, new Color(0.6f, 0.68f, 0.82f));
            RectTransform sRt = subLabel.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 0f);
            sRt.pivot = new Vector2(0.5f, 0f);
            sRt.anchoredPosition = new Vector2(0f, 3f);
            sRt.sizeDelta = new Vector2(0f, 18f);

            // Link DesktopIcon component fields
            SerializedObject diSo = new SerializedObject(dIcon);
            diSo.FindProperty("_appName").stringValue = title;
            diSo.FindProperty("_appDescription").stringValue = sub;
            diSo.FindProperty("_iconBackground").objectReferenceValue = boxImg;
            diSo.FindProperty("_iconBorder").objectReferenceValue = borderImg;
            diSo.FindProperty("_titleText").objectReferenceValue = titleLabel;
            diSo.FindProperty("_subText").objectReferenceValue = subLabel;
            diSo.FindProperty("_highlightColor").colorValue = accentColor;
            diSo.ApplyModifiedProperties();
        }

        private static void CreateMetricBar(Transform parent, string name, string label, string defaultVal, float yPos, Color fillColor, out TextMeshProUGUI valText, out Image fillImg)
        {
            GameObject container = CreateUI(name, parent);
            RectTransform cRt = container.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 1f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 1f);
            cRt.anchoredPosition = new Vector2(0f, yPos);
            cRt.sizeDelta = new Vector2(-28f, 46f);

            // Label
            CreateLabel(container.transform, "Label", label, 13, TextAlignmentOptions.Left, new Color(0.75f, 0.82f, 0.95f), true);

            // Value text
            valText = CreateLabel(container.transform, "Value", defaultVal, 14, TextAlignmentOptions.Right, fillColor, true);
            RectTransform vtRt = valText.GetComponent<RectTransform>();
            vtRt.anchorMin = new Vector2(0.5f, 0.5f);
            vtRt.anchorMax = new Vector2(1f, 1f);
            vtRt.pivot = new Vector2(1f, 1f);
            vtRt.anchoredPosition = Vector2.zero;
            vtRt.sizeDelta = new Vector2(0f, 20f);

            // Bar Track
            GameObject track = CreateUI("Track", container.transform);
            Image trackImg = track.AddComponent<Image>();
            trackImg.color = new Color(0.1f, 0.15f, 0.24f, 0.8f);
            RectTransform tRt = track.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 0f);
            tRt.pivot = new Vector2(0.5f, 0f);
            tRt.anchoredPosition = Vector2.zero;
            tRt.sizeDelta = new Vector2(0f, 8f);

            // Bar Fill
            GameObject fill = CreateUI("Fill", track.transform);
            fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.35f;
            StretchFull(fill.GetComponent<RectTransform>());
        }

        private static void CreateDockApp(Transform parent, string name, Color accentColor, bool isActive)
        {
            GameObject dockItem = CreateUI($"Dock_{name}", parent);
            RectTransform itemRt = dockItem.GetComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(74f, 64f);

            Button btn = dockItem.AddComponent<Button>();
            DesktopIcon iconComp = dockItem.AddComponent<DesktopIcon>();

            // Icon Background
            GameObject bg = CreateUI("Bg", dockItem.transform);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.12f, 0.20f, 0.9f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = new Vector2(0f, 4f);
            bgRt.sizeDelta = new Vector2(52f, 52f);

            // Border Line
            GameObject rim = CreateUI("Rim", bg.transform);
            Image rimImg = rim.AddComponent<Image>();
            rimImg.color = accentColor;
            RectTransform rimRt = rim.GetComponent<RectTransform>();
            rimRt.anchorMin = new Vector2(0f, 0f);
            rimRt.anchorMax = new Vector2(1f, 0f);
            rimRt.pivot = new Vector2(0.5f, 0f);
            rimRt.anchoredPosition = Vector2.zero;
            rimRt.sizeDelta = new Vector2(0f, 2f);

            // App Label in Icon
            TextMeshProUGUI label = CreateLabel(bg.transform, "Label", name.ToUpper(), 9, TextAlignmentOptions.Center, Color.white, true);
            StretchFull(label.GetComponent<RectTransform>());

            // Active Dot Indicator
            GameObject dot = CreateUI("ActiveDot", dockItem.transform);
            Image dotImg = dot.AddComponent<Image>();
            dotImg.color = accentColor;
            RectTransform dotRt = dot.GetComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0.5f, 0f);
            dotRt.anchorMax = new Vector2(0.5f, 0f);
            dotRt.pivot = new Vector2(0.5f, 0f);
            dotRt.anchoredPosition = new Vector2(0f, -2f);
            dotRt.sizeDelta = new Vector2(5f, 5f);
            dot.SetActive(isActive);

            // Setup DesktopIcon
            SerializedObject diSo = new SerializedObject(iconComp);
            diSo.FindProperty("_appName").stringValue = name;
            diSo.FindProperty("_iconBackground").objectReferenceValue = bgImg;
            diSo.FindProperty("_iconBorder").objectReferenceValue = rimImg;
            diSo.FindProperty("_activeIndicator").objectReferenceValue = dot;
            diSo.FindProperty("_highlightColor").colorValue = accentColor;
            diSo.ApplyModifiedProperties();
        }
    }
}
