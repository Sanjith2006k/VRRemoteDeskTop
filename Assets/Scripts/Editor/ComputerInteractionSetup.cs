using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.InputSystem;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Player;
using VRRemoteDesktop.UI;

namespace VRRemoteDesktop.Editor
{
    [InitializeOnLoad]
    public class ComputerInteractionSetup : EditorWindow
    {
        private const string PrefKey = "VRRemoteDesktop_Phase5_SetupDone";

        static ComputerInteractionSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(PrefKey, false))
                {
                    SessionState.SetBool(PrefKey, true);
                    SetupPhase5();
                }
            };
        }

        [MenuItem("VR Remote Desktop/Setup Phase 5 Computer Interaction")]
        public static void SetupPhase5()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Main")
            {
                if (File.Exists("Assets/Scenes/Main.unity"))
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                }
            }

            Debug.Log("[ComputerInteractionSetup] Setting up Phase 5 Computer Interaction Mode...");

            // 1. Locate Workstation and Screen
            GameObject workstation = GameObject.Find("Workstation");
            if (workstation == null)
            {
                workstation = new GameObject("Workstation");
            }

            Transform monitorTransform = null;
            GameObject screenObj = GameObject.Find("Screen");
            if (screenObj != null)
            {
                monitorTransform = screenObj.transform;
            }
            else
            {
                GameObject vm = GameObject.Find("VirtualMonitor");
                if (vm != null) monitorTransform = vm.transform;
            }

            // 2. Setup SeatedFocusPoint under Workstation
            Transform seatedFocus = workstation.transform.Find("SeatedFocusPoint");
            if (seatedFocus == null)
            {
                GameObject focusObj = new GameObject("SeatedFocusPoint");
                focusObj.transform.SetParent(workstation.transform);
                // Center seated directly in the chair in front of monitors at comfortable viewing distance
                focusObj.transform.localPosition = new Vector3(0f, 1.18f, 0.40f);
                focusObj.transform.localRotation = Quaternion.identity;
                seatedFocus = focusObj.transform;
            }

            // 3. Build / Verify InteractionPromptCanvas
            InteractionPromptUI promptUI = BuildInteractionPromptCanvas(workstation.transform);

            // 4. Configure ComputerInteractionManager
            ComputerInteractionManager interactionManager = workstation.GetComponent<ComputerInteractionManager>();
            if (interactionManager == null)
            {
                interactionManager = workstation.AddComponent<ComputerInteractionManager>();
            }

            // Find Player components
            PlayerController playerCtrl = Object.FindAnyObjectByType<PlayerController>();
            MouseLook mouseLook = Object.FindAnyObjectByType<MouseLook>();
            Camera mainCam = Camera.main;
            Transform camOffset = null;
            if (mainCam != null && mainCam.transform.parent != null)
            {
                camOffset = mainCam.transform.parent;
            }

            // Load Input Actions
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            InputActionReference interactRef = null;
            InputActionReference cancelRef = null;

            if (actions != null)
            {
                var interactAction = actions.FindAction("Player/Interact");
                if (interactAction != null)
                {
                    interactRef = ScriptableObject.CreateInstance<InputActionReference>();
                    interactRef.Set(interactAction);
                }

                var cancelAction = actions.FindAction("Player/Cancel");
                if (cancelAction != null)
                {
                    cancelRef = ScriptableObject.CreateInstance<InputActionReference>();
                    cancelRef.Set(cancelAction);
                }
            }

            // Serialize properties on ComputerInteractionManager
            SerializedObject so = new SerializedObject(interactionManager);
            if (monitorTransform != null) so.FindProperty("_monitorTransform").objectReferenceValue = monitorTransform;
            so.FindProperty("_seatedFocusPoint").objectReferenceValue = seatedFocus;
            if (playerCtrl != null) so.FindProperty("_playerController").objectReferenceValue = playerCtrl;
            if (mouseLook != null) so.FindProperty("_mouseLook").objectReferenceValue = mouseLook;
            if (mainCam != null) so.FindProperty("_playerCamera").objectReferenceValue = mainCam;
            if (camOffset != null) so.FindProperty("_cameraOffset").objectReferenceValue = camOffset;
            if (promptUI != null) so.FindProperty("_promptUI").objectReferenceValue = promptUI;
            if (interactRef != null) so.FindProperty("_interactAction").objectReferenceValue = interactRef;
            if (cancelRef != null) so.FindProperty("_cancelAction").objectReferenceValue = cancelRef;
            so.FindProperty("_interactionDistance").floatValue = 2.8f;
            so.FindProperty("_facingAngleThreshold").floatValue = 80.0f;
            so.FindProperty("_transitionDuration").floatValue = 0.6f;
            so.FindProperty("_seatedViewingDistance").floatValue = 0.68f;
            so.FindProperty("_seatedVerticalOffset").floatValue = 0.0f;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[ComputerInteractionSetup] Phase 5 Computer Interaction Mode setup completed and scene saved!");
        }

        private static InteractionPromptUI BuildInteractionPromptCanvas(Transform parent)
        {
            GameObject canvasObj = GameObject.Find("InteractionPromptCanvas");
            if (canvasObj != null)
            {
                DestroyImmediate(canvasObj);
            }

            canvasObj = new GameObject("InteractionPromptCanvas");
            canvasObj.transform.SetParent(parent);
            // Floating elegantly in front of the keyboard/monitor stand facing user
            canvasObj.transform.localPosition = new Vector3(0f, 0.90f, 0.42f);
            canvasObj.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            canvasObj.transform.localScale = Vector3.one * 0.0018f;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(360f, 160f);

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            canvasObj.AddComponent<GraphicRaycaster>();
            InteractionPromptUI promptUI = canvasObj.AddComponent<InteractionPromptUI>();

            // --- 1. Enter Prompt (USE COMPUTER - [E]) ---
            GameObject enterObj = new GameObject("EnterPrompt");
            enterObj.transform.SetParent(canvasObj.transform, false);

            RectTransform enterRt = enterObj.AddComponent<RectTransform>();
            enterRt.anchorMin = new Vector2(0.5f, 0.5f);
            enterRt.anchorMax = new Vector2(0.5f, 0.5f);
            enterRt.pivot = new Vector2(0.5f, 0.5f);
            enterRt.sizeDelta = new Vector2(340f, 76f);
            enterRt.anchoredPosition = new Vector2(0f, -10f);

            CanvasGroup enterGroup = enterObj.AddComponent<CanvasGroup>();
            enterGroup.alpha = 0f;

            // Background Glass Panel
            Image enterBg = enterObj.AddComponent<Image>();
            enterBg.color = new Color(0.04f, 0.06f, 0.09f, 0.88f);

            // Border Glow Trim
            GameObject enterBorder = new GameObject("BorderGlow");
            enterBorder.transform.SetParent(enterObj.transform, false);
            RectTransform borderRt = enterBorder.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(2f, 2f);
            Image borderImg = enterBorder.AddComponent<Image>();
            borderImg.color = new Color(0f, 0.95f, 1.0f, 0.75f); // Cyan Glow

            // Inner dark inset
            GameObject enterInset = new GameObject("InnerInset");
            enterInset.transform.SetParent(enterBorder.transform, false);
            RectTransform insetRt = enterInset.AddComponent<RectTransform>();
            insetRt.anchorMin = Vector2.zero;
            insetRt.anchorMax = Vector2.one;
            insetRt.offsetMin = new Vector2(2f, 2f);
            insetRt.offsetMax = new Vector2(-2f, -2f);
            Image insetImg = enterInset.AddComponent<Image>();
            insetImg.color = new Color(0.05f, 0.07f, 0.11f, 0.95f);

            // Keycap Badge [E]
            GameObject keyBadgeObj = new GameObject("KeyBadge");
            keyBadgeObj.transform.SetParent(enterInset.transform, false);
            RectTransform keyBadgeRt = keyBadgeObj.AddComponent<RectTransform>();
            keyBadgeRt.anchorMin = new Vector2(0f, 0.5f);
            keyBadgeRt.anchorMax = new Vector2(0f, 0.5f);
            keyBadgeRt.pivot = new Vector2(0f, 0.5f);
            keyBadgeRt.anchoredPosition = new Vector2(14f, 0f);
            keyBadgeRt.sizeDelta = new Vector2(50f, 50f);
            Image keyBadgeImg = keyBadgeObj.AddComponent<Image>();
            keyBadgeImg.color = new Color(0f, 0.9f, 1.0f, 1f); // Neon Cyan

            GameObject keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyBadgeObj.transform, false);
            RectTransform keyTextRt = keyTextObj.AddComponent<RectTransform>();
            keyTextRt.anchorMin = Vector2.zero;
            keyTextRt.anchorMax = Vector2.one;
            keyTextRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI keyText = keyTextObj.AddComponent<TextMeshProUGUI>();
            keyText.text = "E";
            keyText.fontSize = 28;
            keyText.fontStyle = FontStyles.Bold;
            keyText.alignment = TextAlignmentOptions.Center;
            keyText.color = new Color(0.04f, 0.06f, 0.09f, 1f); // Charcoal dark on cyan

            // Title Text ("USE COMPUTER")
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(enterInset.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(76f, 11f);
            titleRt.sizeDelta = new Vector2(-86f, 26f);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "USE COMPUTER";
            titleText.fontSize = 18;
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 2.5f;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.color = Color.white;

            // Subtitle Text ("Press [E] to interact")
            GameObject subObj = new GameObject("SubText");
            subObj.transform.SetParent(enterInset.transform, false);
            RectTransform subRt = subObj.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0.5f);
            subRt.anchorMax = new Vector2(1f, 0.5f);
            subRt.pivot = new Vector2(0f, 0.5f);
            subRt.anchoredPosition = new Vector2(76f, -12f);
            subRt.sizeDelta = new Vector2(-86f, 22f);
            TextMeshProUGUI subText = subObj.AddComponent<TextMeshProUGUI>();
            subText.text = "Press E to interact";
            subText.fontSize = 12;
            subText.fontStyle = FontStyles.Normal;
            subText.alignment = TextAlignmentOptions.Left;
            subText.color = new Color(0.55f, 0.90f, 1.0f, 0.85f);

            // --- 2. Exit Prompt (PRESS [ESC] TO EXIT) ---
            GameObject exitObj = new GameObject("ExitPrompt");
            exitObj.transform.SetParent(canvasObj.transform, false);

            RectTransform exitRt = exitObj.AddComponent<RectTransform>();
            exitRt.anchorMin = new Vector2(0.5f, 0.5f);
            exitRt.anchorMax = new Vector2(0.5f, 0.5f);
            exitRt.pivot = new Vector2(0.5f, 0.5f);
            exitRt.sizeDelta = new Vector2(300f, 46f);
            exitRt.anchoredPosition = new Vector2(0f, 65f); // Placed above

            CanvasGroup exitGroup = exitObj.AddComponent<CanvasGroup>();
            exitGroup.alpha = 0f;

            Image exitBg = exitObj.AddComponent<Image>();
            exitBg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

            // Exit Border Glow (Violet/Magenta)
            GameObject exitBorder = new GameObject("ExitBorder");
            exitBorder.transform.SetParent(exitObj.transform, false);
            RectTransform exitBorderRt = exitBorder.AddComponent<RectTransform>();
            exitBorderRt.anchorMin = Vector2.zero;
            exitBorderRt.anchorMax = Vector2.one;
            exitBorderRt.sizeDelta = new Vector2(2f, 2f);
            Image exitBorderImg = exitBorder.AddComponent<Image>();
            exitBorderImg.color = new Color(1.0f, 0.05f, 0.55f, 0.8f);

            GameObject exitInset = new GameObject("ExitInset");
            exitInset.transform.SetParent(exitBorder.transform, false);
            RectTransform exitInsetRt = exitInset.AddComponent<RectTransform>();
            exitInsetRt.anchorMin = Vector2.zero;
            exitInsetRt.anchorMax = Vector2.one;
            exitInsetRt.offsetMin = new Vector2(1.5f, 1.5f);
            exitInsetRt.offsetMax = new Vector2(-1.5f, -1.5f);
            Image exitInsetImg = exitInset.AddComponent<Image>();
            exitInsetImg.color = new Color(0.08f, 0.09f, 0.13f, 0.95f);

            // ESC Badge
            GameObject escBadgeObj = new GameObject("EscBadge");
            escBadgeObj.transform.SetParent(exitInset.transform, false);
            RectTransform escBadgeRt = escBadgeObj.AddComponent<RectTransform>();
            escBadgeRt.anchorMin = new Vector2(0f, 0.5f);
            escBadgeRt.anchorMax = new Vector2(0f, 0.5f);
            escBadgeRt.pivot = new Vector2(0f, 0.5f);
            escBadgeRt.anchoredPosition = new Vector2(10f, 0f);
            escBadgeRt.sizeDelta = new Vector2(56f, 30f);
            Image escBadgeImg = escBadgeObj.AddComponent<Image>();
            escBadgeImg.color = new Color(1.0f, 0.05f, 0.55f, 1f);

            GameObject escTextObj = new GameObject("EscText");
            escTextObj.transform.SetParent(escBadgeObj.transform, false);
            RectTransform escTextRt = escTextObj.AddComponent<RectTransform>();
            escTextRt.anchorMin = Vector2.zero;
            escTextRt.anchorMax = Vector2.one;
            escTextRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI escText = escTextObj.AddComponent<TextMeshProUGUI>();
            escText.text = "ESC";
            escText.fontSize = 14;
            escText.fontStyle = FontStyles.Bold;
            escText.alignment = TextAlignmentOptions.Center;
            escText.color = Color.white;

            // Exit Title Text
            GameObject exitTitleObj = new GameObject("ExitTitle");
            exitTitleObj.transform.SetParent(exitInset.transform, false);
            RectTransform exitTitleRt = exitTitleObj.AddComponent<RectTransform>();
            exitTitleRt.anchorMin = new Vector2(0f, 0.5f);
            exitTitleRt.anchorMax = new Vector2(1f, 0.5f);
            exitTitleRt.pivot = new Vector2(0f, 0.5f);
            exitTitleRt.anchoredPosition = new Vector2(74f, 0f);
            exitTitleRt.sizeDelta = new Vector2(-80f, 26f);
            TextMeshProUGUI exitTitleText = exitTitleObj.AddComponent<TextMeshProUGUI>();
            exitTitleText.text = "EXIT COMPUTER MODE";
            exitTitleText.fontSize = 13;
            exitTitleText.fontStyle = FontStyles.Bold;
            exitTitleText.characterSpacing = 1.5f;
            exitTitleText.alignment = TextAlignmentOptions.Left;
            exitTitleText.color = Color.white;

            // Link references in InteractionPromptUI
            SerializedObject pSo = new SerializedObject(promptUI);
            pSo.FindProperty("_enterPromptGroup").objectReferenceValue = enterGroup;
            pSo.FindProperty("_exitPromptGroup").objectReferenceValue = exitGroup;
            pSo.FindProperty("_enterTitleText").objectReferenceValue = titleText;
            pSo.FindProperty("_enterKeyText").objectReferenceValue = keyText;
            pSo.FindProperty("_enterKeyBadge").objectReferenceValue = keyBadgeImg;
            pSo.FindProperty("_enterBorderGlow").objectReferenceValue = borderImg;
            pSo.FindProperty("_exitTitleText").objectReferenceValue = exitTitleText;
            pSo.FindProperty("_exitKeyText").objectReferenceValue = escText;
            pSo.FindProperty("_enableBillboarding").boolValue = true;
            pSo.FindProperty("_fadeSpeed").floatValue = 8.0f;
            pSo.FindProperty("_enablePulse").boolValue = true;
            pSo.ApplyModifiedProperties();

            return promptUI;
        }
    }
}
