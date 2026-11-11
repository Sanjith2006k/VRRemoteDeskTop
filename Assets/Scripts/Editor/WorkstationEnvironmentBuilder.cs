using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.XR;

namespace VRRemoteDesktop.Editor
{
    /// <summary>
    /// Builds the expanded Phase 3 futuristic room environment and complete workstation hierarchy:
    /// - Expanded Room (16m x 16m x 4.5m) with floor, ceiling, architectural walls & light trims
    /// - Futuristic Desk with dual-tone underglow LED strips and large desk mat
    /// - Mechanical Keyboard with keycaps and backlight glow
    /// - Ergonomic Futuristic Mouse with scroll wheel and accent grooves
    /// - Studio Monitor Speakers (L & R) with glowing driver rings
    /// - Futuristic PC Tower with glass panel and glowing cooling fans
    /// - Auxiliary Status Display with holographic telemetry
    /// - Premium Monitor with slim bezel, stand, and RemoteDesktopScreen integration
    /// - Balanced cinematic lighting setup (monitor glow, accent bounce, ceiling ambient)
    /// </summary>
    [InitializeOnLoad]
    public class WorkstationEnvironmentBuilder : EditorWindow
    {
        private const string PrefKey = "VRRemoteDesktop_Phase3_BuildDone";

        static WorkstationEnvironmentBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(PrefKey, false))
                {
                    SessionState.SetBool(PrefKey, true);
                    BuildPhase3();
                }
            };
        }

        [MenuItem("VR Remote Desktop/Build Phase 3 Workstation & Environment")]
        public static void BuildPhase3()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Main")
            {
                if (File.Exists("Assets/Scenes/Main.unity"))
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                }
            }

            Debug.Log("[WorkstationEnvironmentBuilder] Building Phase 3 Environment & Workstation...");

            CreateMaterials();
            BuildExpandedEnvironment();
            BuildCompleteWorkstation();
            SetupLightingAndPostProcessing();

            // Re-connect screen texture from UI if UI exists
            ConnectScreenToRenderTexture();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[WorkstationEnvironmentBuilder] Phase 3 Workstation & Expanded Environment built and saved successfully!");
        }

        private static void CreateMaterials()
        {
            if (!Directory.Exists("Assets/Materials"))
            {
                Directory.CreateDirectory("Assets/Materials");
            }

            // Base Materials
            CreateURPMaterial("Assets/Materials/Mat_DarkCharcoal.mat", new Color(0.08f, 0.09f, 0.11f), false, Color.black, 0.35f);
            CreateURPMaterial("Assets/Materials/Mat_Floor.mat", new Color(0.04f, 0.045f, 0.06f), false, Color.black, 0.28f);
            CreateURPMaterial("Assets/Materials/Mat_DeskSurface.mat", new Color(0.06f, 0.07f, 0.09f), false, Color.black, 0.75f);
            CreateURPMaterial("Assets/Materials/Mat_DeskMat.mat", new Color(0.03f, 0.035f, 0.045f), false, Color.black, 0.15f);
            CreateURPMaterial("Assets/Materials/Mat_KeyboardBody.mat", new Color(0.05f, 0.055f, 0.07f), false, Color.black, 0.45f);
            CreateURPMaterial("Assets/Materials/Mat_Keycaps.mat", new Color(0.09f, 0.10f, 0.13f), false, Color.black, 0.55f);
            CreateURPMaterial("Assets/Materials/Mat_SpeakerCabinet.mat", new Color(0.05f, 0.06f, 0.075f), false, Color.black, 0.5f);
            CreateURPMaterial("Assets/Materials/Mat_PCTower.mat", new Color(0.04f, 0.045f, 0.055f), false, Color.black, 0.8f);
            CreateURPMaterial("Assets/Materials/Mat_WallTrim.mat", new Color(0.06f, 0.07f, 0.09f), false, Color.black, 0.35f);

            // Emissive Palette (Cyan, Violet, Magenta)
            CreateURPMaterial("Assets/Materials/Mat_EmissiveCyan.mat", Color.black, true, new Color(0.0f, 1.0f, 1.0f) * 3.5f);
            CreateURPMaterial("Assets/Materials/Mat_EmissiveViolet.mat", Color.black, true, new Color(0.55f, 0.1f, 1.0f) * 3.5f);
            CreateURPMaterial("Assets/Materials/Mat_EmissiveMagenta.mat", Color.black, true, new Color(1.0f, 0.05f, 0.55f) * 3.5f);
            CreateURPMaterial("Assets/Materials/Mat_EmissiveWhite.mat", Color.black, true, new Color(0.9f, 0.95f, 1.0f) * 2.0f);

            // Screen Unlit Material
            string screenMatPath = "Assets/Materials/Mat_VirtualScreen.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(screenMatPath) == null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetColor("_BaseColor", Color.white);
                AssetDatabase.CreateAsset(mat, screenMatPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateURPMaterial(string path, Color baseColor, bool isEmissive, Color emissiveColor, float smoothness = 0.5f)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", baseColor);
                mat.SetFloat("_Smoothness", smoothness);

                if (isEmissive)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mat.SetColor("_EmissionColor", emissiveColor);
                }

                AssetDatabase.CreateAsset(mat, path);
            }
        }

        private static void BuildExpandedEnvironment()
        {
            Material matCharcoal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DarkCharcoal.mat");
            Material matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Floor.mat");
            Material matCyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveCyan.mat");
            Material matViolet = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveViolet.mat");
            Material matTrim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_WallTrim.mat");

            // Clean up old environment
            GameObject oldEnv = GameObject.Find("Environment");
            if (oldEnv != null) DestroyImmediate(oldEnv);

            GameObject envParent = new GameObject("Environment");

            // 1. Floor (Expanded to 18m x 18m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(envParent.transform);
            floor.transform.position = new Vector3(0f, -0.05f, 0f);
            floor.transform.localScale = new Vector3(18f, 0.1f, 18f);
            floor.GetComponent<Renderer>().sharedMaterial = matFloor;

            // 2. Ceiling (18m x 18m at y = 4.85m)
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(envParent.transform);
            ceiling.transform.position = new Vector3(0f, 4.85f, 0f);
            ceiling.transform.localScale = new Vector3(18f, 0.1f, 18f);
            ceiling.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Recessed ceiling light strips
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject ceilLight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilLight.name = $"Ceiling_Strip_{(i > 0 ? "R" : "L")}";
                ceilLight.transform.SetParent(ceiling.transform);
                ceilLight.transform.localPosition = new Vector3(i * 0.25f, -0.51f, 0f);
                ceilLight.transform.localScale = new Vector3(0.04f, 0.05f, 0.7f);
                ceilLight.GetComponent<Renderer>().sharedMaterial = matViolet;
                DestroyImmediate(ceilLight.GetComponent<Collider>());
            }

            // 3. Walls
            GameObject wallsParent = new GameObject("Walls");
            wallsParent.transform.SetParent(envParent.transform);

            // Front Wall (z = 9m)
            CreateWall(wallsParent.transform, "Wall_Front", new Vector3(0f, 2.4f, 9f), new Vector3(18f, 4.8f, 0.3f), matCharcoal);
            // Back Wall (z = -9m)
            CreateWall(wallsParent.transform, "Wall_Back", new Vector3(0f, 2.4f, -9f), new Vector3(18f, 4.8f, 0.3f), matCharcoal);
            // Left Wall (x = -9m)
            CreateWall(wallsParent.transform, "Wall_Left", new Vector3(-9f, 2.4f, 0f), new Vector3(0.3f, 4.8f, 18f), matCharcoal);
            // Right Wall (x = 9m)
            CreateWall(wallsParent.transform, "Wall_Right", new Vector3(9f, 2.4f, 0f), new Vector3(0.3f, 4.8f, 18f), matCharcoal);

            // 4. Architectural Emissive Trims & Baseboards
            // Front Wall Accent Trim (Cyan)
            CreateTrim(wallsParent.transform, "Trim_Front_Cyan", new Vector3(0f, 2.4f, 8.84f), new Vector3(17.6f, 0.06f, 0.03f), matCyan);
            CreateTrim(wallsParent.transform, "Trim_Front_Base", new Vector3(0f, 0.1f, 8.84f), new Vector3(17.6f, 0.08f, 0.03f), matViolet);

            // Left Wall Accent Trim (Violet)
            CreateTrim(wallsParent.transform, "Trim_Left_Violet", new Vector3(-8.84f, 2.4f, 0f), new Vector3(0.03f, 0.06f, 17.6f), matViolet);
            CreateTrim(wallsParent.transform, "Trim_Left_Base", new Vector3(-8.84f, 0.1f, 0f), new Vector3(0.03f, 0.08f, 17.6f), matCyan);

            // Right Wall Accent Trim (Violet)
            CreateTrim(wallsParent.transform, "Trim_Right_Violet", new Vector3(8.84f, 2.4f, 0f), new Vector3(0.03f, 0.06f, 17.6f), matViolet);
            CreateTrim(wallsParent.transform, "Trim_Right_Base", new Vector3(8.84f, 0.1f, 0f), new Vector3(0.03f, 0.08f, 17.6f), matCyan);

            // 5. Corner Pillars
            float[] posX = { -8.7f, 8.7f };
            float[] posZ = { -8.7f, 8.7f };
            for (int x = 0; x < 2; x++)
            {
                for (int z = 0; z < 2; z++)
                {
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pillar.name = $"Pillar_{x}_{z}";
                    pillar.transform.SetParent(envParent.transform);
                    pillar.transform.position = new Vector3(posX[x], 2.4f, posZ[z]);
                    pillar.transform.localScale = new Vector3(0.6f, 4.8f, 0.6f);
                    pillar.GetComponent<Renderer>().sharedMaterial = matTrim;
                }
            }
        }

        private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateTrim(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = name;
            trim.transform.SetParent(parent);
            trim.transform.position = pos;
            trim.transform.localScale = scale;
            trim.GetComponent<Renderer>().sharedMaterial = mat;
            DestroyImmediate(trim.GetComponent<Collider>());
        }

        private static void BuildCompleteWorkstation()
        {
            Material matCharcoal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DarkCharcoal.mat");
            Material matDesk = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DeskSurface.mat");
            Material matDeskMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DeskMat.mat");
            Material matKbBody = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_KeyboardBody.mat");
            Material matKeycaps = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Keycaps.mat");
            Material matSpeaker = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_SpeakerCabinet.mat");
            Material matPC = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PCTower.mat");
            Material matCyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveCyan.mat");
            Material matViolet = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveViolet.mat");
            Material matMagenta = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveMagenta.mat");
            Material matScreen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_VirtualScreen.mat");

            // Clean up existing workstation
            GameObject oldWork = GameObject.Find("Workstation");
            if (oldWork != null) DestroyImmediate(oldWork);

            GameObject workParent = new GameObject("Workstation");
            workParent.transform.position = Vector3.zero;

            // 1. Desk Hierarchy
            GameObject deskRoot = new GameObject("Desk");
            deskRoot.transform.SetParent(workParent.transform);
            deskRoot.transform.position = new Vector3(0f, 0f, 0.65f);

            // Desk Top (2.1m wide, 0.85m deep, 0.05m thick)
            GameObject deskTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskTop.name = "Desk_Top";
            deskTop.transform.SetParent(deskRoot.transform);
            deskTop.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            deskTop.transform.localScale = new Vector3(2.1f, 0.05f, 0.85f);
            deskTop.GetComponent<Renderer>().sharedMaterial = matDesk;

            // Desk Legs (Angled / Modern Futuristic)
            GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legL.name = "Desk_Leg_L";
            legL.transform.SetParent(deskRoot.transform);
            legL.transform.localPosition = new Vector3(-0.98f, 0.365f, 0f);
            legL.transform.localScale = new Vector3(0.06f, 0.73f, 0.75f);
            legL.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legR.name = "Desk_Leg_R";
            legR.transform.SetParent(deskRoot.transform);
            legR.transform.localPosition = new Vector3(0.98f, 0.365f, 0f);
            legR.transform.localScale = new Vector3(0.06f, 0.73f, 0.75f);
            legR.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Cable Trough / Crossbar
            GameObject crossbar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crossbar.name = "Desk_Crossbar";
            crossbar.transform.SetParent(deskRoot.transform);
            crossbar.transform.localPosition = new Vector3(0f, 0.35f, 0.28f);
            crossbar.transform.localScale = new Vector3(1.96f, 0.06f, 0.12f);
            crossbar.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Desk Underglow LED Strips
            GameObject stripCyan = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripCyan.name = "Desk_Accent_Cyan";
            stripCyan.transform.SetParent(deskRoot.transform);
            stripCyan.transform.localPosition = new Vector3(0f, 0.725f, -0.41f);
            stripCyan.transform.localScale = new Vector3(2.02f, 0.012f, 0.03f);
            stripCyan.GetComponent<Renderer>().sharedMaterial = matCyan;
            DestroyImmediate(stripCyan.GetComponent<Collider>());

            GameObject stripViolet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripViolet.name = "Desk_Accent_Violet";
            stripViolet.transform.SetParent(deskRoot.transform);
            stripViolet.transform.localPosition = new Vector3(0f, 0.725f, 0.41f);
            stripViolet.transform.localScale = new Vector3(2.02f, 0.012f, 0.03f);
            stripViolet.GetComponent<Renderer>().sharedMaterial = matViolet;
            DestroyImmediate(stripViolet.GetComponent<Collider>());

            // Desk Mat (0.95m wide x 0.40m deep, centered)
            GameObject deskMat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskMat.name = "Desk_Mat";
            deskMat.transform.SetParent(deskRoot.transform);
            deskMat.transform.localPosition = new Vector3(0f, 0.7775f, -0.14f);
            deskMat.transform.localScale = new Vector3(0.95f, 0.005f, 0.40f);
            deskMat.GetComponent<Renderer>().sharedMaterial = matDeskMat;

            // Desk Mat Edge Stitching Accent (Magenta)
            GameObject matTrimObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matTrimObj.name = "DeskMat_StitchAccent";
            matTrimObj.transform.SetParent(deskMat.transform);
            matTrimObj.transform.localPosition = new Vector3(0f, 0.51f, -0.49f);
            matTrimObj.transform.localScale = new Vector3(1f, 0.1f, 0.02f);
            matTrimObj.GetComponent<Renderer>().sharedMaterial = matMagenta;
            DestroyImmediate(matTrimObj.GetComponent<Collider>());

            // 2. Futuristic Keyboard
            BuildKeyboard(deskRoot.transform, matKbBody, matKeycaps, matCyan, matViolet);

            // 3. Futuristic Mouse
            BuildMouse(deskRoot.transform, matKbBody, matMagenta, matCyan);

            // 4. Studio Monitor Speakers
            BuildSpeakers(deskRoot.transform, matSpeaker, matCyan, matViolet);

            // 5. Futuristic PC Tower
            BuildPCTower(deskRoot.transform, matPC, matCyan, matMagenta);

            // 6. Status Display / Tech Widget
            BuildStatusWidget(deskRoot.transform, matCharcoal, matCyan, matViolet);

            // 7. Primary Virtual Monitor
            BuildPrimaryMonitor(workParent.transform, deskRoot.transform.position, matCharcoal, matScreen);

            // 8. Phase 5 Computer Interaction Mode Setup
            ComputerInteractionSetup.SetupPhase5();
        }

        private static void BuildKeyboard(Transform parent, Material matBody, Material matKey, Material matCyan, Material matViolet)
        {
            GameObject kbRoot = new GameObject("Keyboard");
            kbRoot.transform.SetParent(parent);
            kbRoot.transform.localPosition = new Vector3(-0.06f, 0.782f, -0.15f);
            kbRoot.transform.localRotation = Quaternion.Euler(4f, 0f, 0f); // Ergonomic tilt

            // Keyboard Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Keyboard_Body";
            body.transform.SetParent(kbRoot.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            body.transform.localScale = new Vector3(0.44f, 0.015f, 0.15f);
            body.GetComponent<Renderer>().sharedMaterial = matBody;

            // Emissive Underglow Strip
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = "Keyboard_Glow";
            glow.transform.SetParent(body.transform);
            glow.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            glow.transform.localScale = new Vector3(1.02f, 0.2f, 1.05f);
            glow.GetComponent<Renderer>().sharedMaterial = matCyan;
            DestroyImmediate(glow.GetComponent<Collider>());

            // Key Blocks (Main typing block, Arrow keys, Numpad)
            GameObject keysMain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keysMain.name = "Keys_Alphanumeric";
            keysMain.transform.SetParent(kbRoot.transform);
            keysMain.transform.localPosition = new Vector3(-0.06f, 0.012f, 0f);
            keysMain.transform.localScale = new Vector3(0.28f, 0.01f, 0.12f);
            keysMain.GetComponent<Renderer>().sharedMaterial = matKey;
            DestroyImmediate(keysMain.GetComponent<Collider>());

            GameObject spacebar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spacebar.name = "Key_Spacebar";
            spacebar.transform.SetParent(kbRoot.transform);
            spacebar.transform.localPosition = new Vector3(-0.06f, 0.014f, -0.05f);
            spacebar.transform.localScale = new Vector3(0.12f, 0.01f, 0.02f);
            spacebar.GetComponent<Renderer>().sharedMaterial = matCyan;
            DestroyImmediate(spacebar.GetComponent<Collider>());

            GameObject keysNav = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keysNav.name = "Keys_NavCluster";
            keysNav.transform.SetParent(kbRoot.transform);
            keysNav.transform.localPosition = new Vector3(0.11f, 0.012f, 0f);
            keysNav.transform.localScale = new Vector3(0.04f, 0.01f, 0.12f);
            keysNav.GetComponent<Renderer>().sharedMaterial = matKey;
            DestroyImmediate(keysNav.GetComponent<Collider>());

            GameObject keysNumpad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keysNumpad.name = "Keys_Numpad";
            keysNumpad.transform.SetParent(kbRoot.transform);
            keysNumpad.transform.localPosition = new Vector3(0.17f, 0.012f, 0f);
            keysNumpad.transform.localScale = new Vector3(0.06f, 0.01f, 0.12f);
            keysNumpad.GetComponent<Renderer>().sharedMaterial = matKey;
            DestroyImmediate(keysNumpad.GetComponent<Collider>());
        }

        private static void BuildMouse(Transform parent, Material matBody, Material matMagenta, Material matCyan)
        {
            GameObject mouseRoot = new GameObject("Mouse");
            mouseRoot.transform.SetParent(parent);
            mouseRoot.transform.localPosition = new Vector3(0.30f, 0.785f, -0.15f);

            // Ergonomic Mouse Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Mouse_Body";
            body.transform.SetParent(mouseRoot.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.068f, 0.032f, 0.115f);
            body.GetComponent<Renderer>().sharedMaterial = matBody;

            // Scroll Wheel (Magenta Emissive)
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "Mouse_ScrollWheel";
            wheel.transform.SetParent(mouseRoot.transform);
            wheel.transform.localPosition = new Vector3(0f, 0.018f, 0.025f);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.014f, 0.006f, 0.014f);
            wheel.GetComponent<Renderer>().sharedMaterial = matMagenta;
            DestroyImmediate(wheel.GetComponent<Collider>());

            // Side Accent Glow (Cyan)
            GameObject sideGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideGlow.name = "Mouse_SideGlow";
            sideGlow.transform.SetParent(mouseRoot.transform);
            sideGlow.transform.localPosition = new Vector3(-0.035f, -0.005f, 0f);
            sideGlow.transform.localScale = new Vector3(0.004f, 0.012f, 0.08f);
            sideGlow.GetComponent<Renderer>().sharedMaterial = matCyan;
            DestroyImmediate(sideGlow.GetComponent<Collider>());
        }

        private static void BuildSpeakers(Transform parent, Material matSpeaker, Material matCyan, Material matViolet)
        {
            GameObject speakersGroup = new GameObject("Speakers");
            speakersGroup.transform.SetParent(parent);

            // Left Speaker (Symmetric, rested on desk, angled inward)
            BuildSingleSpeaker(speakersGroup.transform, "Speaker_L", new Vector3(-0.74f, 0.915f, 0.12f), 15f, matSpeaker, matCyan, matViolet);
            // Right Speaker (Symmetric, rested on desk, angled inward)
            BuildSingleSpeaker(speakersGroup.transform, "Speaker_R", new Vector3(0.74f, 0.915f, 0.12f), -15f, matSpeaker, matCyan, matViolet);
        }

        private static void BuildSingleSpeaker(Transform parent, string name, Vector3 pos, float rotY, Material matCabinet, Material matCyan, Material matViolet)
        {
            GameObject spk = new GameObject(name);
            spk.transform.SetParent(parent);
            spk.transform.localPosition = pos;
            spk.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

            // Cabinet
            GameObject cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cab.name = "Cabinet";
            cab.transform.SetParent(spk.transform);
            cab.transform.localPosition = Vector3.zero;
            cab.transform.localScale = new Vector3(0.14f, 0.28f, 0.16f);
            cab.GetComponent<Renderer>().sharedMaterial = matCabinet;

            // Main Woofer Ring (Cyan)
            GameObject woofer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            woofer.name = "Woofer_Ring";
            woofer.transform.SetParent(spk.transform);
            woofer.transform.localPosition = new Vector3(0f, -0.04f, -0.081f);
            woofer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            woofer.transform.localScale = new Vector3(0.09f, 0.005f, 0.09f);
            woofer.GetComponent<Renderer>().sharedMaterial = matCyan;
            DestroyImmediate(woofer.GetComponent<Collider>());

            // Tweeter Ring (Violet)
            GameObject tweeter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tweeter.name = "Tweeter_Ring";
            tweeter.transform.SetParent(spk.transform);
            tweeter.transform.localPosition = new Vector3(0f, 0.07f, -0.081f);
            tweeter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tweeter.transform.localScale = new Vector3(0.04f, 0.005f, 0.04f);
            tweeter.GetComponent<Renderer>().sharedMaterial = matViolet;
            DestroyImmediate(tweeter.GetComponent<Collider>());
        }

        private static void BuildPCTower(Transform parent, Material matPC, Material matCyan, Material matMagenta)
        {
            GameObject pcRoot = new GameObject("PC_Tower");
            pcRoot.transform.SetParent(parent);
            pcRoot.transform.localPosition = new Vector3(0.94f, 1.015f, 0.10f);

            // Main Chassis
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            chassis.transform.SetParent(pcRoot.transform);
            chassis.transform.localPosition = Vector3.zero;
            chassis.transform.localScale = new Vector3(0.20f, 0.48f, 0.42f);
            chassis.GetComponent<Renderer>().sharedMaterial = matPC;

            // Front Intake LED Ring 1 (Cyan)
            GameObject fan1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fan1.name = "Front_Fan_Top";
            fan1.transform.SetParent(pcRoot.transform);
            fan1.transform.localPosition = new Vector3(0f, 0.10f, -0.211f);
            fan1.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            fan1.transform.localScale = new Vector3(0.12f, 0.005f, 0.12f);
            fan1.GetComponent<Renderer>().sharedMaterial = matCyan;
            DestroyImmediate(fan1.GetComponent<Collider>());

            // Front Intake LED Ring 2 (Magenta)
            GameObject fan2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fan2.name = "Front_Fan_Bottom";
            fan2.transform.SetParent(pcRoot.transform);
            fan2.transform.localPosition = new Vector3(0f, -0.08f, -0.211f);
            fan2.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            fan2.transform.localScale = new Vector3(0.12f, 0.005f, 0.12f);
            fan2.GetComponent<Renderer>().sharedMaterial = matMagenta;
            DestroyImmediate(fan2.GetComponent<Collider>());
        }

        private static void BuildStatusWidget(Transform parent, Material matBody, Material matCyan, Material matViolet)
        {
            GameObject widget = new GameObject("StatusDisplay");
            widget.transform.SetParent(parent);
            widget.transform.localPosition = new Vector3(-0.52f, 0.835f, 0.14f);
            widget.transform.localRotation = Quaternion.Euler(-15f, 20f, 0f);

            // Stand & Frame
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Widget_Frame";
            frame.transform.SetParent(widget.transform);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(0.16f, 0.10f, 0.02f);
            frame.GetComponent<Renderer>().sharedMaterial = matBody;

            // Display Screen (Cyan Telemetry Glow)
            GameObject display = GameObject.CreatePrimitive(PrimitiveType.Quad);
            display.name = "Widget_Screen";
            display.transform.SetParent(widget.transform);
            display.transform.localPosition = new Vector3(0f, 0f, -0.011f);
            display.transform.localScale = new Vector3(0.14f, 0.08f, 1f);
            display.GetComponent<Renderer>().sharedMaterial = matCyan;
            DestroyImmediate(display.GetComponent<Collider>());
        }

        private static void BuildPrimaryMonitor(Transform workParent, Vector3 deskPos, Material matCharcoal, Material matScreen)
        {
            // Positioned centered at normal viewing height
            Vector3 monitorPos = deskPos + new Vector3(0f, 0.77f, 0.18f);

            GameObject monitorRoot = new GameObject("VirtualMonitor");
            monitorRoot.transform.SetParent(workParent);
            monitorRoot.transform.position = monitorPos;

            // Monitor Stand Base
            GameObject standBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            standBase.name = "MonitorStand_Base";
            standBase.transform.SetParent(monitorRoot.transform);
            standBase.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            standBase.transform.localScale = new Vector3(0.42f, 0.02f, 0.22f);
            standBase.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            DestroyImmediate(standBase.GetComponent<Collider>());

            // Monitor Stand Neck (Angled support column)
            GameObject standNeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            standNeck.name = "MonitorStand_Neck";
            standNeck.transform.SetParent(monitorRoot.transform);
            standNeck.transform.localPosition = new Vector3(0f, 0.18f, 0.08f);
            standNeck.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            standNeck.transform.localScale = new Vector3(0.08f, 0.36f, 0.04f);
            standNeck.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            DestroyImmediate(standNeck.GetComponent<Collider>());

            // Monitor Frame (1.24m x 0.72m x 0.04m)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "MonitorFrame";
            frame.transform.SetParent(monitorRoot.transform);
            frame.transform.localPosition = new Vector3(0f, 0.38f, 0.02f);
            frame.transform.localScale = new Vector3(1.24f, 0.72f, 0.04f);
            frame.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Screen Quad Surface (1.2m x 0.68m)
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "Screen";
            screen.transform.SetParent(frame.transform);
            screen.transform.localPosition = new Vector3(0f, 0f, -0.51f);
            screen.transform.localScale = new Vector3(0.97f, 0.95f, 1f);
            screen.GetComponent<Renderer>().sharedMaterial = matScreen;

            // Add RemoteDesktopScreen component
            var screenComp = screen.AddComponent<RemoteDesktopScreen>();

            // Add VRDesktopInteractable for cursor raycasting
            var interactable = screen.AddComponent<VRDesktopInteractable>();

            // Link existing UI cursor if present in scene
            var existingCursor = Object.FindAnyObjectByType<VRRemoteDesktop.Input.VRDesktopCursor>();
            if (existingCursor != null)
            {
                SerializedObject intSo = new SerializedObject(interactable);
                intSo.FindProperty("_cursor").objectReferenceValue = existingCursor;
                intSo.ApplyModifiedProperties();
            }

            // Save monitor prefab
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Computer"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "Computer");
            }
            string prefabPath = "Assets/Prefabs/Computer/VirtualMonitor.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(monitorRoot, prefabPath, InteractionMode.AutomatedAction);
            Debug.Log("[WorkstationEnvironmentBuilder] Saved VirtualMonitor prefab to: " + prefabPath);
        }

        private static void SetupLightingAndPostProcessing()
        {
            // 1. Environmental Key Light (Soft, moody directional light)
            GameObject dirLight = GameObject.Find("Directional Light");
            if (dirLight != null)
            {
                Light l = dirLight.GetComponent<Light>();
                if (l != null)
                {
                    l.intensity = 0.15f;
                    l.color = new Color(0.25f, 0.30f, 0.42f);
                    l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
            }

            // 2. Workstation Ambient Downlight
            GameObject workLight = GameObject.Find("Workstation Light");
            if (workLight == null)
            {
                workLight = new GameObject("Workstation Light");
            }
            workLight.transform.position = new Vector3(0f, 2.2f, 0.65f);
            Light deskLight = workLight.GetComponent<Light>();
            if (deskLight == null) deskLight = workLight.AddComponent<Light>();
            deskLight.type = LightType.Point;
            deskLight.color = new Color(0.85f, 0.92f, 1.0f);
            deskLight.intensity = 2.0f;
            deskLight.range = 5.0f;

            // 3. Monitor Ambient Screen Glow (Soft blue-cyan glow reflecting onto desk)
            GameObject monitorGlow = GameObject.Find("Monitor Glow Light");
            if (monitorGlow == null)
            {
                monitorGlow = new GameObject("Monitor Glow Light");
            }
            monitorGlow.transform.position = new Vector3(0f, 1.15f, 0.70f);
            Light mLight = monitorGlow.GetComponent<Light>();
            if (mLight == null) mLight = monitorGlow.AddComponent<Light>();
            mLight.type = LightType.Point;
            mLight.color = new Color(0.1f, 0.75f, 0.95f);
            mLight.intensity = 1.6f;
            mLight.range = 2.5f;

            // 4. Room Accent Bounce Lights (Cyan & Violet near architectural trims)
            GameObject accentL = GameObject.Find("Accent_Light_Left");
            if (accentL == null) accentL = new GameObject("Accent_Light_Left");
            accentL.transform.position = new Vector3(-7.5f, 2.4f, 3.5f);
            Light lLight = accentL.GetComponent<Light>();
            if (lLight == null) lLight = accentL.AddComponent<Light>();
            lLight.type = LightType.Point;
            lLight.color = new Color(0.5f, 0.1f, 1.0f); // Violet
            lLight.intensity = 1.5f;
            lLight.range = 8.5f;

            GameObject accentR = GameObject.Find("Accent_Light_Right");
            if (accentR == null) accentR = new GameObject("Accent_Light_Right");
            accentR.transform.position = new Vector3(7.5f, 2.4f, 3.5f);
            Light rLight = accentR.GetComponent<Light>();
            if (rLight == null) rLight = accentR.AddComponent<Light>();
            rLight.type = LightType.Point;
            rLight.color = new Color(0.0f, 0.9f, 1.0f); // Cyan
            rLight.intensity = 1.5f;
            rLight.range = 8.5f;

            // 5. Volume Profile with ACES Tonemapping and Balanced Bloom
            Volume volume = Object.FindAnyObjectByType<Volume>();
            if (volume != null && volume.profile != null)
            {
                if (!volume.profile.TryGet(out Bloom bloom))
                {
                    bloom = volume.profile.Add<Bloom>();
                }
                bloom.active = true;
                bloom.intensity.Override(1.2f);
                bloom.threshold.Override(1.0f);

                if (!volume.profile.TryGet(out Tonemapping tonemap))
                {
                    tonemap = volume.profile.Add<Tonemapping>();
                }
                tonemap.active = true;
                tonemap.mode.Override(TonemappingMode.ACES);
            }
        }

        private static void ConnectScreenToRenderTexture()
        {
            string rtPath = "Assets/Textures/UI_RenderTexture.renderTexture";
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            if (rt != null)
            {
                var screen = Object.FindAnyObjectByType<RemoteDesktopScreen>();
                if (screen != null)
                {
                    SerializedObject screenSo = new SerializedObject(screen);
                    screenSo.FindProperty("_defaultTexture").objectReferenceValue = rt;
                    screenSo.ApplyModifiedProperties();
                    screen.SetTexture(rt);
                    Debug.Log("[WorkstationEnvironmentBuilder] Connected UI_RenderTexture to RemoteDesktopScreen!");
                }
            }
        }
    }
}
