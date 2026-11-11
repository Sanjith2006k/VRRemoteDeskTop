using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.XR;
using VRRemoteDesktop.Input;
using VRRemoteDesktop.Environment;
using VRRemoteDesktop.Audio;
using VRRemoteDesktop.UI;

namespace VRRemoteDesktop.Editor
{
    /// <summary>
    /// Master builder fulfilling user requirements:
    /// 1. Interactive Workstation Chair:
    ///    - High-end ergonomic gaming chair in front of desk.
    ///    - Pressing 'E' triggers smooth camera animation into seated computer mode.
    /// 2. Resized Cozy Architectural Room:
    ///    - Scaled down from 24m warehouse to perfectly proportioned ~9.2m x 8.2m space.
    ///    - Left wall directly touches and frames the luxury sectional sofa.
    ///    - Right wall features the space observation window.
    ///    - Back wall brought forward, eliminating all empty space.
    /// 3. Four Massive Cinema Corner Speakers:
    ///    - Big floor standing stage tower subwoofers at front-left and front-right.
    ///    - Big angled cinema surround speakers at top-left and top-right ceiling corners.
    /// 4. Massive Dual-Engine Ceiling Projector:
    ///    - Commercial IMAX digital laser projector with dual optic lenses and cooling fins.
    /// 5. Curved Cockpit Battlestation Desk:
    ///    - PC CPU tower has zero collision with right monitor.
    ///    - All accessories, peripherals, and consoles cleanly aligned.
    /// </summary>
    [InitializeOnLoad]
    public static class ExpandedRoomWorkstationBuilder
    {
        private const string PrefKey = "VRRemoteDesktop_ExpandedRoom_BuildDone_v21";

        static ExpandedRoomWorkstationBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(PrefKey, false))
                {
                    SessionState.SetBool(PrefKey, true);
                    BuildCommandCenter();
                }
            };
        }

        [MenuItem("VR Remote Desktop/Build Expanded 3-Monitor Command Center")]
        public static void BuildCommandCenter()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Main")
            {
                if (File.Exists("Assets/Scenes/Main.unity"))
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                }
            }

            Debug.Log("[ExpandedRoomWorkstationBuilder] Starting build of Proportionate Battlestation, Chair, Speakers & IMAX Screen...");

            // 1. Ensure RenderTextures and Materials exist
            MultiMonitorAssetSetup.EnsureAssets();

            // 2. Load Materials
            Material matCharcoal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DarkCharcoal.mat");
            Material matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Floor.mat");
            Material matDesk = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DeskSurface.mat");
            Material matDeskMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DeskMat.mat");
            Material matKbBody = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_KeyboardBody.mat");
            Material matKeycaps = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Keycaps.mat");
            Material matSpeaker = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_SpeakerCabinet.mat");
            Material matPC = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PCTower.mat");
            Material matPlatform = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PlatformDark.mat");
            Material matAcoustic = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_AcousticWood.mat");
            Material matChrome = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_MetalChrome.mat");
            Material matCoffee = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_CoffeeMachine.mat");
            Material matWhiteDrawer = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DeskDrawerWhite.mat");
            Material matGlass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_TemperedGlass.mat");

            // Emissives
            Material matCyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveCyan.mat");
            Material matViolet = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveViolet.mat");
            Material matMagenta = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveMagenta.mat");
            Material matOrange = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_NeonOrange.mat");
            Material matGreen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_NeonGreen.mat");
            Material matHoloCyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_HologramCyan.mat");
            Material matAmbientGlow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_AmbientWallGlow.mat");
            Material matWarmOrb = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_WarmOrb.mat");

            // Screen & Projector Materials
            Material matScreenCenter = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_VirtualScreen.mat");
            Material matScreenLeft = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_ScreenLeft.mat");
            Material matScreenRight = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_ScreenRight.mat");
            Material matProjectorScreen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_ProjectorScreen.mat");
            Material matProjectorBeam = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_ProjectorBeam.mat");

            // Scenery & Biophilic Corner Plant Materials
            Material matCosmicVista = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_CosmicVista.mat");
            Material matPlanterCeramic = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PlanterCeramic.mat");
            Material matPlanterSoil = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PlanterSoil.mat");
            Material matPlantStem = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PlantStem.mat");
            Material matLeafEmerald = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PlantLeafEmerald.mat");
            Material matLeafVibrant = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PlantLeafVibrant.mat");

            // 3D Nature Landscape Materials (Mountains, River, Trees, Cottages, Valley)
            Material matMtnRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_MountainRock.mat");
            Material matMtnSnow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_MountainSnow.mat");
            Material matGrass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_TerrainGrass.mat");
            Material matPineDark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PineNeedles.mat");
            Material matPineLight = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PineNeedlesVibrant.mat");
            Material matBark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_TreeBark.mat");
            Material matRiver = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_RiverWater.mat");
            Material matRiverStone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_RiverStone.mat");
            Material matCabinWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_CabinWood.mat");
            Material matCabinRoof = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_CabinRoof.mat");
            Material matWindowGlow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_WindowGlowWarm.mat");

            // Tracking lists for dynamic lighting controller
            List<Renderer> cyanTrims = new List<Renderer>();
            List<Renderer> violetTrims = new List<Renderer>();

            // 3. Build Proportionate Room Architecture (Left Wall touches Sofa, 4 Big Corner Speakers, Big Projector)
            try
            {
                BuildProportionateArchitecture(matCharcoal, matFloor, matPlatform, matAcoustic, matChrome, matGlass, matSpeaker,
                                               matCyan, matViolet, matProjectorScreen, matProjectorBeam, cyanTrims, violetTrims,
                                               matCosmicVista, matPlanterCeramic, matPlanterSoil, matPlantStem, matLeafEmerald, matLeafVibrant,
                                               matMtnRock, matMtnSnow, matGrass, matBark, matPineDark, matPineLight,
                                               matRiver, matRiverStone, matCabinWood, matCabinRoof, matWindowGlow);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ExpandedRoomWorkstationBuilder] Error in Step 3 (Architecture): " + ex);
            }

            // 4. Build Triple Monitor Workstation with Curved Desk & Interactive Chair
            try
            {
                BuildTripleMonitorWorkstation(matCharcoal, matDesk, matDeskMat, matKbBody, matKeycaps, matSpeaker, matPC, matChrome,
                                              matCyan, matViolet, matMagenta, matOrange, matGreen, matWhiteDrawer, matGlass,
                                              matAmbientGlow, matWarmOrb, matAcoustic,
                                              matScreenCenter, matScreenLeft, matScreenRight, cyanTrims, violetTrims);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ExpandedRoomWorkstationBuilder] Error in Step 4 (Workstation): " + ex);
            }

            // 5. Build Side Canvases (Left & Right UI Dashboards)
            try
            {
                BuildAuxiliaryDashboards();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ExpandedRoomWorkstationBuilder] Error in Step 5 (Dashboards): " + ex);
            }

            // 6. Build Interactive Props (Includes VIP Theater Viewing Sofa behind PC chair & in front of Globe!)
            try
            {
                BuildInteractiveProps(matCharcoal, matChrome, matCoffee, matCyan, matViolet, matMagenta, matOrange, matGreen, matHoloCyan, matAcoustic);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ExpandedRoomWorkstationBuilder] Error in Step 6 (Interactive Props): " + ex);
            }

            // 7. Setup / Wire Lighting & Audio Managers
            try
            {
                SetupRoomLighting(cyanTrims, violetTrims);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ExpandedRoomWorkstationBuilder] Error in Step 7 (Lighting): " + ex);
            }

            // 8. Re-wire ComputerInteractionManager & Cursor & TheaterCinemaController
            try
            {
                RewireInteractionSystem();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[ExpandedRoomWorkstationBuilder] Error in Step 8 (Rewire): " + ex);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ExpandedRoomWorkstationBuilder] Command Center build completed and saved successfully!");
        }

        private static void BuildProportionateArchitecture(Material matCharcoal, Material matFloor, Material matPlatform, Material matAcoustic,
                                                          Material matChrome, Material matGlass, Material matSpeaker,
                                                          Material matCyan, Material matViolet, Material matProjectorScreen, Material matProjectorBeam,
                                                          List<Renderer> cyanTrims, List<Renderer> violetTrims,
                                                          Material matCosmicVista, Material matPlanterCeramic, Material matPlanterSoil, Material matPlantStem,
                                                          Material matLeafEmerald, Material matLeafVibrant,
                                                          Material matMtnRock, Material matMtnSnow, Material matGrass,
                                                          Material matBark, Material matPineDark, Material matPineLight,
                                                          Material matRiver, Material matRiverStone,
                                                          Material matCabinWood, Material matCabinRoof, Material matWindowGlow)
        {
            GameObject oldEnv = GameObject.Find("Environment");
            if (oldEnv != null) Object.DestroyImmediate(oldEnv);

            GameObject env = new GameObject("Environment");

            // Room dimensions: 10.4m width (x: [-4.6, +5.8]), 9.0m depth (z: [-5.0, +4.0]), 5.2m height
            // 1. Floor (10.8m x 9.4m at y = -0.05m)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(env.transform);
            floor.transform.position = new Vector3(0.6f, -0.05f, -0.5f);
            floor.transform.localScale = new Vector3(10.8f, 0.1f, 9.4f);
            floor.GetComponent<Renderer>().sharedMaterial = matFloor;

            // 2. Raised Command Platform (9.8m x 5.6m x 0.12m)
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "CommandPlatform";
            platform.transform.SetParent(env.transform);
            platform.transform.position = new Vector3(0.5f, 0.06f, 0.9f);
            platform.transform.localScale = new Vector3(9.8f, 0.12f, 5.6f);
            platform.GetComponent<Renderer>().sharedMaterial = matPlatform;

            // Step edge lighting trim (Front & Sides)
            CreateStepTrim(platform.transform, "Platform_Trim_Front", new Vector3(0f, 0.055f, -0.505f), new Vector3(1f, 0.08f, 0.015f), matCyan, cyanTrims);
            CreateStepTrim(platform.transform, "Platform_Trim_Left", new Vector3(-0.505f, 0.055f, 0f), new Vector3(0.015f, 0.08f, 1f), matCyan, cyanTrims);
            CreateStepTrim(platform.transform, "Platform_Trim_Right", new Vector3(0.505f, 0.055f, 0f), new Vector3(0.015f, 0.08f, 1f), matCyan, cyanTrims);

            // 3. Ceiling (10.8m x 9.4m at y = 5.25m)
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(env.transform);
            ceiling.transform.position = new Vector3(0.6f, 5.25f, -0.5f);
            ceiling.transform.localScale = new Vector3(10.8f, 0.1f, 9.4f);
            ceiling.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Ceiling recessed light channels
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                beam.name = $"Ceiling_Beam_{i}";
                beam.transform.SetParent(ceiling.transform);
                beam.transform.localPosition = new Vector3(i * 0.32f, -0.51f, 0f);
                beam.transform.localScale = new Vector3(0.035f, 0.04f, 0.85f);
                Material m = (i < 0) ? matCyan : matViolet;
                beam.GetComponent<Renderer>().sharedMaterial = m;
                Object.DestroyImmediate(beam.GetComponent<Collider>());
                if (i < 0) cyanTrims.Add(beam.GetComponent<Renderer>()); else violetTrims.Add(beam.GetComponent<Renderer>());
            }

            // 4. Walls
            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(env.transform);

            // Front Wall (z = 4.0m)
            CreateWall(walls.transform, "Wall_Front", new Vector3(0.6f, 2.6f, 4.0f), new Vector3(10.8f, 5.3f, 0.3f), matCharcoal);

            // Acoustic feature wall slats directly behind workstation
            GameObject acousticWall = new GameObject("AcousticFeatureWall");
            acousticWall.transform.SetParent(walls.transform);
            acousticWall.transform.position = new Vector3(0f, 2.6f, 3.82f);
            for (int s = -10; s <= 10; s++)
            {
                GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = $"Slat_{s}";
                slat.transform.SetParent(acousticWall.transform);
                slat.transform.localPosition = new Vector3(s * 0.42f, 0f, 0f);
                slat.transform.localScale = new Vector3(0.28f, 5.0f, 0.08f);
                slat.GetComponent<Renderer>().sharedMaterial = matAcoustic;
            }

            // Vertical Accent Lighting Channels on outer edges of feature wall
            for (int v = -1; v <= 1; v += 2)
            {
                GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"FeatureWall_LightStrip_{v}";
                strip.transform.SetParent(acousticWall.transform);
                strip.transform.localPosition = new Vector3(v * 4.25f, 0f, -0.045f);
                strip.transform.localScale = new Vector3(0.04f, 4.8f, 0.02f);
                strip.GetComponent<Renderer>().sharedMaterial = matCyan;
                Object.DestroyImmediate(strip.GetComponent<Collider>());
                cyanTrims.Add(strip.GetComponent<Renderer>());
            }

            // --- PROPORTIONATE CINEMA SCREEN (Framed with ~1.4m margins on all 4 sides) ---
            GameObject projScreenObj = new GameObject("BigProjectorScreen_IMAX");
            projScreenObj.transform.SetParent(acousticWall.transform, false);
            projScreenObj.transform.localPosition = new Vector3(0f, -0.1f, -0.065f);

            // Outer bevelled screen frame (6.8m x 3.3m - beautifully proportioned)
            GameObject screenFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screenFrame.name = "ProjectorScreen_Frame";
            screenFrame.transform.SetParent(projScreenObj.transform, false);
            screenFrame.transform.localScale = new Vector3(6.8f, 3.3f, 0.05f);
            screenFrame.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            Object.DestroyImmediate(screenFrame.GetComponent<Collider>());

            // Matte white IMAX projection canvas (6.7m x 3.15m)
            GameObject screenQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenQuad.name = "ProjectorScreen_Canvas";
            screenQuad.transform.SetParent(screenFrame.transform, false);
            screenQuad.transform.localPosition = new Vector3(0f, 0f, -0.51f);
            screenQuad.transform.localScale = new Vector3(0.985f, 0.965f, 1f);
            screenQuad.GetComponent<Renderer>().sharedMaterial = matProjectorScreen;
            Object.DestroyImmediate(screenQuad.GetComponent<Collider>());

            // Screen ambient bottom border glow
            GameObject screenBottomTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screenBottomTrim.name = "ProjectorScreen_Trim";
            screenBottomTrim.transform.SetParent(screenFrame.transform, false);
            screenBottomTrim.transform.localPosition = new Vector3(0f, -0.505f, -0.51f);
            screenBottomTrim.transform.localScale = new Vector3(0.99f, 0.015f, 0.02f);
            screenBottomTrim.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(screenBottomTrim.GetComponent<Collider>());
            cyanTrims.Add(screenBottomTrim.GetComponent<Renderer>());

            // Test Welcome Screen Canvas for VIP Theater Cinema Mode (Parented to acoustic wall with uniform 1:1 scale)
            BuildTheaterWelcomeCanvas(acousticWall.transform);

            // --- 4 BIG CINEMA SPEAKERS AT THE 4 EDGES/CORNERS OF THE SCREEN ---
            BuildCinemaCornerSpeakers(env.transform, matSpeaker, matCharcoal, matChrome, matCyan);

            // --- MASSIVE DUAL-ENGINE CEILING PROJECTOR ---
            BuildBigCeilingProjector(env.transform, matCharcoal, matChrome, matCyan);

            // Back Wall (z = -5.0m) - Extended back to make marked area much bigger
            CreateWall(walls.transform, "Wall_Back", new Vector3(0.6f, 2.6f, -5.0f), new Vector3(10.8f, 5.3f, 0.3f), matCharcoal);

            // Left Wall (x = -4.6m) - DIRECTLY TOUCHES THE SOFA!
            CreateWall(walls.transform, "Wall_Left", new Vector3(-4.6f, 2.6f, -0.5f), new Vector3(0.3f, 5.3f, 9.4f), matCharcoal);

            // Right Wall (x = +5.8m) - Widened significantly to enlarge marked area next to globe!
            CreateWall(walls.transform, "Wall_Right_Top", new Vector3(5.8f, 4.4f, -0.5f), new Vector3(0.3f, 1.6f, 9.4f), matCharcoal);
            CreateWall(walls.transform, "Wall_Right_Bottom", new Vector3(5.8f, 0.6f, -0.5f), new Vector3(0.3f, 1.2f, 9.4f), matCharcoal);
            CreateWall(walls.transform, "Wall_Right_Back", new Vector3(5.8f, 2.6f, -4.0f), new Vector3(0.3f, 5.3f, 2.3f), matCharcoal);
            CreateWall(walls.transform, "Wall_Right_Front", new Vector3(5.8f, 2.6f, 3.0f), new Vector3(0.3f, 5.3f, 2.3f), matCharcoal);

            // Observation Glass Pane (6.6m x 2.6m) - Crystal-clear tempered glass
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glass.name = "ObservationWindow_Glass";
            glass.transform.SetParent(walls.transform);
            glass.transform.position = new Vector3(5.75f, 2.6f, -0.5f);
            glass.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            glass.transform.localScale = new Vector3(6.6f, 2.6f, 1f);
            glass.GetComponent<Renderer>().sharedMaterial = matGlass;

            // Audio: Walking near the window dynamically plays and modulates the nature ambient scenery sound!
            glass.AddComponent<VRRemoteDesktop.Environment.WindowSceneryAudio>();

            // Fully Modelled 3D Nature Landscape outside the window (Mountains, River, Trees, Cottages, Valley)
            BuildWindowScenery(walls.transform, matCosmicVista, matCyan,
                               matMtnRock, matMtnSnow, matGrass, matBark, matPineDark, matPineLight,
                               matRiver, matRiverStone, matCabinWood, matCabinRoof, matWindowGlow);

            // --- LUXURY SOFA & LOUNGE TABLE (Hugging Left Wall at x = -4.5m) ---
            BuildLoungeArea(env.transform, matCharcoal, matChrome, matGlass, matPlatform, matCyan, matViolet);

            // --- TWO BIOPHILIC CYBER PLANTS IN EACH CORNER (8 PLANTS TOTAL) ---
            BuildCornerPlants(env.transform, matPlanterCeramic, matPlanterSoil, matPlantStem, matLeafEmerald, matLeafVibrant, matCyan);
        }

        private static void BuildWindowScenery(Transform parent, Material matCosmicVista, Material matCyan,
                                              Material matMtnRock, Material matMtnSnow, Material matGrass,
                                              Material matBark, Material matPineDark, Material matPineLight,
                                              Material matRiver, Material matRiverStone,
                                              Material matCabinWood, Material matCabinRoof, Material matWindowGlow)
        {
            GameObject sceneryRoot = new GameObject("ObservationWindow_Scenery");
            sceneryRoot.transform.SetParent(parent, false);

            NatureLandscapeBuilder.Build3DLandscape(sceneryRoot.transform,
                                                    matMtnRock, matMtnSnow, matGrass,
                                                    matBark, matPineDark, matPineLight,
                                                    matRiver, matRiverStone,
                                                    matCabinWood, matCabinRoof, matWindowGlow,
                                                    matCosmicVista);
        }

        private static void BuildCornerPlants(Transform parent, Material matCeramic, Material matSoil, Material matStem,
                                              Material matLeafEmerald, Material matLeafVibrant, Material matCyan)
        {
            GameObject plantsRoot = new GameObject("RoomCornerPlants");
            plantsRoot.transform.SetParent(parent, false);

            // Rear-Left Corner ONLY (behind luxury lounge sofa) - 2 plants framing the sofa
            // Plants near the coffee machine (rear-right) and theater speakers (front-left/right) removed per user requests.
            BuildTallArchitecturalPlant(plantsRoot.transform, "Plant_RL_TallPalm", new Vector3(-4.10f, 0.06f, -4.30f), 65f,
                                        matCeramic, matSoil, matStem, matLeafEmerald, matLeafVibrant, matCyan);
            BuildLushMonsteraPlant(plantsRoot.transform, "Plant_RL_Monstera", new Vector3(-3.50f, 0.06f, -4.50f), -25f,
                                   matCeramic, matSoil, matStem, matLeafEmerald, matLeafVibrant, matCyan);
        }

        private static void BuildTallArchitecturalPlant(Transform parent, string name, Vector3 pos, float yRot,
                                                       Material matCeramic, Material matSoil, Material matStem,
                                                       Material matLeafA, Material matLeafB, Material matCyan)
        {
            GameObject plantRoot = new GameObject(name);
            plantRoot.transform.SetParent(parent, false);
            plantRoot.transform.position = pos;
            plantRoot.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // 1. Ceramic Planter Pot (Cylinder, 0.44m wide x 0.50m tall)
            GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "PlanterPot";
            pot.transform.SetParent(plantRoot.transform, false);
            pot.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            pot.transform.localScale = new Vector3(0.44f, 0.25f, 0.44f);
            pot.GetComponent<Renderer>().sharedMaterial = matCeramic;

            // Planter base ring (metallic riser)
            GameObject potBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            potBase.name = "PlanterBase";
            potBase.transform.SetParent(plantRoot.transform, false);
            potBase.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            potBase.transform.localScale = new Vector3(0.38f, 0.03f, 0.38f);
            potBase.GetComponent<Renderer>().sharedMaterial = matCeramic;

            // Planter glowing LED accent ring near rim
            GameObject ledRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ledRing.name = "Planter_LEDRing";
            ledRing.transform.SetParent(plantRoot.transform, false);
            ledRing.transform.localPosition = new Vector3(0f, 0.47f, 0f);
            ledRing.transform.localScale = new Vector3(0.455f, 0.015f, 0.455f);
            ledRing.GetComponent<Renderer>().sharedMaterial = matCyan;

            // Soil Bed
            GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            soil.name = "PottingSoil";
            soil.transform.SetParent(plantRoot.transform, false);
            soil.transform.localPosition = new Vector3(0f, 0.485f, 0f);
            soil.transform.localScale = new Vector3(0.42f, 0.015f, 0.42f);
            soil.GetComponent<Renderer>().sharedMaterial = matSoil;

            // 2. Central Cane Stalks (Bamboo-style vertical stems)
            float[] stemHeights = new float[] { 1.15f, 1.45f, 1.65f };
            Vector3[] stemOffsets = new Vector3[]
            {
                new Vector3(-0.04f, 0f, -0.04f),
                new Vector3(0.05f, 0f, -0.02f),
                new Vector3(0.0f, 0f, 0.05f)
            };

            for (int s = 0; s < 3; s++)
            {
                GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stem.name = $"Stem_{s}";
                stem.transform.SetParent(plantRoot.transform, false);
                float h = stemHeights[s];
                stem.transform.localPosition = stemOffsets[s] + new Vector3(0f, 0.50f + h * 0.5f, 0f);
                stem.transform.localScale = new Vector3(0.024f, h * 0.5f, 0.024f);
                stem.transform.localRotation = Quaternion.Euler((s == 0 ? 3f : -2f), 0f, (s == 1 ? 3f : -2f));
                stem.GetComponent<Renderer>().sharedMaterial = matStem;
            }

            // 3. Arching Palm / Ficus Fronds (10 tiered leaves in radial spiral)
            int leafCount = 10;
            for (int i = 0; i < leafCount; i++)
            {
                float frac = (float)i / leafCount;
                float angle = i * 137.5f;
                float height = Mathf.Lerp(0.85f, 1.75f, frac);
                float radius = Mathf.Lerp(0.30f, 0.48f, Mathf.Sin(frac * Mathf.PI));
                float leafLength = Mathf.Lerp(0.40f, 0.60f, frac);

                Vector3 leafPos = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius * 0.45f,
                    height,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius * 0.45f
                );

                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = $"PalmLeaf_{i}";
                leaf.transform.SetParent(plantRoot.transform, false);
                leaf.transform.localPosition = leafPos;

                float pitch = Mathf.Lerp(22f, 48f, frac);
                leaf.transform.localRotation = Quaternion.Euler(pitch, angle, -8f * Mathf.Sin(angle * Mathf.Deg2Rad));
                leaf.transform.localScale = new Vector3(0.14f, 0.008f, leafLength);
                leaf.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? matLeafA : matLeafB;
                Object.DestroyImmediate(leaf.GetComponent<Collider>());
            }
        }

        private static void BuildLushMonsteraPlant(Transform parent, string name, Vector3 pos, float yRot,
                                                   Material matCeramic, Material matSoil, Material matStem,
                                                   Material matLeafA, Material matLeafB, Material matCyan)
        {
            GameObject plantRoot = new GameObject(name);
            plantRoot.transform.SetParent(parent, false);
            plantRoot.transform.position = pos;
            plantRoot.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // 1. Wide Bowl Ceramic Planter (0.50m diameter x 0.30m tall)
            GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.name = "MonsteraPot";
            pot.transform.SetParent(plantRoot.transform, false);
            pot.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            pot.transform.localScale = new Vector3(0.50f, 0.15f, 0.50f);
            pot.GetComponent<Renderer>().sharedMaterial = matCeramic;

            // Glowing cyan accent ring around middle
            GameObject ledRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ledRing.name = "Monstera_LEDRing";
            ledRing.transform.SetParent(plantRoot.transform, false);
            ledRing.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            ledRing.transform.localScale = new Vector3(0.46f, 0.015f, 0.46f);
            ledRing.GetComponent<Renderer>().sharedMaterial = matCyan;

            // Soil Bed
            GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            soil.name = "MonsteraSoil";
            soil.transform.SetParent(plantRoot.transform, false);
            soil.transform.localPosition = new Vector3(0f, 0.29f, 0f);
            soil.transform.localScale = new Vector3(0.47f, 0.015f, 0.47f);
            soil.GetComponent<Renderer>().sharedMaterial = matSoil;

            // 2. Monstera Stems & Broad Heart-Leaves (8 outward-reaching broad leaves)
            int leafCount = 8;
            for (int i = 0; i < leafCount; i++)
            {
                float angle = i * (360f / leafCount) + (i % 2 == 0 ? 8f : -12f);
                float dist = 0.26f + (i % 3) * 0.08f;
                float height = 0.45f + (i * 0.08f);

                // Curved Petiole / Stalk
                GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stalk.name = $"Stalk_{i}";
                stalk.transform.SetParent(plantRoot.transform, false);
                Vector3 stalkMid = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * dist * 0.5f,
                    (0.30f + height) * 0.5f,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * dist * 0.5f
                );
                stalk.transform.localPosition = stalkMid;
                stalk.transform.localScale = new Vector3(0.018f, 0.18f, 0.018f);
                stalk.transform.localRotation = Quaternion.Euler(30f, angle, 0f);
                stalk.GetComponent<Renderer>().sharedMaterial = matStem;
                Object.DestroyImmediate(stalk.GetComponent<Collider>());

                // Broad Monstera Leaf Blade
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"MonsteraLeaf_{i}";
                blade.transform.SetParent(plantRoot.transform, false);
                blade.transform.localPosition = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * dist,
                    height + 0.10f,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * dist
                );
                blade.transform.localRotation = Quaternion.Euler(24f, angle + 15f, 15f);
                blade.transform.localScale = new Vector3(0.28f, 0.007f, 0.36f);
                blade.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? matLeafA : matLeafB;
                Object.DestroyImmediate(blade.GetComponent<Collider>());
            }
        }

        private static void BuildCinemaCornerSpeakers(Transform parent, Material matSpeaker, Material matChassis, Material matChrome, Material matCyan)
        {
            GameObject spkRoot = new GameObject("CinemaSurroundSpeakers");
            spkRoot.transform.SetParent(parent, false);

            // 1 & 2: Massive Floor Standing Stage Subwoofer / Tower Speakers (Front-Left & Front-Right)
            BuildFloorTowerSpeaker(spkRoot.transform, "Speaker_Floor_FL", new Vector3(-4.05f, 0.95f, 3.45f), 15f, matSpeaker, matChassis, matCyan);
            BuildFloorTowerSpeaker(spkRoot.transform, "Speaker_Floor_FR", new Vector3(4.05f, 0.95f, 3.45f), -15f, matSpeaker, matChassis, matCyan);

            // 3 & 4: Top Ceiling/Wall Cinema Surround Speakers (Angled downward toward listener)
            BuildTopSurroundSpeaker(spkRoot.transform, "Speaker_Ceiling_TL", new Vector3(-3.95f, 4.45f, 3.35f), 24f, 18f, matSpeaker, matChassis, matCyan);
            BuildTopSurroundSpeaker(spkRoot.transform, "Speaker_Ceiling_TR", new Vector3(3.95f, 4.45f, 3.35f), 24f, -18f, matSpeaker, matChassis, matCyan);
        }

        private static void BuildFloorTowerSpeaker(Transform parent, string name, Vector3 pos, float yRot, Material matSpeaker, Material matChassis, Material matCyan)
        {
            GameObject spk = new GameObject(name);
            spk.transform.SetParent(parent, false);
            spk.transform.position = pos;
            spk.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // Large Speaker Cabinet (0.65m wide x 1.85m tall x 0.52m deep)
            GameObject cabinet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinet.name = "Cabinet";
            cabinet.transform.SetParent(spk.transform, false);
            cabinet.transform.localScale = new Vector3(0.65f, 1.85f, 0.52f);
            cabinet.GetComponent<Renderer>().sharedMaterial = matChassis;

            // Top High-Frequency Horn Tweeter
            GameObject horn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            horn.name = "HornTweeter";
            horn.transform.SetParent(cabinet.transform, false);
            horn.transform.localPosition = new Vector3(0f, 0.36f, -0.51f);
            horn.transform.localScale = new Vector3(0.75f, 0.16f, 0.05f);
            horn.GetComponent<Renderer>().sharedMaterial = matSpeaker;
            Object.DestroyImmediate(horn.GetComponent<Collider>());

            // Dual 15-inch Subwoofer Drivers with Glowing Cyan Rings
            for (int w = 0; w < 2; w++)
            {
                float yOffset = (w == 0) ? 0.08f : -0.25f;
                GameObject woofer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                woofer.name = $"Subwoofer_{w + 1}";
                woofer.transform.SetParent(cabinet.transform, false);
                woofer.transform.localPosition = new Vector3(0f, yOffset, -0.51f);
                woofer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                woofer.transform.localScale = new Vector3(0.48f, 0.02f, 0.48f);
                woofer.GetComponent<Renderer>().sharedMaterial = matSpeaker;
                Object.DestroyImmediate(woofer.GetComponent<Collider>());

                GameObject glowRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                glowRing.name = "GlowRing";
                glowRing.transform.SetParent(woofer.transform, false);
                glowRing.transform.localPosition = new Vector3(0f, -0.52f, 0f);
                glowRing.transform.localScale = new Vector3(0.85f, 0.06f, 0.85f);
                glowRing.GetComponent<Renderer>().sharedMaterial = matCyan;
                Object.DestroyImmediate(glowRing.GetComponent<Collider>());
            }

            // Base Isolation Plinth
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "BasePlinth";
            plinth.transform.SetParent(cabinet.transform, false);
            plinth.transform.localPosition = new Vector3(0f, -0.51f, 0f);
            plinth.transform.localScale = new Vector3(1.06f, 0.06f, 1.06f);
            plinth.GetComponent<Renderer>().sharedMaterial = matChassis;
            Object.DestroyImmediate(plinth.GetComponent<Collider>());
        }

        private static void BuildTopSurroundSpeaker(Transform parent, string name, Vector3 pos, float xRot, float yRot, Material matSpeaker, Material matChassis, Material matCyan)
        {
            GameObject spk = new GameObject(name);
            spk.transform.SetParent(parent, false);
            spk.transform.position = pos;
            spk.transform.rotation = Quaternion.Euler(xRot, yRot, 0f);

            // Angled Cinema Surround Cabinet (0.55m wide x 0.72m tall x 0.42m deep)
            GameObject cabinet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinet.name = "Cabinet";
            cabinet.transform.SetParent(spk.transform, false);
            cabinet.transform.localScale = new Vector3(0.55f, 0.72f, 0.42f);
            cabinet.GetComponent<Renderer>().sharedMaterial = matChassis;

            // 12-inch Coaxial Driver with Glowing Cyan Ring
            GameObject driver = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            driver.name = "CoaxialDriver";
            driver.transform.SetParent(cabinet.transform, false);
            driver.transform.localPosition = new Vector3(0f, -0.05f, -0.51f);
            driver.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            driver.transform.localScale = new Vector3(0.68f, 0.03f, 0.68f);
            driver.GetComponent<Renderer>().sharedMaterial = matSpeaker;
            Object.DestroyImmediate(driver.GetComponent<Collider>());

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "DriverGlowRing";
            ring.transform.SetParent(driver.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.52f, 0f);
            ring.transform.localScale = new Vector3(0.85f, 0.05f, 0.85f);
            ring.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(ring.GetComponent<Collider>());
        }

        private static void BuildBigCeilingProjector(Transform parent, Material matChassis, Material matChrome, Material matCyan)
        {
            GameObject projObj = new GameObject("CeilingProjector_CommercialIMAX");
            projObj.transform.SetParent(parent, false);
            projObj.transform.position = new Vector3(0f, 4.75f, 1.1f);
            projObj.transform.rotation = Quaternion.Euler(18f, 0f, 0f); // Angled down directly onto the IMAX screen

            // Dual Heavy-Duty Steel Ceiling Drop Arms
            for (int a = -1; a <= 1; a += 2)
            {
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                arm.name = $"MountArm_{(a < 0 ? "L" : "R")}";
                arm.transform.SetParent(parent, false);
                arm.transform.position = new Vector3(a * 0.45f, 5.02f, 1.1f);
                arm.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
                arm.GetComponent<Renderer>().sharedMaterial = matChassis;
                Object.DestroyImmediate(arm.GetComponent<Collider>());
            }

            // Massive Commercial IMAX Projector Chassis (1.4m wide x 0.45m tall x 1.15m deep)
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "ProjectorChassis";
            chassis.transform.SetParent(projObj.transform, false);
            chassis.transform.localScale = new Vector3(1.4f, 0.45f, 1.15f);
            chassis.GetComponent<Renderer>().sharedMaterial = matChassis;

            // Dual Massive Optical Projection Lenses
            for (int l = -1; l <= 1; l += 2)
            {
                GameObject lensBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lensBarrel.name = $"OpticLensBarrel_{(l < 0 ? "L" : "R")}";
                lensBarrel.transform.SetParent(chassis.transform, false);
                lensBarrel.transform.localPosition = new Vector3(l * 0.28f, 0f, 0.52f);
                lensBarrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                lensBarrel.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);
                lensBarrel.GetComponent<Renderer>().sharedMaterial = matChrome;
                Object.DestroyImmediate(lensBarrel.GetComponent<Collider>());

                GameObject lensGlass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lensGlass.name = "LensAperture";
                lensGlass.transform.SetParent(lensBarrel.transform, false);
                lensGlass.transform.localPosition = new Vector3(0f, -0.52f, 0f);
                lensGlass.transform.localScale = new Vector3(0.85f, 0.06f, 0.85f);
                lensGlass.GetComponent<Renderer>().sharedMaterial = matCyan;
                Object.DestroyImmediate(lensGlass.GetComponent<Collider>());
            }

            // Side Ventilation Heatsink Fin Panels with Cyan LED Backlight
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject vent = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vent.name = $"HeatsinkVent_{(s < 0 ? "L" : "R")}";
                vent.transform.SetParent(chassis.transform, false);
                vent.transform.localPosition = new Vector3(s * 0.51f, 0f, 0f);
                vent.transform.localScale = new Vector3(0.04f, 0.55f, 0.85f);
                vent.GetComponent<Renderer>().sharedMaterial = matCyan;
                Object.DestroyImmediate(vent.GetComponent<Collider>());
            }

            // High-Intensity Cinematic Projection SpotLight Beam
            GameObject beamLightObj = new GameObject("ProjectorBeamLight");
            beamLightObj.transform.SetParent(projObj.transform, false);
            beamLightObj.transform.localPosition = new Vector3(0f, 0f, 0.65f);
            Light projLight = beamLightObj.AddComponent<Light>();
            projLight.type = LightType.Spot;
            projLight.range = 10.0f;
            projLight.spotAngle = 78f;
            projLight.innerSpotAngle = 60f;
            projLight.color = new Color(0.45f, 0.82f, 1.0f);
            projLight.intensity = 5.5f;
        }

        private static void BuildLoungeArea(Transform parent, Material matCharcoal, Material matChrome, Material matGlass,
                                           Material matPlatform, Material matCyan, Material matViolet)
        {
            GameObject loungeRoot = new GameObject("LoungeArea_TouchingLeftWall");
            loungeRoot.transform.SetParent(parent, false);
            loungeRoot.transform.position = new Vector3(-3.85f, 0f, 0.0f);
            loungeRoot.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // Facing +X towards room center & window

            // 1. Modern Area Rug
            GameObject rug = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rug.name = "Lounge_Rug";
            rug.transform.SetParent(loungeRoot.transform, false);
            rug.transform.localPosition = new Vector3(0f, 0.005f, 0.45f);
            rug.transform.localScale = new Vector3(2.8f, 0.008f, 2.2f);
            rug.GetComponent<Renderer>().sharedMaterial = matPlatform;

            // 2. Modern Sectional Sofa (Back of sofa sits flush at x = -4.48m, TOUCHING LEFT WALL at x = -4.6m!)
            GameObject sofaRoot = new GameObject("SectionalSofa");
            sofaRoot.transform.SetParent(loungeRoot.transform, false);
            sofaRoot.transform.localPosition = new Vector3(0f, 0f, 0f);

            // Plinth Base with Cyan Accent
            GameObject sofaBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sofaBase.name = "Sofa_Base";
            sofaBase.transform.SetParent(sofaRoot.transform, false);
            sofaBase.transform.localPosition = new Vector3(0f, 0.11f, 0f);
            sofaBase.transform.localScale = new Vector3(2.6f, 0.22f, 0.95f);
            sofaBase.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            GameObject sofaBaseGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sofaBaseGlow.name = "Sofa_Base_Glow";
            sofaBaseGlow.transform.SetParent(sofaBase.transform, false);
            sofaBaseGlow.transform.localPosition = new Vector3(0f, -0.42f, 0.51f);
            sofaBaseGlow.transform.localScale = new Vector3(0.96f, 0.12f, 0.02f);
            sofaBaseGlow.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(sofaBaseGlow.GetComponent<Collider>());

            // 3 Plush Seating Cushions
            for (int c = -1; c <= 1; c++)
            {
                GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seat.name = $"Seat_Cushion_{c + 1}";
                seat.transform.SetParent(sofaRoot.transform, false);
                seat.transform.localPosition = new Vector3(c * 0.80f, 0.28f, 0.06f);
                seat.transform.localScale = new Vector3(0.76f, 0.16f, 0.80f);
                seat.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            }

            // Backrest Frame (Touches the left wall!)
            GameObject backrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backrest.name = "Backrest_Frame";
            backrest.transform.SetParent(sofaRoot.transform, false);
            backrest.transform.localPosition = new Vector3(0f, 0.55f, -0.38f);
            backrest.transform.localScale = new Vector3(2.6f, 0.55f, 0.18f);
            backrest.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // 3 Angled Backrest Pillows
            for (int p = -1; p <= 1; p++)
            {
                GameObject backPillow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backPillow.name = $"Back_Pillow_{p + 1}";
                backPillow.transform.SetParent(sofaRoot.transform, false);
                backPillow.transform.localPosition = new Vector3(p * 0.80f, 0.54f, -0.26f);
                backPillow.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                backPillow.transform.localScale = new Vector3(0.74f, 0.38f, 0.14f);
                backPillow.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            }

            // Left & Right Armrests
            for (int a = -1; a <= 1; a += 2)
            {
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = $"Armrest_{(a < 0 ? "L" : "R")}";
                arm.transform.SetParent(sofaRoot.transform, false);
                arm.transform.localPosition = new Vector3(a * 1.35f, 0.38f, 0.02f);
                arm.transform.localScale = new Vector3(0.20f, 0.36f, 0.95f);
                arm.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            }

            // Throw Pillows
            GameObject pillowCyan = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillowCyan.name = "AccentPillow_Cyan";
            pillowCyan.transform.SetParent(sofaRoot.transform, false);
            pillowCyan.transform.localPosition = new Vector3(-1.18f, 0.42f, -0.15f);
            pillowCyan.transform.localRotation = Quaternion.Euler(0f, 25f, 15f);
            pillowCyan.transform.localScale = new Vector3(0.26f, 0.26f, 0.12f);
            pillowCyan.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(pillowCyan.GetComponent<Collider>());

            GameObject pillowViolet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillowViolet.name = "AccentPillow_Violet";
            pillowViolet.transform.SetParent(sofaRoot.transform, false);
            pillowViolet.transform.localPosition = new Vector3(1.18f, 0.42f, -0.15f);
            pillowViolet.transform.localRotation = Quaternion.Euler(0f, -25f, -15f);
            pillowViolet.transform.localScale = new Vector3(0.26f, 0.26f, 0.12f);
            pillowViolet.GetComponent<Renderer>().sharedMaterial = matViolet;
            Object.DestroyImmediate(pillowViolet.GetComponent<Collider>());

            // 3. Lounge Coffee Table
            GameObject tableRoot = new GameObject("LoungeCoffeeTable");
            tableRoot.transform.SetParent(loungeRoot.transform, false);
            tableRoot.transform.localPosition = new Vector3(0f, 0f, 1.15f);

            // Chrome Legs
            for (int leg = -1; leg <= 1; leg += 2)
            {
                GameObject legObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                legObj.name = $"TableLeg_{(leg < 0 ? "L" : "R")}";
                legObj.transform.SetParent(tableRoot.transform, false);
                legObj.transform.localPosition = new Vector3(leg * 0.65f, 0.18f, 0f);
                legObj.transform.localScale = new Vector3(0.04f, 0.36f, 0.58f);
                legObj.GetComponent<Renderer>().sharedMaterial = matChrome;
                Object.DestroyImmediate(legObj.GetComponent<Collider>());
            }

            // Smoked Glass Top
            GameObject tableFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableFrame.name = "TableFrame";
            tableFrame.transform.SetParent(tableRoot.transform, false);
            tableFrame.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            tableFrame.transform.localScale = new Vector3(1.52f, 0.035f, 0.68f);
            tableFrame.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            GameObject glassTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassTop.name = "SmokedGlassTop";
            glassTop.transform.SetParent(tableRoot.transform, false);
            glassTop.transform.localPosition = new Vector3(0f, 0.405f, 0f);
            glassTop.transform.localScale = new Vector3(1.48f, 0.012f, 0.64f);
            glassTop.GetComponent<Renderer>().sharedMaterial = matGlass;
            Object.DestroyImmediate(glassTop.GetComponent<Collider>());

            // Accessories: Datapad & Cup
            GameObject datapad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            datapad.name = "Datapad";
            datapad.transform.SetParent(tableRoot.transform, false);
            datapad.transform.localPosition = new Vector3(-0.25f, 0.42f, 0.05f);
            datapad.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
            datapad.transform.localScale = new Vector3(0.24f, 0.01f, 0.16f);
            datapad.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            Object.DestroyImmediate(datapad.GetComponent<Collider>());

            GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cup.name = "LoungeCup";
            cup.transform.SetParent(tableRoot.transform, false);
            cup.transform.localPosition = new Vector3(0.35f, 0.445f, -0.08f);
            cup.transform.localScale = new Vector3(0.07f, 0.06f, 0.07f);
            cup.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(cup.GetComponent<Collider>());
        }

        private static void CreateStepTrim(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, List<Renderer> trimList)
        {
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = name;
            trim.transform.SetParent(parent);
            trim.transform.localPosition = localPos;
            trim.transform.localScale = localScale;
            trim.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(trim.GetComponent<Collider>());
            trimList.Add(trim.GetComponent<Renderer>());
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

        private static void BuildTripleMonitorWorkstation(Material matCharcoal, Material matDesk, Material matDeskMat,
                                                         Material matKbBody, Material matKeycaps, Material matSpeaker, Material matPC, Material matChrome,
                                                         Material matCyan, Material matViolet, Material matMagenta, Material matOrange, Material matGreen,
                                                         Material matWhiteDrawer, Material matGlass, Material matAmbientGlow, Material matWarmOrb, Material matAcoustic,
                                                         Material matScreenCenter, Material matScreenLeft, Material matScreenRight,
                                                         List<Renderer> cyanTrims, List<Renderer> violetTrims)
        {
            GameObject oldWork = GameObject.Find("Workstation");
            if (oldWork != null) Object.DestroyImmediate(oldWork);

            GameObject oldShelf = GameObject.Find("FloatingWallShelf");
            if (oldShelf != null) Object.DestroyImmediate(oldShelf);

            GameObject workParent = new GameObject("Workstation");
            workParent.transform.position = Vector3.zero;

            // 1. EXPANDED CURVED COCKPIT DESK
            GameObject deskRoot = new GameObject("Desk");
            deskRoot.transform.SetParent(workParent.transform);
            deskRoot.transform.position = new Vector3(0f, 0f, 0.65f);

            // A. Central Desk Section (1.80m x 0.98m x 0.05m)
            GameObject deskCenter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskCenter.name = "Desk_Center";
            deskCenter.transform.SetParent(deskRoot.transform);
            deskCenter.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            deskCenter.transform.localScale = new Vector3(1.80f, 0.05f, 0.98f);
            deskCenter.GetComponent<Renderer>().sharedMaterial = matDesk;

            // B. Left Wing Desk (Attached straight seamlessly)
            GameObject deskLeftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskLeftWing.name = "Desk_LeftWing";
            deskLeftWing.transform.SetParent(deskRoot.transform);
            deskLeftWing.transform.localPosition = new Vector3(-1.40f, 0.75f, 0f);
            deskLeftWing.transform.localRotation = Quaternion.identity;
            deskLeftWing.transform.localScale = new Vector3(1.05f, 0.05f, 0.98f);
            deskLeftWing.GetComponent<Renderer>().sharedMaterial = matDesk;

            // C. Right Wing Desk (Attached straight seamlessly)
            GameObject deskRightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskRightWing.name = "Desk_RightWing";
            deskRightWing.transform.SetParent(deskRoot.transform);
            deskRightWing.transform.localPosition = new Vector3(1.40f, 0.75f, 0f);
            deskRightWing.transform.localRotation = Quaternion.identity;
            deskRightWing.transform.localScale = new Vector3(1.05f, 0.05f, 0.98f);
            deskRightWing.GetComponent<Renderer>().sharedMaterial = matDesk;

            // Dual White IKEA Alex-Style 5-Drawer Units supporting the wings underneath
            BuildAlexDrawer(deskRoot.transform, "AlexDrawer_Left", new Vector3(-1.40f, 0.36f, 0f), 0f, matWhiteDrawer, matCharcoal);
            BuildAlexDrawer(deskRoot.transform, "AlexDrawer_Right", new Vector3(1.40f, 0.36f, 0f), 0f, matWhiteDrawer, matCharcoal);

            // Underglow LED strips
            CreateDeskEdgeGlow(deskCenter.transform, "Center_Glow_Front", new Vector3(0f, -0.49f, -0.485f), new Vector3(0.98f, 0.15f, 0.03f), matCyan, cyanTrims);
            CreateDeskEdgeGlow(deskLeftWing.transform, "LeftWing_Glow_Front", new Vector3(0f, -0.49f, -0.485f), new Vector3(0.98f, 0.15f, 0.03f), matCyan, cyanTrims);
            CreateDeskEdgeGlow(deskRightWing.transform, "RightWing_Glow_Front", new Vector3(0f, -0.49f, -0.485f), new Vector3(0.98f, 0.15f, 0.03f), matCyan, cyanTrims);

            // Hanging Game Controllers under left front desk edge
            BuildHangingController(deskRoot.transform, "Controller_1", new Vector3(-0.85f, 0.64f, -0.42f), matCharcoal, matCyan);
            BuildHangingController(deskRoot.transform, "Controller_2", new Vector3(-0.65f, 0.64f, -0.42f), matCharcoal, matMagenta);

            // Extra-Wide Gaming Desk Mat (1.20m x 0.46m) centered on desk
            GameObject deskMat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskMat.name = "Desk_Mat";
            deskMat.transform.SetParent(deskRoot.transform);
            deskMat.transform.localPosition = new Vector3(0f, 0.7775f, -0.16f);
            deskMat.transform.localScale = new Vector3(1.20f, 0.005f, 0.46f);
            deskMat.GetComponent<Renderer>().sharedMaterial = matDeskMat;

            // Desk Mat Stitching Accent
            GameObject matTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            matTrim.name = "DeskMat_StitchAccent";
            matTrim.transform.SetParent(deskMat.transform);
            matTrim.transform.localPosition = new Vector3(0f, 0.51f, -0.49f);
            matTrim.transform.localScale = new Vector3(1f, 0.1f, 0.02f);
            matTrim.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(matTrim.GetComponent<Collider>());

            // Mechanical Keyboard with White Keycaps & White Gaming Mouse
            BuildKeyboard(deskRoot.transform, matKbBody, matWhiteDrawer, matCyan, matViolet);
            BuildMouse(deskRoot.transform, matWhiteDrawer, matCyan);

            // Horizontal Gaming Consoles under screens on desk (Xbox One S white & PS4 slim style)
            BuildHorizontalConsoles(deskRoot.transform, matWhiteDrawer, matCharcoal, matCyan);

            // Left Wing Props: Headphone Stand with Headset & Glowing Moon Orb Lamp
            BuildHeadphoneStandAndHeadset(deskRoot.transform, new Vector3(-1.75f, 0.775f, 0.10f), 0f, matCharcoal, matChrome, matCyan);
            BuildMoonLamp(deskRoot.transform, new Vector3(-1.42f, 0.775f, 0.30f), matWarmOrb, matAcoustic);

            // Mini Desk Sign ("HAPPY PEOPLE...")
            BuildDeskSign(deskRoot.transform, new Vector3(-0.55f, 0.775f, 0.22f), matWhiteDrawer);

            // --- HIGH-REALISM PC CPU TOWER (On Right Desk, with ZERO Monitor Collision!) ---
            BuildPCTowerRealistic(deskRoot.transform, new Vector3(1.82f, 1.035f, 0.12f), 0f,
                                  matPC, matGlass, matChrome, matCyan, matMagenta, matViolet);

            // --- INTERACTIVE WORKSTATION GAMING CHAIR (Centered in front of desk) ---
            // Pressing 'E' triggers smooth camera animation into seated computer mode!
            BuildInteractiveChair(workParent.transform, new Vector3(0f, 0.12f, 0.15f), matCharcoal, matChrome, matCyan);

            // Holographic Interaction Prompt Canvas floating directly above desk & chair
            BuildInteractionPromptCanvas(workParent.transform);

            // 2. Physical Three-Monitor Array (Seamless Panoramic curved display)
            GameObject monitorsRoot = new GameObject("TripleMonitorSetup");
            monitorsRoot.transform.SetParent(workParent.transform);
            monitorsRoot.transform.position = new Vector3(0f, 0f, 0.65f);

            // Heavy-duty Triple Monitor Stand Rail (Straight heavy aluminum rail)
            GameObject standRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            standRail.name = "TripleMonitor_MountRail";
            standRail.transform.SetParent(monitorsRoot.transform);
            standRail.transform.localPosition = new Vector3(0f, 1.05f, 0.27f);
            standRail.transform.localScale = new Vector3(3.22f, 0.05f, 0.05f);
            standRail.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(standRail.GetComponent<Collider>());

            GameObject standBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            standBase.name = "TripleMonitor_StandBase";
            standBase.transform.SetParent(monitorsRoot.transform);
            standBase.transform.localPosition = new Vector3(0f, 0.775f, 0.28f);
            standBase.transform.localScale = new Vector3(0.58f, 0.02f, 0.24f);
            standBase.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            Object.DestroyImmediate(standBase.GetComponent<Collider>());

            GameObject standColumn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            standColumn.name = "TripleMonitor_StandColumn";
            standColumn.transform.SetParent(monitorsRoot.transform);
            standColumn.transform.localPosition = new Vector3(0f, 0.92f, 0.28f);
            standColumn.transform.localScale = new Vector3(0.08f, 0.28f, 0.08f);
            standColumn.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(standColumn.GetComponent<Collider>());

            // --- VIBRANT ICE-BLUE / CYAN AMBIENT WALL GLOW HALO BEHIND MONITORS (Straight Halo) ---
            GameObject wallGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallGlow.name = "Ambient_Wall_Glow";
            wallGlow.transform.SetParent(monitorsRoot.transform);
            wallGlow.transform.localPosition = new Vector3(0f, 1.15f, 0.35f);
            wallGlow.transform.localScale = new Vector3(3.26f, 0.06f, 0.02f);
            wallGlow.GetComponent<Renderer>().sharedMaterial = matAmbientGlow;
            Object.DestroyImmediate(wallGlow.GetComponent<Collider>());
            cyanTrims.Add(wallGlow.GetComponent<Renderer>());

            GameObject wallGlowLightObj = new GameObject("Ambient_Wall_Glow_Light");
            wallGlowLightObj.transform.SetParent(monitorsRoot.transform);
            wallGlowLightObj.transform.localPosition = new Vector3(0f, 1.15f, 0.44f);
            Light wallGlowLight = wallGlowLightObj.AddComponent<Light>();
            wallGlowLight.type = LightType.Point;
            wallGlowLight.range = 3.6f;
            wallGlowLight.intensity = 3.4f;
            wallGlowLight.color = new Color(0.0f, 0.65f, 1.0f);

            // A. Center Monitor (Primary Remote Desktop - Straight)
            GameObject centerMon = BuildSingleMonitor(monitorsRoot.transform, "VirtualMonitor", "Screen",
                                                     new Vector3(0f, 1.15f, 0.22f), Quaternion.identity,
                                                     new Vector3(1.06f, 0.62f, 0.04f), matCharcoal, matScreenCenter);

            // Webcam on top center of center monitor
            BuildWebcam(centerMon.transform, matCharcoal, matCyan);

            // Attach RemoteDesktopScreen and VRDesktopInteractable to Center Screen
            var centerScreenQuad = centerMon.transform.Find("MonitorFrame/Screen").gameObject;
            var rdsCenter = centerScreenQuad.AddComponent<RemoteDesktopScreen>();
            var intCenter = centerScreenQuad.AddComponent<VRDesktopInteractable>();

            // B. Left Monitor (Attached seamlessly to left edge of Center Monitor in a straight line)
            GameObject leftMon = BuildSingleMonitor(monitorsRoot.transform, "LeftMonitor", "LeftScreen",
                                                   new Vector3(-1.06f, 1.15f, 0.22f), Quaternion.identity,
                                                   new Vector3(1.06f, 0.62f, 0.04f), matCharcoal, matScreenLeft);
            var leftScreenQuad = leftMon.transform.Find("MonitorFrame/LeftScreen").gameObject;
            var rdsLeft = leftScreenQuad.AddComponent<RemoteDesktopScreen>();
            var auxLeft = leftScreenQuad.AddComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>();
            auxLeft.Configure("LeftDesktopCanvas", "UILeftCamera", new Color(0f, 1f, 1f, 0.95f));

            // C. Right Monitor (Attached seamlessly to right edge of Center Monitor in a straight line)
            GameObject rightMon = BuildSingleMonitor(monitorsRoot.transform, "RightMonitor", "RightScreen",
                                                    new Vector3(1.06f, 1.15f, 0.22f), Quaternion.identity,
                                                    new Vector3(1.06f, 0.62f, 0.04f), matCharcoal, matScreenRight);
            var rightScreenQuad = rightMon.transform.Find("MonitorFrame/RightScreen").gameObject;
            var rdsRight = rightScreenQuad.AddComponent<RemoteDesktopScreen>();
            var auxRight = rightScreenQuad.AddComponent<VRRemoteDesktop.XR.AuxiliaryScreenInteractable>();
            auxRight.Configure("RightDesktopCanvas", "UIRightCamera", new Color(0.85f, 0.4f, 1f, 0.95f));

            // Setup MultiMonitorManager on CoreSystems
            GameObject coreSystems = GameObject.Find("CoreSystems");
            if (coreSystems == null) coreSystems = new GameObject("CoreSystems");
            var mmm = coreSystems.GetComponent<MultiMonitorManager>();
            if (mmm == null) mmm = coreSystems.AddComponent<MultiMonitorManager>();

            SerializedObject mmmSo = new SerializedObject(mmm);
            mmmSo.FindProperty("_centerScreen").objectReferenceValue = rdsCenter;
            mmmSo.FindProperty("_leftScreen").objectReferenceValue = rdsLeft;
            mmmSo.FindProperty("_rightScreen").objectReferenceValue = rdsRight;
            mmmSo.FindProperty("_centerRenderTexture").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Textures/UI_RenderTexture.renderTexture");
            mmmSo.FindProperty("_leftRenderTexture").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Textures/UI_Left_RenderTexture.renderTexture");
            mmmSo.FindProperty("_rightRenderTexture").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Textures/UI_Right_RenderTexture.renderTexture");
            mmmSo.ApplyModifiedProperties();
        }

        private static void BuildInteractiveChair(Transform parent, Vector3 localPos, Material matChassis, Material matChrome, Material matCyan)
        {
            GameObject chairObj = new GameObject("Workstation_GamingChair");
            chairObj.transform.SetParent(parent, false);
            chairObj.transform.localPosition = localPos;
            chairObj.transform.localRotation = Quaternion.identity;

            // 1. Caster Base (5-star wheeled base)
            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "BaseHub";
            hub.transform.SetParent(chairObj.transform, false);
            hub.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            hub.transform.localScale = new Vector3(0.14f, 0.04f, 0.14f);
            hub.GetComponent<Renderer>().sharedMaterial = matChassis;
            Object.DestroyImmediate(hub.GetComponent<Collider>());

            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                GameObject spoke = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spoke.name = $"BaseSpoke_{i}";
                spoke.transform.SetParent(chairObj.transform, false);
                spoke.transform.localPosition = new Vector3(Mathf.Sin(angle) * 0.16f, 0.035f, Mathf.Cos(angle) * 0.16f);
                spoke.transform.localRotation = Quaternion.Euler(0f, i * 72f, 0f);
                spoke.transform.localScale = new Vector3(0.04f, 0.025f, 0.28f);
                spoke.GetComponent<Renderer>().sharedMaterial = matChassis;
                Object.DestroyImmediate(spoke.GetComponent<Collider>());

                // Wheel
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{i}";
                wheel.transform.SetParent(spoke.transform, false);
                wheel.transform.localPosition = new Vector3(0f, -0.4f, 0.45f);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(0.06f, 0.03f, 0.06f);
                wheel.GetComponent<Renderer>().sharedMaterial = matChassis;
                Object.DestroyImmediate(wheel.GetComponent<Collider>());
            }

            // 2. Pneumatic Chrome Gas Cylinder
            GameObject liftPillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            liftPillar.name = "PneumaticCylinder";
            liftPillar.transform.SetParent(chairObj.transform, false);
            liftPillar.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            liftPillar.transform.localScale = new Vector3(0.05f, 0.24f, 0.05f);
            liftPillar.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(liftPillar.GetComponent<Collider>());

            // 3. Thick Contoured Bucket Seat Cushion
            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "SeatCushion";
            seat.transform.SetParent(chairObj.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.44f, 0.02f);
            seat.transform.localScale = new Vector3(0.54f, 0.10f, 0.52f);
            seat.GetComponent<Renderer>().sharedMaterial = matChassis;
            Object.DestroyImmediate(seat.GetComponent<Collider>());

            // Seat Racing Cyan Side Bolsters
            for (int b = -1; b <= 1; b += 2)
            {
                GameObject bolster = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bolster.name = $"SeatBolster_{(b < 0 ? "L" : "R")}";
                bolster.transform.SetParent(seat.transform, false);
                bolster.transform.localPosition = new Vector3(b * 0.48f, 0.35f, 0f);
                bolster.transform.localRotation = Quaternion.Euler(0f, 0f, b * 20f);
                bolster.transform.localScale = new Vector3(0.12f, 0.5f, 0.95f);
                bolster.GetComponent<Renderer>().sharedMaterial = matCyan;
                Object.DestroyImmediate(bolster.GetComponent<Collider>());
            }

            // 4. High Ergonomic Backrest
            GameObject backrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backrest.name = "Backrest";
            backrest.transform.SetParent(chairObj.transform, false);
            backrest.transform.localPosition = new Vector3(0f, 0.88f, -0.22f);
            backrest.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            backrest.transform.localScale = new Vector3(0.50f, 0.76f, 0.08f);
            backrest.GetComponent<Renderer>().sharedMaterial = matChassis;
            Object.DestroyImmediate(backrest.GetComponent<Collider>());

            // Lumbar & Headrest Pillows
            GameObject headrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headrest.name = "HeadrestPillow";
            headrest.transform.SetParent(backrest.transform, false);
            headrest.transform.localPosition = new Vector3(0f, 0.38f, 0.52f);
            headrest.transform.localScale = new Vector3(0.65f, 0.20f, 0.65f);
            headrest.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(headrest.GetComponent<Collider>());

            // 5. 3D Adjustable Armrests
            for (int a = -1; a <= 1; a += 2)
            {
                GameObject armStem = GameObject.CreatePrimitive(PrimitiveType.Cube);
                armStem.name = $"ArmStem_{(a < 0 ? "L" : "R")}";
                armStem.transform.SetParent(chairObj.transform, false);
                armStem.transform.localPosition = new Vector3(a * 0.30f, 0.55f, 0.02f);
                armStem.transform.localScale = new Vector3(0.035f, 0.22f, 0.04f);
                armStem.GetComponent<Renderer>().sharedMaterial = matChrome;
                Object.DestroyImmediate(armStem.GetComponent<Collider>());

                GameObject armPad = GameObject.CreatePrimitive(PrimitiveType.Cube);
                armPad.name = "ArmPad";
                armPad.transform.SetParent(armStem.transform, false);
                armPad.transform.localPosition = new Vector3(0f, 0.52f, 0.05f);
                armPad.transform.localScale = new Vector3(2.5f, 0.15f, 6.5f);
                armPad.GetComponent<Renderer>().sharedMaterial = matChassis;
                Object.DestroyImmediate(armPad.GetComponent<Collider>());
            }

            // Box Collider for interaction detection
            var col = chairObj.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.65f, -0.05f);
            col.size = new Vector3(1.1f, 1.4f, 1.1f);

            // WorkstationChair component handles [E] SIT & USE COMPUTER
            var chairComp = chairObj.AddComponent<WorkstationChair>();
            chairComp.SetPromptAction("SIT & USE COMPUTER");

            // 4. Interactive Robotic Companion ("Bit-Bot") docked right beside the workstation chair
            // Parented under Workstation so it automatically descends during hydraulic theater mode!
            BuildRobotProp(parent, new Vector3(-0.82f, 0.68f, 0.15f), matChassis, matChrome, matCyan, matCyan);
        }

        private static void CreateDeskEdgeGlow(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat, List<Renderer> trimList)
        {
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = name;
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = localPos;
            glow.transform.localScale = localScale;
            glow.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(glow.GetComponent<Collider>());
            trimList.Add(glow.GetComponent<Renderer>());
        }

        private static GameObject BuildSingleMonitor(Transform parent, string rootName, string screenName,
                                                     Vector3 localPos, Quaternion localRot, Vector3 frameScale,
                                                     Material matFrame, Material matScreen)
        {
            GameObject mon = new GameObject(rootName);
            mon.transform.SetParent(parent, false);
            mon.transform.localPosition = localPos;
            mon.transform.localRotation = localRot;

            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "MonitorFrame";
            frame.transform.SetParent(mon.transform, false);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = frameScale;
            frame.GetComponent<Renderer>().sharedMaterial = matFrame;

            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = screenName;
            screen.transform.SetParent(frame.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0f, -0.51f);
            screen.transform.localScale = new Vector3(0.975f, 0.955f, 1f);
            screen.GetComponent<Renderer>().sharedMaterial = matScreen;

            return mon;
        }

        private static void BuildAlexDrawer(Transform parent, string name, Vector3 localPos, float yRot, Material matWhite, Material matHandle)
        {
            GameObject drawerObj = new GameObject(name);
            drawerObj.transform.SetParent(parent, false);
            drawerObj.transform.localPosition = localPos;
            drawerObj.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            // Main white pedestal body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(drawerObj.transform, false);
            body.transform.localScale = new Vector3(0.38f, 0.72f, 0.76f);
            body.GetComponent<Renderer>().sharedMaterial = matWhite;

            // 5 Horizontal drawer pull slot accents
            float startY = 0.25f;
            float stepY = 0.125f;
            for (int i = 0; i < 5; i++)
            {
                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handle.name = $"Drawer_Handle_{i}";
                handle.transform.SetParent(drawerObj.transform, false);
                handle.transform.localPosition = new Vector3(0f, startY - (i * stepY), -0.385f);
                handle.transform.localScale = new Vector3(0.24f, 0.012f, 0.015f);
                handle.GetComponent<Renderer>().sharedMaterial = matHandle;
                Object.DestroyImmediate(handle.GetComponent<Collider>());
            }
        }

        private static void BuildHangingController(Transform parent, string name, Vector3 localPos, Material matBody, Material matGlow)
        {
            GameObject ctrl = new GameObject(name);
            ctrl.transform.SetParent(parent, false);
            ctrl.transform.localPosition = localPos;
            ctrl.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);

            // Mount bracket hook under desk
            GameObject hook = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hook.name = "MountHook";
            hook.transform.SetParent(ctrl.transform, false);
            hook.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            hook.transform.localScale = new Vector3(0.03f, 0.08f, 0.03f);
            hook.GetComponent<Renderer>().sharedMaterial = matBody;
            Object.DestroyImmediate(hook.GetComponent<Collider>());

            // Gamepad body
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "GamepadBody";
            pad.transform.SetParent(ctrl.transform, false);
            pad.transform.localScale = new Vector3(0.13f, 0.045f, 0.09f);
            pad.GetComponent<Renderer>().sharedMaterial = matBody;
            Object.DestroyImmediate(pad.GetComponent<Collider>());

            // Thumbsticks
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stick.name = $"Thumbstick_{i}";
                stick.transform.SetParent(ctrl.transform, false);
                stick.transform.localPosition = new Vector3(i * 0.035f, 0.025f, -0.015f);
                stick.transform.localScale = new Vector3(0.022f, 0.012f, 0.022f);
                stick.GetComponent<Renderer>().sharedMaterial = matGlow;
                Object.DestroyImmediate(stick.GetComponent<Collider>());
            }
        }

        private static void BuildHorizontalConsoles(Transform parent, Material matWhite, Material matBlack, Material matCyan)
        {
            GameObject consolesRoot = new GameObject("ConsolesUnderMonitors");
            consolesRoot.transform.SetParent(parent, false);

            // White Xbox One S style console on the left under center monitor
            GameObject xbox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            xbox.name = "Console_XboxStyle";
            xbox.transform.SetParent(consolesRoot.transform, false);
            xbox.transform.localPosition = new Vector3(-0.20f, 0.795f, 0.22f);
            xbox.transform.localScale = new Vector3(0.30f, 0.042f, 0.22f);
            xbox.GetComponent<Renderer>().sharedMaterial = matWhite;
            Object.DestroyImmediate(xbox.GetComponent<Collider>());

            // Circular cooling grill recess
            GameObject grill = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grill.name = "CoolingGrill";
            grill.transform.SetParent(xbox.transform, false);
            grill.transform.localPosition = new Vector3(0.24f, 0.52f, 0f);
            grill.transform.localScale = new Vector3(0.35f, 0.02f, 0.45f);
            grill.GetComponent<Renderer>().sharedMaterial = matBlack;
            Object.DestroyImmediate(grill.GetComponent<Collider>());

            // Power LED dot
            GameObject powerLed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            powerLed.name = "PowerLED";
            powerLed.transform.SetParent(xbox.transform, false);
            powerLed.transform.localPosition = new Vector3(-0.35f, 0f, -0.51f);
            powerLed.transform.localScale = new Vector3(0.04f, 0.2f, 0.04f);
            powerLed.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(powerLed.GetComponent<Collider>());

            // Slim PS4 style console on the right under center monitor
            GameObject ps4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ps4.name = "Console_PS4Style";
            ps4.transform.SetParent(consolesRoot.transform, false);
            ps4.transform.localPosition = new Vector3(0.20f, 0.792f, 0.22f);
            ps4.transform.localScale = new Vector3(0.28f, 0.038f, 0.24f);
            ps4.GetComponent<Renderer>().sharedMaterial = matBlack;
            Object.DestroyImmediate(ps4.GetComponent<Collider>());

            // Glowing blue center seam stripe
            GameObject ps4Seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ps4Seam.name = "LightStripe";
            ps4Seam.transform.SetParent(ps4.transform, false);
            ps4Seam.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            ps4Seam.transform.localScale = new Vector3(0.02f, 0.02f, 1.0f);
            ps4Seam.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(ps4Seam.GetComponent<Collider>());
        }

        private static void BuildHeadphoneStandAndHeadset(Transform parent, Vector3 localPos, float yRot, Material matBody, Material matChrome, Material matCyan)
        {
            GameObject hpRoot = new GameObject("HeadphoneStand");
            hpRoot.transform.SetParent(parent, false);
            hpRoot.transform.localPosition = localPos;
            hpRoot.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            // Stand Base
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            b.name = "Base";
            b.transform.SetParent(hpRoot.transform, false);
            b.transform.localScale = new Vector3(0.12f, 0.012f, 0.12f);
            b.GetComponent<Renderer>().sharedMaterial = matBody;
            Object.DestroyImmediate(b.GetComponent<Collider>());

            // Base Glowing LED Ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "BaseGlowRing";
            ring.transform.SetParent(hpRoot.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            ring.transform.localScale = new Vector3(0.122f, 0.003f, 0.122f);
            ring.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            // Stand Stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stem.name = "Stem";
            stem.transform.SetParent(hpRoot.transform, false);
            stem.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            stem.transform.localScale = new Vector3(0.022f, 0.25f, 0.022f);
            stem.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(stem.GetComponent<Collider>());

            // Top Rest Cradle
            GameObject cradle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cradle.name = "TopRest";
            cradle.transform.SetParent(hpRoot.transform, false);
            cradle.transform.localPosition = new Vector3(0f, 0.255f, 0f);
            cradle.transform.localScale = new Vector3(0.10f, 0.015f, 0.045f);
            cradle.GetComponent<Renderer>().sharedMaterial = matBody;
            Object.DestroyImmediate(cradle.GetComponent<Collider>());

            // Gaming Headset on the Stand
            GameObject headset = new GameObject("GamingHeadset");
            headset.transform.SetParent(hpRoot.transform, false);
            headset.transform.localPosition = new Vector3(0f, 0.24f, 0f);

            // Headband
            GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band.name = "Headband";
            band.transform.SetParent(headset.transform, false);
            band.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            band.transform.localScale = new Vector3(0.12f, 0.015f, 0.035f);
            band.GetComponent<Renderer>().sharedMaterial = matBody;
            Object.DestroyImmediate(band.GetComponent<Collider>());

            // Left & Right Earcups
            for (int e = -1; e <= 1; e += 2)
            {
                GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cup.name = $"Earcup_{(e < 0 ? "L" : "R")}";
                cup.transform.SetParent(headset.transform, false);
                cup.transform.localPosition = new Vector3(e * 0.065f, -0.05f, 0f);
                cup.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                cup.transform.localScale = new Vector3(0.065f, 0.025f, 0.065f);
                cup.GetComponent<Renderer>().sharedMaterial = matBody;
                Object.DestroyImmediate(cup.GetComponent<Collider>());

                // Glowing Cyan Accent Ring on Cup
                GameObject cupGlow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cupGlow.name = "GlowRing";
                cupGlow.transform.SetParent(cup.transform, false);
                cupGlow.transform.localPosition = new Vector3(0f, -0.51f, 0f);
                cupGlow.transform.localScale = new Vector3(0.85f, 0.05f, 0.85f);
                cupGlow.GetComponent<Renderer>().sharedMaterial = matCyan;
                Object.DestroyImmediate(cupGlow.GetComponent<Collider>());
            }
        }

        private static void BuildMoonLamp(Transform parent, Vector3 localPos, Material matWarmOrb, Material matWood)
        {
            GameObject lamp = new GameObject("MoonLamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = localPos;

            // Wooden Geometric Tripod Stand
            for (int leg = 0; leg < 3; leg++)
            {
                float angle = leg * 120f * Mathf.Deg2Rad;
                GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stick.name = $"StandLeg_{leg}";
                stick.transform.SetParent(lamp.transform, false);
                stick.transform.localPosition = new Vector3(Mathf.Sin(angle) * 0.035f, 0.025f, Mathf.Cos(angle) * 0.035f);
                stick.transform.localRotation = Quaternion.Euler(-15f * Mathf.Cos(angle), 0f, 15f * Mathf.Sin(angle));
                stick.transform.localScale = new Vector3(0.012f, 0.065f, 0.012f);
                stick.GetComponent<Renderer>().sharedMaterial = matWood;
                Object.DestroyImmediate(stick.GetComponent<Collider>());
            }

            // Glowing Spherical Moon
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "MoonOrb";
            orb.transform.SetParent(lamp.transform, false);
            orb.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            orb.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            orb.GetComponent<Renderer>().sharedMaterial = matWarmOrb;
            Object.DestroyImmediate(orb.GetComponent<Collider>());

            // Warm soft Point Light
            GameObject lightObj = new GameObject("MoonLight");
            lightObj.transform.SetParent(lamp.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.range = 1.2f;
            pLight.intensity = 1.6f;
            pLight.color = new Color(1.0f, 0.94f, 0.82f);
        }

        private static void BuildDeskSign(Transform parent, Vector3 localPos, Material matWhite)
        {
            GameObject sign = new GameObject("DeskSign");
            sign.transform.SetParent(parent, false);
            sign.transform.localPosition = localPos;

            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "SignBlock";
            block.transform.SetParent(sign.transform, false);
            block.transform.localScale = new Vector3(0.18f, 0.065f, 0.025f);
            block.GetComponent<Renderer>().sharedMaterial = matWhite;
            Object.DestroyImmediate(block.GetComponent<Collider>());

            GameObject textObj = new GameObject("SignText", typeof(TextMeshPro));
            textObj.transform.SetParent(sign.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.014f);
            var tmp = textObj.GetComponent<TextMeshPro>();
            tmp.text = "<size=0.012><color=#222222><b>HAPPY PEOPLE DON'T\nHAVE THE BEST OF THINGS\nTHEY MAKE\nTHE BEST OF THINGS</b></color></size>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 0.55f;
        }

        private static void BuildWebcam(Transform centerMonTransform, Material matBody, Material matCyan)
        {
            GameObject camObj = new GameObject("Webcam");
            camObj.transform.SetParent(centerMonTransform, false);
            camObj.transform.localPosition = new Vector3(0f, 0.33f, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(camObj.transform, false);
            body.transform.localScale = new Vector3(0.10f, 0.03f, 0.035f);
            body.GetComponent<Renderer>().sharedMaterial = matBody;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Lens circle
            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lens.name = "Lens";
            lens.transform.SetParent(camObj.transform, false);
            lens.transform.localPosition = new Vector3(0f, 0f, -0.018f);
            lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lens.transform.localScale = new Vector3(0.016f, 0.005f, 0.016f);
            lens.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(lens.GetComponent<Collider>());
        }

        private static void BuildPCTowerRealistic(Transform parent, Vector3 localPos, float yRot,
                                                  Material matPC, Material matGlass, Material matChrome,
                                                  Material matCyan, Material matMagenta, Material matViolet)
        {
            GameObject pcRoot = new GameObject("PC_Tower_Battlestation");
            pcRoot.transform.SetParent(parent, false);
            pcRoot.transform.localPosition = localPos;
            pcRoot.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);

            var pcController = pcRoot.AddComponent<RealisticPCTowerController>();
            pcController.BuildRealisticChassis(matPC, matGlass, matChrome, matCyan, matMagenta, matViolet);
        }

        private static void BuildKeyboard(Transform parent, Material matBody, Material matKeycaps, Material matCyan, Material matViolet)
        {
            GameObject kbRoot = new GameObject("Keyboard");
            kbRoot.transform.SetParent(parent, false);
            kbRoot.transform.localPosition = new Vector3(-0.06f, 0.782f, -0.16f);
            kbRoot.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Keyboard_Body";
            body.transform.SetParent(kbRoot.transform, false);
            body.transform.localScale = new Vector3(0.46f, 0.015f, 0.16f);
            body.GetComponent<Renderer>().sharedMaterial = matBody;

            // White Keycaps bed
            GameObject keys = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keys.name = "White_Keycaps";
            keys.transform.SetParent(kbRoot.transform, false);
            keys.transform.localPosition = new Vector3(0f, 0.009f, 0f);
            keys.transform.localScale = new Vector3(0.44f, 0.008f, 0.145f);
            keys.GetComponent<Renderer>().sharedMaterial = matKeycaps;
            Object.DestroyImmediate(keys.GetComponent<Collider>());

            GameObject underglow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            underglow.name = "Underglow";
            underglow.transform.SetParent(kbRoot.transform, false);
            underglow.transform.localPosition = new Vector3(0f, -0.007f, -0.075f);
            underglow.transform.localScale = new Vector3(0.45f, 0.004f, 0.01f);
            underglow.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(underglow.GetComponent<Collider>());
        }

        private static void BuildMouse(Transform parent, Material matBodyWhite, Material matCyan)
        {
            GameObject mouseRoot = new GameObject("Mouse");
            mouseRoot.transform.SetParent(parent, false);
            mouseRoot.transform.localPosition = new Vector3(0.35f, 0.783f, -0.16f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Mouse_Body";
            body.transform.SetParent(mouseRoot.transform, false);
            body.transform.localScale = new Vector3(0.075f, 0.024f, 0.125f);
            body.GetComponent<Renderer>().sharedMaterial = matBodyWhite;

            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "ScrollWheel";
            wheel.transform.SetParent(mouseRoot.transform, false);
            wheel.transform.localPosition = new Vector3(0f, 0.014f, 0.026f);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.014f, 0.006f, 0.014f);
            wheel.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(wheel.GetComponent<Collider>());
        }

        private static void BuildAuxiliaryDashboards()
        {
            // Build Left & Right Dashboard UI Canvases and isolated Cameras
            // A. Left Dashboard (Telemetry & Diagnostics)
            EnsureDashboardSystem("LeftDesktopCanvas", "UILeftCamera", new Vector3(-100f, 1000f, 0f),
                                  "Assets/Textures/UI_Left_RenderTexture.renderTexture", true);

            // B. Right Dashboard (Network Operations & Feed)
            EnsureDashboardSystem("RightDesktopCanvas", "UIRightCamera", new Vector3(100f, 1000f, 0f),
                                  "Assets/Textures/UI_Right_RenderTexture.renderTexture", false);
        }

        private static void EnsureDashboardSystem(string canvasName, string camName, Vector3 camPos, string rtPath, bool isLeft)
        {
            GameObject oldCam = GameObject.Find(camName);
            if (oldCam != null) Object.DestroyImmediate(oldCam);

            GameObject camObj = new GameObject(camName);
            camObj.transform.position = camPos;

            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.035f, 0.06f, 1f);
            cam.cullingMask = 1 << LayerMask.NameToLayer("UI");
            cam.targetTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);

            // Clean old Canvas
            GameObject oldCanvas = GameObject.Find(canvasName);
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            GameObject canvasObj = new GameObject(canvasName);
            canvasObj.layer = LayerMask.NameToLayer("UI");
            canvasObj.transform.SetParent(camObj.transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 100f;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            if (isLeft)
            {
                BuildLeftDashboardUIContent(canvasObj);
            }
            else
            {
                BuildRightDashboardUIContent(canvasObj);
            }
        }

        private static void BuildLeftDashboardUIContent(GameObject root)
        {
            var dash = root.GetComponent<LeftDashboardUI>();
            if (dash == null) dash = root.AddComponent<LeftDashboardUI>();
            dash.BuildDashboardUI();
        }

        private static void BuildRightDashboardUIContent(GameObject root)
        {
            var dash = root.GetComponent<RightDashboardUI>();
            if (dash == null) dash = root.AddComponent<RightDashboardUI>();
            dash.BuildDashboardUI();
        }

        private static void CreateTelemetryCard(GameObject parent, string name, Vector2 aMin, Vector2 aMax, string title, out TextMeshProUGUI bodyTmp)
        {
            GameObject cObj = new GameObject(name, typeof(RectTransform), typeof(Image));
            cObj.transform.SetParent(parent.transform, false);
            RectTransform rt = cObj.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            cObj.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.09f, 0.92f);

            GameObject hObj = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            hObj.transform.SetParent(cObj.transform, false);
            RectTransform hrt = hObj.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -8f);
            hrt.sizeDelta = new Vector2(-28f, 32f);

            var hTmp = hObj.GetComponent<TextMeshProUGUI>();
            hTmp.text = $"<color=#00ffff><b>{title}</b></color>";
            hTmp.fontSize = 18;
            hTmp.alignment = TextAlignmentOptions.MidlineLeft;

            GameObject bObj = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bObj.transform.SetParent(cObj.transform, false);
            RectTransform brt = bObj.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(16f, 12f);
            brt.offsetMax = new Vector2(-16f, -44f);

            bodyTmp = bObj.GetComponent<TextMeshProUGUI>();
            bodyTmp.fontSize = 15;
            bodyTmp.alignment = TextAlignmentOptions.TopLeft;
            bodyTmp.lineSpacing = 14f;
            bodyTmp.text = "Initializing telemetry stream...";
        }

        private static void BuildInteractiveProps(Material matCharcoal, Material matChrome, Material matCoffee,
                                                  Material matCyan, Material matViolet, Material matMagenta, Material matOrange, Material matGreen, Material matHolo, Material matAcoustic)
        {
            GameObject oldProps = GameObject.Find("InteractiveProps");
            if (oldProps != null) Object.DestroyImmediate(oldProps);

            GameObject propsRoot = new GameObject("InteractiveProps");

            // Prop A: Barista Espresso Synthesizer Station (Far right corner of window wall, facing front!)
            BuildCoffeeProp(propsRoot.transform, new Vector3(4.90f, 0.85f, -4.30f), 180f, matCoffee, matChrome, matCyan, matOrange, matAcoustic);

            // Prop A2: Ceremonial Zen Tea Station (Matching station adjacent on beverage counter)
            BuildTeaProp(propsRoot.transform, new Vector3(3.70f, 0.85f, -4.30f), 180f, matCoffee, matChrome, matGreen, matOrange, matAcoustic);

            // Prop B: Central Hologram Pedestal (Moved back to z = -4.05m behind VIP sofa with open walkways)
            BuildHologramProp(propsRoot.transform, new Vector3(0f, 0f, -4.05f), matCharcoal, matChrome, matHolo, matCyan);

            // Prop C: VIP Theater Viewing Sofa with built-in console buttons
            BuildVIPTheaterSofa(propsRoot.transform, matCharcoal, matChrome, matCyan, matViolet, matMagenta, matGreen);

            // Prop D: Achievement Trophy Board on Back Wall (Flush on back wall at z = -4.85m)
            BuildAchievementBoard(propsRoot.transform, new Vector3(0f, 2.2f, -4.85f), matCharcoal, matCyan, matGreen);

            // Prop E: Interactive Weather Station Console (Mounted on wall beside observation window at x = 5.70f, y = 1.45f, z = 2.95f)
            BuildWeatherStationProp(propsRoot.transform, new Vector3(5.70f, 1.45f, 2.95f), -90f, matCharcoal, matChrome, matOrange, matCyan);
        }

        private static void BuildRobotProp(Transform parent, Vector3 pos, Material matBody, Material matChrome, Material matCyan, Material matMagenta)
        {
            GameObject bot = new GameObject("RobotAssistant");
            bot.transform.SetParent(parent, false);
            bot.transform.position = pos;

            // Small Table Pedestal
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = "TablePedestal";
            table.transform.SetParent(bot.transform, false);
            table.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            table.transform.localScale = new Vector3(0.35f, 0.42f, 0.35f);
            table.GetComponent<Renderer>().sharedMaterial = matBody;

            // Bot Root
            GameObject botModel = new GameObject("BotModel");
            botModel.transform.SetParent(bot.transform, false);
            botModel.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            // Sphere Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Body";
            body.transform.SetParent(botModel.transform, false);
            body.transform.localScale = new Vector3(0.24f, 0.28f, 0.24f);
            body.GetComponent<Renderer>().sharedMaterial = matBody;

            // Floating Magnetic Thruster Ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "MagneticRing";
            ring.transform.SetParent(botModel.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            ring.transform.localScale = new Vector3(0.22f, 0.015f, 0.22f);
            ring.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            // Articulated Head
            GameObject head = new GameObject("Head");
            head.transform.SetParent(botModel.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.18f, 0f);

            GameObject headMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headMesh.name = "HeadMesh";
            headMesh.transform.SetParent(head.transform, false);
            headMesh.transform.localScale = new Vector3(0.20f, 0.16f, 0.20f);
            headMesh.GetComponent<Renderer>().sharedMaterial = matBody;

            // Glowing Visor Eyes (Quad)
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visor.name = "VisorEyes";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.01f, -0.101f);
            visor.transform.localScale = new Vector3(0.12f, 0.05f, 1f);
            visor.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(visor.GetComponent<Collider>());

            // Antenna
            GameObject ant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ant.name = "Antenna";
            ant.transform.SetParent(head.transform, false);
            ant.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            ant.transform.localScale = new Vector3(0.008f, 0.06f, 0.008f);
            ant.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(ant.GetComponent<Collider>());

            // Add Collider to bot for raycasting
            var col = bot.AddComponent<SphereCollider>();
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.15f, 0f);

            // Add RobotAssistant component
            var assistant = bot.AddComponent<RobotAssistant>();
            SerializedObject so = new SerializedObject(assistant);
            so.FindProperty("_headTransform").objectReferenceValue = head.transform;
            so.FindProperty("_bodyTransform").objectReferenceValue = botModel.transform;
            so.FindProperty("_eyeVisorRenderer").objectReferenceValue = visor.GetComponent<Renderer>();
            so.ApplyModifiedProperties();
        }

        private static void BuildCoffeeProp(Transform parent, Vector3 pos, float yRot, Material matCoffee, Material matChrome, Material matCyan, Material matOrange, Material matWood)
        {
            GameObject coffeeObj = new GameObject("CoffeeStation");
            coffeeObj.transform.SetParent(parent, false);
            coffeeObj.transform.position = pos;
            coffeeObj.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // Counter Side Table with Wood Slats
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            counter.transform.SetParent(coffeeObj.transform, false);
            counter.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            counter.transform.localScale = new Vector3(1.2f, 0.84f, 0.70f);
            counter.GetComponent<Renderer>().sharedMaterial = matCoffee;

            // Commercial Barista Espresso Machine Chassis
            GameObject machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machine.name = "EspressoMachineHousing";
            machine.transform.SetParent(coffeeObj.transform, false);
            machine.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            machine.transform.localScale = new Vector3(0.56f, 0.46f, 0.40f);
            machine.GetComponent<Renderer>().sharedMaterial = matChrome;

            // Top Cup Warming Rack
            GameObject rack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rack.name = "CupWarmingRack";
            rack.transform.SetParent(machine.transform, false);
            rack.transform.localPosition = new Vector3(0f, 0.51f, 0f);
            rack.transform.localScale = new Vector3(0.92f, 0.04f, 0.88f);
            rack.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(rack.GetComponent<Collider>());

            // Dual Clear Bean Hoppers on top
            for (int h = -1; h <= 1; h += 2)
            {
                GameObject hopper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hopper.name = $"BeanHopper_{h}";
                hopper.transform.SetParent(machine.transform, false);
                hopper.transform.localPosition = new Vector3(h * 0.32f, 0.72f, 0.15f);
                hopper.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
                hopper.GetComponent<Renderer>().sharedMaterial = matCoffee;
                Object.DestroyImmediate(hopper.GetComponent<Collider>());
            }

            // Dual Chrome Group Heads & Portafilter Handles
            for (int g = -1; g <= 1; g += 2)
            {
                GameObject groupHead = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                groupHead.name = $"GroupHead_{g}";
                groupHead.transform.SetParent(machine.transform, false);
                groupHead.transform.localPosition = new Vector3(g * 0.25f, -0.05f, -0.51f);
                groupHead.transform.localScale = new Vector3(0.14f, 0.08f, 0.14f);
                groupHead.GetComponent<Renderer>().sharedMaterial = matChrome;
                Object.DestroyImmediate(groupHead.GetComponent<Collider>());

                GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                handle.name = "PortafilterHandle";
                handle.transform.SetParent(groupHead.transform, false);
                handle.transform.localPosition = new Vector3(0f, -0.2f, -0.8f);
                handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                handle.transform.localScale = new Vector3(0.25f, 0.7f, 0.25f);
                handle.GetComponent<Renderer>().sharedMaterial = matCoffee;
                Object.DestroyImmediate(handle.GetComponent<Collider>());
            }

            // Chrome Dispensing Nozzle (central)
            GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nozzle.name = "DispenseNozzle";
            nozzle.transform.SetParent(coffeeObj.transform, false);
            nozzle.transform.localPosition = new Vector3(0f, 0.14f, -0.22f);
            nozzle.transform.localScale = new Vector3(0.04f, 0.03f, 0.04f);
            nozzle.GetComponent<Renderer>().sharedMaterial = matOrange;
            Object.DestroyImmediate(nozzle.GetComponent<Collider>());

            // Chrome Drip Tray
            GameObject dripTray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dripTray.name = "DripTray";
            dripTray.transform.SetParent(coffeeObj.transform, false);
            dripTray.transform.localPosition = new Vector3(0f, 0.035f, -0.22f);
            dripTray.transform.localScale = new Vector3(0.52f, 0.05f, 0.22f);
            dripTray.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(dripTray.GetComponent<Collider>());

            // Quantum Espresso Mug on Drip Tray
            GameObject mug = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mug.name = "SpawnedCup";
            mug.transform.SetParent(coffeeObj.transform, false);
            mug.transform.localPosition = new Vector3(0f, 0.09f, -0.22f);
            mug.transform.localScale = new Vector3(0.08f, 0.055f, 0.08f);
            mug.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(mug.GetComponent<Collider>());

            // Brew Light
            GameObject lightObj = new GameObject("BrewLight");
            lightObj.transform.SetParent(coffeeObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.22f, -0.24f);
            Light brewLight = lightObj.AddComponent<Light>();
            brewLight.type = LightType.Point;
            brewLight.range = 0.9f;
            brewLight.intensity = 2.2f;
            brewLight.color = new Color(1f, 0.6f, 0.1f);
            brewLight.enabled = false;

            // Box Collider
            var col = coffeeObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.7f, 0.8f, 0.7f);
            col.center = new Vector3(0f, 0.25f, 0f);

            // Front Status OLED Display
            GameObject screenBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screenBase.name = "CoffeeScreenBase";
            screenBase.transform.SetParent(coffeeObj.transform, false);
            screenBase.transform.localPosition = new Vector3(0f, 0.38f, -0.21f);
            screenBase.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            screenBase.transform.localScale = new Vector3(0.28f, 0.12f, 0.02f);
            screenBase.GetComponent<Renderer>().sharedMaterial = matCoffee;
            Object.DestroyImmediate(screenBase.GetComponent<Collider>());

            GameObject textObj = new GameObject("ScreenStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(screenBase.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            textObj.transform.localRotation = Quaternion.identity;
            RectTransform srt = textObj.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(280f, 120f);
            srt.localScale = Vector3.one * 0.001f;
            var stmp = textObj.GetComponent<TextMeshProUGUI>();
            stmp.text = "<color=#ffaa00><b>QUANTUM BREW v3.4</b></color>\nSTATUS: STANDBY";
            stmp.fontSize = 20;
            stmp.alignment = TextAlignmentOptions.Center;
            stmp.color = Color.white;

            var comp = coffeeObj.AddComponent<CoffeeMachineController>();
            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("_nozzleRenderer").objectReferenceValue = nozzle.GetComponent<Renderer>();
            so.FindProperty("_spawnedCup").objectReferenceValue = mug;
            so.FindProperty("_brewLight").objectReferenceValue = brewLight;
            so.FindProperty("_screenStatusText").objectReferenceValue = stmp;
            so.ApplyModifiedProperties();
        }

        private static void BuildTeaProp(Transform parent, Vector3 pos, float yRot, Material matCoffee, Material matChrome, Material matGreen, Material matOrange, Material matWood)
        {
            TeaStationController.BuildRuntimeTeaStation(parent, pos, Quaternion.Euler(0f, yRot, 0f));
        }

        private static void BuildButtonPanelProp(Transform parent, Vector3 pos, Material matBody, Material matCyan, Material matMagenta, Material matGreen, Material matViolet)
        {
            GameObject console = new GameObject("ButtonConsole");
            console.transform.SetParent(parent, false);
            console.transform.localPosition = pos;
            console.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

            GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "ConsoleHousing";
            housing.transform.SetParent(console.transform, false);
            housing.transform.localPosition = Vector3.zero;
            housing.transform.localScale = new Vector3(0.22f, 0.05f, 0.18f);
            housing.GetComponent<Renderer>().sharedMaterial = matBody;

            // Glowing cyan border trim
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "ConsoleTrim";
            trim.transform.SetParent(console.transform, false);
            trim.transform.localPosition = new Vector3(0f, 0.026f, 0f);
            trim.transform.localScale = new Vector3(0.23f, 0.008f, 0.19f);
            trim.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(trim.GetComponent<Collider>());

            Material[] btnMats = new Material[] { matCyan, matMagenta, matGreen, matViolet };
            Transform[] btnTransforms = new Transform[4];

            float[] xOffsets = new float[] { -0.065f, -0.022f, 0.022f, 0.065f };
            for (int i = 0; i < 4; i++)
            {
                GameObject btn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                btn.name = $"Button_{i}";
                btn.transform.SetParent(console.transform, false);
                btn.transform.localPosition = new Vector3(xOffsets[i], 0.035f, 0f);
                btn.transform.localScale = new Vector3(0.035f, 0.022f, 0.035f);
                btn.GetComponent<Renderer>().sharedMaterial = btnMats[i];
                Object.DestroyImmediate(btn.GetComponent<Collider>());
                btnTransforms[i] = btn.transform;
            }

            var col = console.AddComponent<BoxCollider>();
            col.size = new Vector3(0.24f, 0.14f, 0.20f);
            col.center = new Vector3(0f, 0.04f, 0f);

            var panel = console.AddComponent<InteractiveButtonPanel>();
            panel.SetPromptAction("PRESS CONSOLE");
            SerializedObject so = new SerializedObject(panel);
            var propArray = so.FindProperty("_buttonTransforms");
            propArray.arraySize = 4;
            for (int i = 0; i < 4; i++) propArray.GetArrayElementAtIndex(i).objectReferenceValue = btnTransforms[i];
            var rangeProp = so.FindProperty("_interactionRange");
            if (rangeProp != null) rangeProp.floatValue = 2.4f;
            var offsetProp = so.FindProperty("_promptOffset");
            if (offsetProp != null) offsetProp.vector3Value = new Vector3(0f, 0.16f, 0f);
            so.ApplyModifiedProperties();
        }

        private static void BuildHologramProp(Transform parent, Vector3 pos, Material matBase, Material matChrome, Material matHolo, Material matCyan)
        {
            GameObject holo = new GameObject("CentralHologram");
            holo.transform.SetParent(parent, false);
            holo.transform.position = pos;

            // Emitter Pedestal
            GameObject basePedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePedestal.name = "EmitterPedestal";
            basePedestal.transform.SetParent(holo.transform, false);
            basePedestal.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            basePedestal.transform.localScale = new Vector3(1.1f, 0.45f, 1.1f);
            basePedestal.GetComponent<Renderer>().sharedMaterial = matBase;

            GameObject emitterRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            emitterRing.name = "EmitterRing";
            emitterRing.transform.SetParent(holo.transform, false);
            emitterRing.transform.localPosition = new Vector3(0f, 0.91f, 0f);
            emitterRing.transform.localScale = new Vector3(0.85f, 0.015f, 0.85f);
            emitterRing.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(emitterRing.GetComponent<Collider>());

            // Hologram Elements Container
            GameObject elements = new GameObject("HologramElements");
            elements.transform.SetParent(holo.transform, false);
            elements.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            // Core Pivot
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "HoloCore";
            core.transform.SetParent(elements.transform, false);
            core.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            core.GetComponent<Renderer>().sharedMaterial = matHolo;
            Object.DestroyImmediate(core.GetComponent<Collider>());

            // Orbit Ring 1
            GameObject r1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            r1.name = "OrbitRing1";
            r1.transform.SetParent(elements.transform, false);
            r1.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            r1.transform.localScale = new Vector3(1.15f, 0.008f, 1.15f);
            r1.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(r1.GetComponent<Collider>());

            // Orbit Ring 2
            GameObject r2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            r2.name = "OrbitRing2";
            r2.transform.SetParent(elements.transform, false);
            r2.transform.localRotation = Quaternion.Euler(-35f, 60f, 0f);
            r2.transform.localScale = new Vector3(0.85f, 0.008f, 0.85f);
            r2.GetComponent<Renderer>().sharedMaterial = matHolo;
            Object.DestroyImmediate(r2.GetComponent<Collider>());

            // Satellite
            GameObject satPivot = new GameObject("SatellitePivot");
            satPivot.transform.SetParent(elements.transform, false);

            GameObject sat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sat.name = "Satellite";
            sat.transform.SetParent(satPivot.transform, false);
            sat.transform.localPosition = new Vector3(0.82f, 0f, 0f);
            sat.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            sat.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(sat.GetComponent<Collider>());

            var col = holo.AddComponent<CylinderColliderPlaceholder>();
            var sCol = holo.AddComponent<SphereCollider>();
            sCol.radius = 1.0f;
            sCol.center = new Vector3(0f, 1.4f, 0f);

            var comp = holo.AddComponent<HologramController>();
            SerializedObject so = new SerializedObject(comp);
            so.FindProperty("_corePivot").objectReferenceValue = core.transform;
            so.FindProperty("_orbitRing1").objectReferenceValue = r1.transform;
            so.FindProperty("_orbitRing2").objectReferenceValue = r2.transform;
            so.FindProperty("_satellitePivot").objectReferenceValue = satPivot.transform;
            so.FindProperty("_coreRenderer").objectReferenceValue = core.GetComponent<Renderer>();
            so.ApplyModifiedProperties();
        }

        private static void BuildAchievementBoard(Transform parent, Vector3 pos, Material matBack, Material matCyan, Material matGreen)
        {
            // Achievement Trophy Board on Back Wall
            GameObject board = new GameObject("AchievementBoard");
            board.transform.SetParent(parent, false);
            board.transform.position = pos;
            board.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // Outer Frame (Matte charcoal backplate)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "BoardFrame";
            frame.transform.SetParent(board.transform, false);
            frame.transform.localPosition = Vector3.zero;
            frame.transform.localScale = new Vector3(4.4f, 2.0f, 0.06f);
            frame.GetComponent<Renderer>().sharedMaterial = matBack;

            // Inner Accent Backplate (Offset to local z = 0.032f)
            GameObject innerPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            innerPlate.name = "InnerPlate";
            innerPlate.transform.SetParent(board.transform, false);
            innerPlate.transform.localPosition = new Vector3(0f, 0f, 0.032f);
            innerPlate.transform.localScale = new Vector3(4.28f, 1.88f, 0.008f);
            var plateMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_PlatformDark.mat");
            innerPlate.GetComponent<Renderer>().sharedMaterial = plateMat;
            Object.DestroyImmediate(innerPlate.GetComponent<Collider>());

            // Header Title (Offset to local z = 0.045f, facing room)
            GameObject titleObj = new GameObject("TitleText", typeof(TextMeshPro));
            titleObj.transform.SetParent(board.transform, false);
            titleObj.transform.localPosition = new Vector3(0f, 0.70f, 0.045f);
            titleObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var tmp = titleObj.GetComponent<TextMeshPro>();
            tmp.text = "<color=#00ffff><b>COMMANDER ACHIEVEMENTS & TROPHIES</b></color>";
            tmp.fontSize = 4.8f;
            tmp.alignment = TextAlignmentOptions.Center;

            // 6 Achievement slots
            float[] xPos = new float[] { -1.35f, 0f, 1.35f, -1.35f, 0f, 1.35f };
            float[] yPos = new float[] { 0.22f, 0.22f, 0.22f, -0.32f, -0.32f, -0.32f };
            string[] names = new string[] { "Desk Jockey", "Chatty User", "Caffeine Dependency", "Button Tester", "Hologram Master", "Party Animal" };

            // Find or create AchievementManager
            GameObject coreSystems = GameObject.Find("CoreSystems");
            if (coreSystems == null) coreSystems = new GameObject("CoreSystems");
            var achMgr = coreSystems.GetComponent<AchievementManager>();
            if (achMgr == null) achMgr = coreSystems.AddComponent<AchievementManager>();

            for (int i = 0; i < 6; i++)
            {
                GameObject slot = new GameObject($"Slot_{names[i]}");
                slot.transform.SetParent(board.transform, false);
                slot.transform.localPosition = new Vector3(xPos[i], yPos[i], 0.045f);
                slot.transform.localRotation = Quaternion.identity;

                // Slot Base Hexagon / Cylinder Badge (local z = 0.015f)
                GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                b.name = "BadgeMesh";
                b.transform.SetParent(slot.transform, false);
                b.transform.localPosition = new Vector3(0f, 0f, 0.015f);
                b.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                b.transform.localScale = new Vector3(0.24f, 0.012f, 0.24f);
                b.GetComponent<Renderer>().sharedMaterial = matGreen;
                Object.DestroyImmediate(b.GetComponent<Collider>());

                // Label (local z = 0.025f, unmirrored facing room)
                GameObject lObj = new GameObject("Label", typeof(TextMeshPro));
                lObj.transform.SetParent(slot.transform, false);
                lObj.transform.localPosition = new Vector3(0f, -0.18f, 0.025f);
                lObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var lTmp = lObj.GetComponent<TextMeshPro>();
                lTmp.text = $"<color=#c0d8f0>{names[i]}</color>";
                lTmp.fontSize = 2.4f;
                lTmp.alignment = TextAlignmentOptions.Center;

                achMgr.RegisterBadge(names[i], b);
            }
        }

        private static void BuildWeatherStationProp(Transform parent, Vector3 pos, float yRot,
                                                    Material matCharcoal, Material matChrome, Material matIndicator, Material matCyan)
        {
            GameObject station = new GameObject("WeatherSwitchStation");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;
            station.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            // 1. Sleek Wall Mounting Bracket (Cube: 0.34m wide x 0.46m tall x 0.06m thick)
            GameObject bracket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bracket.name = "WallBracket";
            bracket.transform.SetParent(station.transform, false);
            bracket.transform.localPosition = Vector3.zero;
            bracket.transform.localScale = new Vector3(0.34f, 0.46f, 0.06f);
            if (matCharcoal != null) bracket.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            Object.DestroyImmediate(bracket.GetComponent<Collider>());

            // 2. Beveled Metallic Trim Faceplate
            GameObject faceplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            faceplate.name = "BezelFaceplate";
            faceplate.transform.SetParent(station.transform, false);
            faceplate.transform.localPosition = new Vector3(0f, 0f, 0.035f);
            faceplate.transform.localScale = new Vector3(0.30f, 0.42f, 0.02f);
            if (matChrome != null) faceplate.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(faceplate.GetComponent<Collider>());

            // 3. Weather Mode Holographic Display Panel
            GameObject display = GameObject.CreatePrimitive(PrimitiveType.Cube);
            display.name = "WeatherDisplay";
            display.transform.SetParent(station.transform, false);
            display.transform.localPosition = new Vector3(0f, 0.10f, 0.05f);
            display.transform.localScale = new Vector3(0.24f, 0.14f, 0.015f);
            if (matCharcoal != null) display.GetComponent<Renderer>().sharedMaterial = matCharcoal;
            Object.DestroyImmediate(display.GetComponent<Collider>());

            // Display Title Text (Facing into the room unmirrored)
            GameObject titleObj = new GameObject("TitleText", typeof(TextMeshPro));
            titleObj.transform.SetParent(station.transform, false);
            titleObj.transform.localPosition = new Vector3(0f, 0.13f, 0.06f);
            titleObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var titleTmp = titleObj.GetComponent<TextMeshPro>();
            titleTmp.text = "<color=#80d0ff>WEATHER CONTROL</color>";
            titleTmp.fontSize = 1.35f;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 4. Glowing Weather Status Indicator Light / Crystal
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = "WeatherIndicatorGlow";
            indicator.transform.SetParent(station.transform, false);
            indicator.transform.localPosition = new Vector3(0f, 0.06f, 0.06f);
            indicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            indicator.transform.localScale = new Vector3(0.065f, 0.01f, 0.065f);
            if (matIndicator != null) indicator.GetComponent<Renderer>().sharedMaterial = matIndicator;
            Object.DestroyImmediate(indicator.GetComponent<Collider>());

            // 5. Tactile Push Button Ring & Plunger
            GameObject btnBezel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            btnBezel.name = "ButtonBezelRing";
            btnBezel.transform.SetParent(station.transform, false);
            btnBezel.transform.localPosition = new Vector3(0f, -0.09f, 0.05f);
            btnBezel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            btnBezel.transform.localScale = new Vector3(0.12f, 0.012f, 0.12f);
            if (matChrome != null) btnBezel.GetComponent<Renderer>().sharedMaterial = matChrome;
            Object.DestroyImmediate(btnBezel.GetComponent<Collider>());

            GameObject buttonPlunger = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonPlunger.name = "WeatherSwitchButton";
            buttonPlunger.transform.SetParent(station.transform, false);
            buttonPlunger.transform.localPosition = new Vector3(0f, -0.09f, 0.062f);
            buttonPlunger.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            buttonPlunger.transform.localScale = new Vector3(0.095f, 0.015f, 0.095f);
            if (matCyan != null) buttonPlunger.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(buttonPlunger.GetComponent<Collider>());

            // Button Action Label (Facing into the room unmirrored)
            GameObject btnTextObj = new GameObject("ButtonLabel", typeof(TextMeshPro));
            btnTextObj.transform.SetParent(station.transform, false);
            btnTextObj.transform.localPosition = new Vector3(0f, -0.17f, 0.06f);
            btnTextObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var btnTmp = btnTextObj.GetComponent<TextMeshPro>();
            btnTmp.text = "<color=#e0f0ff>[E] CYCLE MODE</color>";
            btnTmp.fontSize = 1.15f;
            btnTmp.alignment = TextAlignmentOptions.Center;

            // 6. Interaction Collider & WeatherController component
            BoxCollider col = station.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0f, 0.05f);
            col.size = new Vector3(0.38f, 0.50f, 0.18f);

            station.AddComponent<VRRemoteDesktop.Environment.WeatherController>();
        }

        private static void SetupRoomLighting(List<Renderer> cyanTrims, List<Renderer> violetTrims)
        {
            GameObject coreSystems = GameObject.Find("CoreSystems");
            if (coreSystems == null) coreSystems = new GameObject("CoreSystems");

            var rlc = coreSystems.GetComponent<RoomLightingController>();
            if (rlc == null) rlc = coreSystems.AddComponent<RoomLightingController>();

            Light dirLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
            Light workLight = GameObject.Find("Workstation Light")?.GetComponent<Light>();

            if (workLight == null)
            {
                GameObject wlObj = new GameObject("Workstation Light");
                workLight = wlObj.AddComponent<Light>();
                workLight.type = LightType.Spot;
                workLight.range = 5.0f;
                workLight.spotAngle = 75f;
                workLight.transform.position = new Vector3(0f, 3.2f, 0.8f);
                workLight.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
            }

            SerializedObject so = new SerializedObject(rlc);
            so.FindProperty("_directionalLight").objectReferenceValue = dirLight;
            so.FindProperty("_workstationLight").objectReferenceValue = workLight;
            so.ApplyModifiedProperties();

            rlc.RegisterTrims(cyanTrims, violetTrims);
        }

        private static void RewireInteractionSystem()
        {
            // Connect ComputerInteractionManager to Screen Quad and PromptUI
            var workstation = GameObject.Find("Workstation");
            var cim = Object.FindAnyObjectByType<ComputerInteractionManager>();
            if (cim == null && workstation != null)
            {
                cim = workstation.AddComponent<ComputerInteractionManager>();
            }

            var screenObj = GameObject.Find("Screen");
            var promptUI = Object.FindAnyObjectByType<InteractionPromptUI>();
            var player = Object.FindAnyObjectByType<VRRemoteDesktop.Player.PlayerController>();
            var mouseLook = Object.FindAnyObjectByType<VRRemoteDesktop.Player.MouseLook>();
            var mainCam = Camera.main;

            // Position seated focus point at (0, 1.18, 0.40) matching the true first-person seated eye position in the chair
            GameObject seatedObj = GameObject.Find("SeatedFocusPoint");
            if (seatedObj == null) seatedObj = new GameObject("SeatedFocusPoint");
            seatedObj.transform.position = new Vector3(0f, 1.18f, 0.40f);
            seatedObj.transform.rotation = Quaternion.identity;
            if (workstation != null) seatedObj.transform.SetParent(workstation.transform, true);

            if (cim != null)
            {
                SerializedObject so = new SerializedObject(cim);
                if (screenObj != null) so.FindProperty("_monitorTransform").objectReferenceValue = screenObj.transform;
                so.FindProperty("_seatedFocusPoint").objectReferenceValue = seatedObj.transform;

                if (promptUI != null) so.FindProperty("_promptUI").objectReferenceValue = promptUI;
                if (player != null) so.FindProperty("_playerController").objectReferenceValue = player;
                if (mouseLook != null) so.FindProperty("_mouseLook").objectReferenceValue = mouseLook;
                if (mainCam != null)
                {
                    so.FindProperty("_playerCamera").objectReferenceValue = mainCam;
                    if (mainCam.transform.parent != null)
                    {
                        so.FindProperty("_cameraOffset").objectReferenceValue = mainCam.transform.parent;
                    }
                }

                so.FindProperty("_transitionDuration").floatValue = 1.0f;
                so.FindProperty("_interactionDistance").floatValue = 5.0f;
                so.FindProperty("_facingAngleThreshold").floatValue = 130.0f;
                so.ApplyModifiedProperties();
                Debug.Log("[ExpandedRoomWorkstationBuilder] Rewired ComputerInteractionManager seated view, prompt UI & enhanced sensitivity.");
            }

            // Wire Workstation Gaming Chair
            var chair = Object.FindAnyObjectByType<WorkstationChair>();
            if (chair != null && seatedObj != null)
            {
                SerializedObject soChair = new SerializedObject(chair);
                var headProp = soChair.FindProperty("_seatedHeadPoint");
                if (headProp != null) headProp.objectReferenceValue = seatedObj.transform;
                var rangeProp = soChair.FindProperty("_interactionRange");
                if (rangeProp != null) rangeProp.floatValue = 4.0f;
                soChair.ApplyModifiedProperties();
            }

            // Wire TheaterCinemaController for VIP Sofa & Workstation Elevator
            var cinemaCtrl = Object.FindAnyObjectByType<TheaterCinemaController>();
            if (cinemaCtrl == null)
            {
                var ctrlObj = new GameObject("TheaterCinemaController");
                cinemaCtrl = ctrlObj.AddComponent<TheaterCinemaController>();
            }

            if (workstation == null) workstation = GameObject.Find("Workstation");
            var sofaSeat = GameObject.Find("VIP_TheaterSofa_SeatPoint");
            var welcomeCanvas = GameObject.Find("Theater_WelcomeCanvas");
            if (welcomeCanvas == null)
            {
                var acousticWall = GameObject.Find("AcousticFeatureWall");
                if (acousticWall != null)
                {
                    var t = acousticWall.transform.Find("Theater_WelcomeCanvas");
                    if (t != null) welcomeCanvas = t.gameObject;
                }
            }
            if (welcomeCanvas == null)
            {
                foreach (var c in Resources.FindObjectsOfTypeAll<Canvas>())
                {
                    if (c.gameObject.name == "Theater_WelcomeCanvas")
                    {
                        welcomeCanvas = c.gameObject;
                        break;
                    }
                }
            }
            var projBeam = GameObject.Find("ProjectorBeamLight");

            if (cinemaCtrl != null)
            {
                SerializedObject so = new SerializedObject(cinemaCtrl);
                if (workstation != null) so.FindProperty("_workstation").objectReferenceValue = workstation.transform;
                if (sofaSeat != null) so.FindProperty("_sofaSeatPoint").objectReferenceValue = sofaSeat.transform;
                if (welcomeCanvas != null) so.FindProperty("_theaterWelcomeCanvas").objectReferenceValue = welcomeCanvas;
                if (projBeam != null) so.FindProperty("_projectorBeamLight").objectReferenceValue = projBeam.GetComponent<Light>();
                so.FindProperty("_descendDistance").floatValue = 3.5f;
                so.FindProperty("_elevatorDuration").floatValue = 3.0f;
                so.ApplyModifiedProperties();
                cinemaCtrl.ResolveReferences();
                Debug.Log("[ExpandedRoomWorkstationBuilder] Rewired TheaterCinemaController with 3.0s hydraulic workstation elevator and Welcome Screen.");
            }

            // Wire VRDesktopCursor to VRDesktopInteractable
            var cursor = Object.FindAnyObjectByType<VRDesktopCursor>();
            var interactable = screenObj != null ? screenObj.GetComponent<VRDesktopInteractable>() : null;
            if (cursor != null && interactable != null)
            {
                SerializedObject so = new SerializedObject(interactable);
                so.FindProperty("_cursor").objectReferenceValue = cursor;
                so.ApplyModifiedProperties();
            }

            // Ensure exactly ONE AudioListener in the scene (on MainCamera or Player)
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            if (mainCam == null) mainCam = Camera.main;
            bool keptOne = false;
            foreach (var al in listeners)
            {
                if (mainCam != null && al.gameObject == mainCam.gameObject && !keptOne)
                {
                    keptOne = true;
                    continue;
                }
                if (!keptOne)
                {
                    keptOne = true;
                    continue;
                }
                Object.DestroyImmediate(al);
            }

            // Ensure Player stands comfortably near the workstation chair
            if (player == null) player = Object.FindAnyObjectByType<VRRemoteDesktop.Player.PlayerController>();
            if (player != null)
            {
                player.transform.position = new Vector3(0f, 1.0f, -0.9f);
                player.transform.rotation = Quaternion.identity;
            }
        }

        private static Button CreateCinemaButton(Transform parent, string name, Vector2 pos, Vector2 size, string labelText, Color bgColor, Color textColor)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = bgColor * 1.25f;
            cb.pressedColor = bgColor * 0.85f;
            btn.colors = cb;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;

            return btn;
        }

        private static Slider CreateCinemaSlider(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor)
        {
            GameObject sliderObj = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);
            RectTransform rt = sliderObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Slider slider = sliderObj.GetComponent<Slider>();

            // Background
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.sizeDelta = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 0.9f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.25f);
            faRt.anchorMax = new Vector2(1f, 0.75f);
            faRt.sizeDelta = new Vector2(-10f, 0f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fRt = fill.GetComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = fillColor;

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = new Vector2(0f, 0f);
            haRt.anchorMax = new Vector2(1f, 1f);
            haRt.sizeDelta = new Vector2(-10f, 0f);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform hRt = handle.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(18f, 0f);
            handle.GetComponent<Image>().color = Color.white;

            slider.fillRect = fRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            return slider;
        }

        private static GameObject BuildTheaterWelcomeCanvas(Transform parent)
        {
            GameObject canvasObj = new GameObject("Theater_WelcomeCanvas");
            canvasObj.transform.SetParent(parent, false);
            canvasObj.transform.localPosition = new Vector3(0f, -0.1f, -0.10f);
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = new Vector3(0.00344f, 0.00287f, 1f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;

            RectTransform rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1920f, 1080f);

            var gr = canvasObj.AddComponent<GraphicRaycaster>();
            gr.ignoreReversedGraphics = false;
            gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            // 1. Full-screen RawImage for video projection output
            GameObject videoSurfaceObj = new GameObject("VideoDisplaySurface", typeof(RectTransform), typeof(RawImage));
            videoSurfaceObj.transform.SetParent(canvasObj.transform, false);
            RectTransform vsRt = videoSurfaceObj.GetComponent<RectTransform>();
            vsRt.anchorMin = Vector2.zero;
            vsRt.anchorMax = Vector2.one;
            vsRt.sizeDelta = Vector2.zero;
            RawImage rawImg = videoSurfaceObj.GetComponent<RawImage>();
            rawImg.color = Color.white;
            videoSurfaceObj.SetActive(false);

            // 2. Standby Welcome Dashboard Panel (Visible when no video is playing)
            GameObject standbyPanel = new GameObject("StandbyWelcomePanel", typeof(RectTransform));
            standbyPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform spRt = standbyPanel.GetComponent<RectTransform>();
            spRt.anchorMin = Vector2.zero;
            spRt.anchorMax = Vector2.one;
            spRt.sizeDelta = Vector2.zero;

            // Dark sleek background
            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(standbyPanel.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            Image bgImg = bgObj.GetComponent<Image>();
            bgImg.color = new Color(0.02f, 0.04f, 0.08f, 0.96f);

            // Neon Cyan Outer Border
            GameObject borderObj = new GameObject("BorderGlow", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(standbyPanel.transform, false);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(-40f, -40f);
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.color = new Color(0f, 0.85f, 1f, 0.35f);

            GameObject innerBg = new GameObject("InnerDark", typeof(RectTransform), typeof(Image));
            innerBg.transform.SetParent(borderObj.transform, false);
            RectTransform innerRt = innerBg.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.sizeDelta = new Vector2(-6f, -6f);
            Image innerImg = innerBg.GetComponent<Image>();
            innerImg.color = new Color(0.03f, 0.05f, 0.09f, 0.98f);

            // Header Badge
            GameObject badgeObj = new GameObject("HeaderBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            badgeObj.transform.SetParent(innerBg.transform, false);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.5f, 1f);
            badgeRt.anchorMax = new Vector2(0.5f, 1f);
            badgeRt.pivot = new Vector2(0.5f, 1f);
            badgeRt.anchoredPosition = new Vector2(0f, -50f);
            badgeRt.sizeDelta = new Vector2(1000f, 40f);
            var badgeTmp = badgeObj.GetComponent<TextMeshProUGUI>();
            badgeTmp.text = "*  COMMAND CENTER VIP THEATER | DOLBY ATMOS 7.1  *";
            badgeTmp.fontSize = 22;
            badgeTmp.fontStyle = FontStyles.Bold;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = new Color(0f, 1f, 0.95f, 1f);

            // Main Welcome Title
            GameObject titleObj = new GameObject("MainTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(innerBg.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -100f);
            titleRt.sizeDelta = new Vector2(1600f, 90f);
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "IMAX LOCAL MEDIA CINEMA";
            titleTmp.fontSize = 58;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(innerBg.transform, false);
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 1f);
            subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -195f);
            subRt.sizeDelta = new Vector2(1400f, 45f);
            var subTmp = subObj.GetComponent<TextMeshProUGUI>();
            subTmp.text = "PLAY ANY DOWNLOADED VIDEO FROM YOUR COMPUTER IN 4K / 60 FPS";
            subTmp.fontSize = 20;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.7f, 0.85f, 1f, 0.9f);

            // Big Central [[DIR] BROWSE LOCAL VIDEO (PRESS 'O')] Button
            Button bigBrowseBtn = CreateCinemaButton(innerBg.transform, "BigBrowseButton", new Vector2(0f, 25f), new Vector2(560f, 85f),
                                                     "[DIR]  BROWSE LOCAL VIDEO FILE", new Color(0f, 0.75f, 0.95f, 0.90f), Color.white);
            bigBrowseBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            bigBrowseBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            bigBrowseBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            bigBrowseBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 26;

            // File formats / Folder hint
            GameObject hintFolderObj = new GameObject("FolderHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintFolderObj.transform.SetParent(innerBg.transform, false);
            RectTransform fhRt = hintFolderObj.GetComponent<RectTransform>();
            fhRt.anchorMin = new Vector2(0.5f, 0.5f);
            fhRt.anchorMax = new Vector2(0.5f, 0.5f);
            fhRt.pivot = new Vector2(0.5f, 0.5f);
            fhRt.anchoredPosition = new Vector2(0f, -50f);
            fhRt.sizeDelta = new Vector2(1200f, 40f);
            var fhTmp = hintFolderObj.GetComponent<TextMeshProUGUI>();
            fhTmp.text = "<color=#80c8ff>Supports:</color> MP4 | WEBM | MOV | M4V | AVI | MKV    <color=#5080a0>|</color>    <color=#80c8ff>Or drop in:</color> Assets/StreamingAssets/Videos/";
            fhTmp.fontSize = 18;
            fhTmp.alignment = TextAlignmentOptions.Center;

            // Hotkeys Guide Card
            GameObject hotkeyCard = new GameObject("HotkeysCard", typeof(RectTransform), typeof(TextMeshProUGUI));
            hotkeyCard.transform.SetParent(innerBg.transform, false);
            RectTransform hkRt = hotkeyCard.GetComponent<RectTransform>();
            hkRt.anchorMin = new Vector2(0.5f, 0f);
            hkRt.anchorMax = new Vector2(0.5f, 0f);
            hkRt.pivot = new Vector2(0.5f, 0f);
            hkRt.anchoredPosition = new Vector2(0f, 130f);
            hkRt.sizeDelta = new Vector2(1400f, 50f);
            var hkTmp = hotkeyCard.GetComponent<TextMeshProUGUI>();
            hkTmp.text = "<color=#00e5ff>[O]</color> Browse Video    <color=#00e5ff>[SPACE]</color> Play/Pause    <color=#00e5ff>[LEFT/RIGHT]</color> +/-10s Seek    <color=#00e5ff>[UP/DOWN]</color> Volume    <color=#00e5ff>[M]</color> Mute    <color=#00e5ff>[S]</color> Stop";
            hkTmp.fontSize = 20;
            hkTmp.alignment = TextAlignmentOptions.Center;
            hkTmp.color = new Color(0.85f, 0.92f, 1f, 0.9f);

            // Exit hint
            GameObject hintObj = new GameObject("ExitHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            hintObj.transform.SetParent(innerBg.transform, false);
            RectTransform hintRt = hintObj.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 40f);
            hintRt.sizeDelta = new Vector2(1200f, 45f);
            var hintTmp = hintObj.GetComponent<TextMeshProUGUI>();
            hintTmp.text = "PRESS  <color=#00ffff><b>[ESC]</b></color>  TO EXIT CINEMA MODE & RAISE WORKSTATION";
            hintTmp.fontSize = 20;
            hintTmp.fontStyle = FontStyles.Bold;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = Color.white;

            // 3. Floating Bottom Media Controls Bar
            GameObject controlsPanel = new GameObject("CinemaControlsPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            controlsPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform cpRt = controlsPanel.GetComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0.5f, 0f);
            cpRt.anchorMax = new Vector2(0.5f, 0f);
            cpRt.pivot = new Vector2(0.5f, 0f);
            cpRt.anchoredPosition = new Vector2(0f, 35f);
            cpRt.sizeDelta = new Vector2(1780f, 100f);

            Image cpImg = controlsPanel.GetComponent<Image>();
            cpImg.color = new Color(0.03f, 0.06f, 0.12f, 0.92f);
            CanvasGroup cpCg = controlsPanel.GetComponent<CanvasGroup>();

            // Scrubber Slider across the top of controls panel
            Slider timeSlider = CreateCinemaSlider(controlsPanel.transform, "TimeScrubber", new Vector2(-110f, 22f), new Vector2(1480f, 24f), new Color(0f, 0.85f, 1f));

            // Time text beside scrubber
            GameObject timeTextObj = new GameObject("TimeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            timeTextObj.transform.SetParent(controlsPanel.transform, false);
            RectTransform ttRt = timeTextObj.GetComponent<RectTransform>();
            ttRt.anchoredPosition = new Vector2(740f, 22f);
            ttRt.sizeDelta = new Vector2(180f, 28f);
            var timeTmp = timeTextObj.GetComponent<TextMeshProUGUI>();
            timeTmp.text = "00:00 / 00:00";
            timeTmp.fontSize = 18;
            timeTmp.fontStyle = FontStyles.Bold;
            timeTmp.alignment = TextAlignmentOptions.Right;
            timeTmp.color = Color.white;

            // Bottom Buttons Row:
            // Browse Button
            Button browseBtn = CreateCinemaButton(controlsPanel.transform, "Btn_Browse", new Vector2(-780f, -24f), new Vector2(140f, 42f), "[DIR] OPEN", new Color(0f, 0.65f, 0.85f), Color.white);
            // Rewind Button
            Button rewindBtn = CreateCinemaButton(controlsPanel.transform, "Btn_Rewind", new Vector2(-650f, -24f), new Vector2(95f, 42f), "[<<] -10s", new Color(0.12f, 0.20f, 0.32f), Color.white);
            // Play/Pause Button
            Button playPauseBtn = CreateCinemaButton(controlsPanel.transform, "Btn_PlayPause", new Vector2(-515f, -24f), new Vector2(130f, 42f), "<b>> PLAY</b>", new Color(0f, 0.80f, 0.50f), Color.white);
            // Stop Button
            Button stopBtn = CreateCinemaButton(controlsPanel.transform, "Btn_Stop", new Vector2(-380f, -24f), new Vector2(100f, 42f), "[STOP] STOP", new Color(0.50f, 0.15f, 0.20f), Color.white);
            // Forward Button
            Button forwardBtn = CreateCinemaButton(controlsPanel.transform, "Btn_Forward", new Vector2(-260f, -24f), new Vector2(95f, 42f), "+10s [>>]", new Color(0.12f, 0.20f, 0.32f), Color.white);

            // Background Playback / Watch Only Toggle Button
            Button bgPlaybackBtn = CreateCinemaButton(controlsPanel.transform, "Btn_BgPlayback", new Vector2(-120f, -24f), new Vector2(165f, 42f), "<b>[CINEMA] BG PLAY: ON</b>", new Color(0.06f, 0.40f, 0.48f), Color.white);

            // Now Playing Title Text (Center)
            GameObject nowPlayingObj = new GameObject("NowPlayingTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            nowPlayingObj.transform.SetParent(controlsPanel.transform, false);
            RectTransform npRt = nowPlayingObj.GetComponent<RectTransform>();
            npRt.anchoredPosition = new Vector2(195f, -24f);
            npRt.sizeDelta = new Vector2(540f, 36f);
            var npTmp = nowPlayingObj.GetComponent<TextMeshProUGUI>();
            npTmp.text = "<color=#00e5ff>READY | PRESS [O] OR OPEN TO LOAD LOCAL VIDEO</color>";
            npTmp.fontSize = 18;
            npTmp.alignment = TextAlignmentOptions.Left;
            npTmp.color = Color.white;
            npTmp.textWrappingMode = TextWrappingModes.NoWrap;

            // Mute Button
            Button muteBtn = CreateCinemaButton(controlsPanel.transform, "Btn_Mute", new Vector2(580f, -24f), new Vector2(95f, 42f), "[+] MUTE", new Color(0.12f, 0.20f, 0.32f), Color.white);

            // Volume Slider
            Slider volSlider = CreateCinemaSlider(controlsPanel.transform, "VolumeSlider", new Vector2(720f, -24f), new Vector2(140f, 20f), new Color(0f, 0.85f, 1f));

            // 4. Attach & Wire TheaterVideoPlayer Component
            var videoPlayerComp = canvasObj.AddComponent<TheaterVideoPlayer>();
            SerializedObject soVp = new SerializedObject(videoPlayerComp);

            soVp.FindProperty("_screenDisplayImage").objectReferenceValue = rawImg;
            soVp.FindProperty("_controlsPanel").objectReferenceValue = controlsPanel;
            soVp.FindProperty("_controlsCanvasGroup").objectReferenceValue = cpCg;
            soVp.FindProperty("_welcomeStandbyPanel").objectReferenceValue = standbyPanel;

            soVp.FindProperty("_titleText").objectReferenceValue = npTmp;
            soVp.FindProperty("_timeText").objectReferenceValue = timeTmp;
            soVp.FindProperty("_statusText").objectReferenceValue = npTmp;
            soVp.FindProperty("_timeSlider").objectReferenceValue = timeSlider;
            soVp.FindProperty("_volumeSlider").objectReferenceValue = volSlider;

            soVp.FindProperty("_playPauseButton").objectReferenceValue = playPauseBtn;
            soVp.FindProperty("_playPauseText").objectReferenceValue = playPauseBtn.GetComponentInChildren<TextMeshProUGUI>();
            soVp.FindProperty("_browseButton").objectReferenceValue = browseBtn;
            soVp.FindProperty("_stopButton").objectReferenceValue = stopBtn;
            soVp.FindProperty("_stepBackButton").objectReferenceValue = rewindBtn;
            soVp.FindProperty("_stepForwardButton").objectReferenceValue = forwardBtn;
            soVp.FindProperty("_muteButton").objectReferenceValue = muteBtn;
            soVp.FindProperty("_muteText").objectReferenceValue = muteBtn.GetComponentInChildren<TextMeshProUGUI>();
            soVp.FindProperty("_bgPlaybackButton").objectReferenceValue = bgPlaybackBtn;
            soVp.FindProperty("_bgPlaybackText").objectReferenceValue = bgPlaybackBtn.GetComponentInChildren<TextMeshProUGUI>();
            soVp.FindProperty("_bgPlaybackImage").objectReferenceValue = bgPlaybackBtn.GetComponent<Image>();

            soVp.ApplyModifiedProperties();

            // Also wire BigBrowseButton to open file browser
            bigBrowseBtn.onClick.AddListener(videoPlayerComp.OpenFileBrowser);

            canvasObj.SetActive(false);
            return canvasObj;
        }

        private static void BuildVIPTheaterSofa(Transform parent, Material matLeather, Material matChrome, Material matCyan, Material matViolet, Material matMagenta, Material matGreen)
        {
            GameObject sofaRoot = new GameObject("VIP_TheaterSofa");
            sofaRoot.transform.SetParent(parent, false);
            sofaRoot.transform.position = new Vector3(0f, 0.06f, -2.60f); // Moved back for generous 2.2m walkway behind desk chair!
            sofaRoot.transform.rotation = Quaternion.identity; // Facing +Z towards the theater screen!

            // Built-in tactile button console on right outer armrest of VIP sofa
            BuildButtonPanelProp(sofaRoot.transform, new Vector3(1.15f, 0.60f, 0.05f), matLeather, matCyan, matMagenta, matGreen, matViolet);

            // Base Plinth
            GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Sofa_Plinth";
            plinth.transform.SetParent(sofaRoot.transform, false);
            plinth.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            plinth.transform.localScale = new Vector3(2.35f, 0.16f, 1.05f);
            plinth.GetComponent<Renderer>().sharedMaterial = matLeather;

            // Plinth Cyan Underglow
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = "Plinth_Glow";
            glow.transform.SetParent(plinth.transform, false);
            glow.transform.localPosition = new Vector3(0f, -0.42f, 0.51f);
            glow.transform.localScale = new Vector3(0.96f, 0.12f, 0.02f);
            glow.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(glow.GetComponent<Collider>());

            // 2 Extra-Wide VIP Recliner Cushions
            for (int s = -1; s <= 1; s += 2)
            {
                GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seat.name = $"SeatCushion_{(s < 0 ? "L" : "R")}";
                seat.transform.SetParent(sofaRoot.transform, false);
                seat.transform.localPosition = new Vector3(s * 0.52f, 0.26f, 0.06f);
                seat.transform.localScale = new Vector3(0.88f, 0.20f, 0.88f);
                seat.GetComponent<Renderer>().sharedMaterial = matLeather;

                // Backrest with comfortable backward recline (-14 deg)
                GameObject backrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backrest.name = $"Backrest_{(s < 0 ? "L" : "R")}";
                backrest.transform.SetParent(sofaRoot.transform, false);
                backrest.transform.localPosition = new Vector3(s * 0.52f, 0.62f, -0.32f);
                backrest.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
                backrest.transform.localScale = new Vector3(0.86f, 0.58f, 0.18f);
                backrest.GetComponent<Renderer>().sharedMaterial = matLeather;

                // Headrest pillow
                GameObject headrest = GameObject.CreatePrimitive(PrimitiveType.Cube);
                headrest.name = $"Headrest_{(s < 0 ? "L" : "R")}";
                headrest.transform.SetParent(backrest.transform, false);
                headrest.transform.localPosition = new Vector3(0f, 0.38f, 0.06f);
                headrest.transform.localScale = new Vector3(0.72f, 0.22f, 0.14f);
                headrest.GetComponent<Renderer>().sharedMaterial = matLeather;
            }

            // 3 Armrests: Left, Center (with dual cup holders!), Right
            for (int a = -1; a <= 1; a++)
            {
                GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arm.name = $"Armrest_{a + 1}";
                arm.transform.SetParent(sofaRoot.transform, false);
                arm.transform.localPosition = new Vector3(a * 1.05f, 0.38f, 0.04f);
                arm.transform.localScale = new Vector3(a == 0 ? 0.30f : 0.20f, 0.40f, 0.98f);
                arm.GetComponent<Renderer>().sharedMaterial = matLeather;

                // Cup holders on armrests
                GameObject cupHolder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cupHolder.name = "CupHolder";
                cupHolder.transform.SetParent(arm.transform, false);
                cupHolder.transform.localPosition = new Vector3(0f, 0.51f, 0.26f);
                cupHolder.transform.localScale = new Vector3(0.48f, 0.02f, 0.18f);
                cupHolder.GetComponent<Renderer>().sharedMaterial = matChrome;
                Object.DestroyImmediate(cupHolder.GetComponent<Collider>());
            }

            // 2 Designer Accent Pillows
            GameObject p1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p1.name = "AccentPillow_Cyan";
            p1.transform.SetParent(sofaRoot.transform, false);
            p1.transform.localPosition = new Vector3(-0.75f, 0.38f, -0.16f);
            p1.transform.localRotation = Quaternion.Euler(0f, 20f, 12f);
            p1.transform.localScale = new Vector3(0.28f, 0.28f, 0.12f);
            p1.GetComponent<Renderer>().sharedMaterial = matCyan;
            Object.DestroyImmediate(p1.GetComponent<Collider>());

            GameObject p2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p2.name = "AccentPillow_Violet";
            p2.transform.SetParent(sofaRoot.transform, false);
            p2.transform.localPosition = new Vector3(0.75f, 0.38f, -0.16f);
            p2.transform.localRotation = Quaternion.Euler(0f, -20f, -12f);
            p2.transform.localScale = new Vector3(0.28f, 0.28f, 0.12f);
            p2.GetComponent<Renderer>().sharedMaterial = matViolet;
            Object.DestroyImmediate(p2.GetComponent<Collider>());

            // Box Collider for interaction detection
            var col = sofaRoot.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.55f, 0f);
            col.size = new Vector3(2.1f, 1.1f, 1.2f);

            // Interactive TheaterSofa component
            var sofaComp = sofaRoot.AddComponent<TheaterSofa>();
            sofaComp.SetPromptAction("HYDRAULIC THEATER");
            SerializedObject soSofa = new SerializedObject(sofaComp);
            var sofaRangeProp = soSofa.FindProperty("_interactionRange");
            if (sofaRangeProp != null) sofaRangeProp.floatValue = 4.0f;
            soSofa.ApplyModifiedProperties();

            // Camera Seating Point for Cinema Mode
            GameObject seatPoint = new GameObject("VIP_TheaterSofa_SeatPoint");
            seatPoint.transform.SetParent(sofaRoot.transform, false);
            seatPoint.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            seatPoint.transform.localRotation = Quaternion.identity;
        }

        private static InteractionPromptUI BuildInteractionPromptCanvas(Transform parent)
        {
            GameObject oldCanvas = GameObject.Find("InteractionPromptCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            GameObject canvasObj = new GameObject("InteractionPromptCanvas");
            canvasObj.transform.SetParent(parent, false);
            canvasObj.transform.localPosition = new Vector3(0f, 1.02f, 0.48f);
            canvasObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            canvasObj.transform.localScale = Vector3.one * 0.0022f;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = canvasObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400f, 180f);

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            InteractionPromptUI promptUI = canvasObj.AddComponent<InteractionPromptUI>();

            // Enter Prompt container
            GameObject enterObj = new GameObject("EnterPrompt", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            enterObj.transform.SetParent(canvasObj.transform, false);
            RectTransform enterRt = enterObj.GetComponent<RectTransform>();
            enterRt.anchorMin = new Vector2(0.5f, 0.5f);
            enterRt.anchorMax = new Vector2(0.5f, 0.5f);
            enterRt.pivot = new Vector2(0.5f, 0.5f);
            enterRt.sizeDelta = new Vector2(380f, 90f);
            enterRt.anchoredPosition = new Vector2(0f, -10f);
            CanvasGroup enterGroup = enterObj.GetComponent<CanvasGroup>();
            enterGroup.alpha = 0f;
            Image enterBg = enterObj.GetComponent<Image>();
            enterBg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);

            // Cyan Border
            GameObject borderObj = new GameObject("BorderGlow", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(enterObj.transform, false);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(2f, 2f);
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.color = new Color(0f, 0.95f, 1f, 0.85f);

            // Inner Inset
            GameObject insetObj = new GameObject("InnerInset", typeof(RectTransform), typeof(Image));
            insetObj.transform.SetParent(borderObj.transform, false);
            RectTransform insetRt = insetObj.GetComponent<RectTransform>();
            insetRt.anchorMin = Vector2.zero;
            insetRt.anchorMax = Vector2.one;
            insetRt.offsetMin = new Vector2(2f, 2f);
            insetRt.offsetMax = new Vector2(-2f, -2f);
            Image insetImg = insetObj.GetComponent<Image>();
            insetImg.color = new Color(0.05f, 0.08f, 0.13f, 0.96f);

            // Key Badge [E]
            GameObject keyBadge = new GameObject("KeyBadge", typeof(RectTransform), typeof(Image));
            keyBadge.transform.SetParent(insetObj.transform, false);
            RectTransform keyRt = keyBadge.GetComponent<RectTransform>();
            keyRt.anchorMin = new Vector2(0f, 0.5f);
            keyRt.anchorMax = new Vector2(0f, 0.5f);
            keyRt.pivot = new Vector2(0f, 0.5f);
            keyRt.anchoredPosition = new Vector2(16f, 0f);
            keyRt.sizeDelta = new Vector2(56f, 56f);
            Image keyImg = keyBadge.GetComponent<Image>();
            keyImg.color = new Color(0f, 0.95f, 1f, 1f);

            GameObject keyTxtObj = new GameObject("KeyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            keyTxtObj.transform.SetParent(keyBadge.transform, false);
            RectTransform ktRt = keyTxtObj.GetComponent<RectTransform>();
            ktRt.anchorMin = Vector2.zero;
            ktRt.anchorMax = Vector2.one;
            ktRt.sizeDelta = Vector2.zero;
            var ktTmp = keyTxtObj.GetComponent<TextMeshProUGUI>();
            ktTmp.text = "E";
            ktTmp.fontSize = 32;
            ktTmp.fontStyle = FontStyles.Bold;
            ktTmp.alignment = TextAlignmentOptions.Center;
            ktTmp.color = new Color(0.04f, 0.06f, 0.10f, 1f);

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(insetObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.5f);
            titleRt.anchorMax = new Vector2(1f, 0.5f);
            titleRt.pivot = new Vector2(0f, 0.5f);
            titleRt.anchoredPosition = new Vector2(88f, 12f);
            titleRt.sizeDelta = new Vector2(-98f, 30f);
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "USE COMPUTER";
            titleTmp.fontSize = 22;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;

            // Subtitle
            GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(insetObj.transform, false);
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0.5f);
            subRt.anchorMax = new Vector2(1f, 0.5f);
            subRt.pivot = new Vector2(0f, 0.5f);
            subRt.anchoredPosition = new Vector2(88f, -14f);
            subRt.sizeDelta = new Vector2(-98f, 24f);
            var subTmp = subObj.GetComponent<TextMeshProUGUI>();
            subTmp.text = "Press [E] to sit & work";
            subTmp.fontSize = 15;
            subTmp.color = new Color(0f, 0.95f, 1f, 0.9f);

            // Exit Prompt container
            GameObject exitObj = new GameObject("ExitPrompt", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            exitObj.transform.SetParent(canvasObj.transform, false);
            RectTransform exitRt = exitObj.GetComponent<RectTransform>();
            exitRt.anchorMin = new Vector2(0.5f, 0.5f);
            exitRt.anchorMax = new Vector2(0.5f, 0.5f);
            exitRt.pivot = new Vector2(0.5f, 0.5f);
            exitRt.sizeDelta = new Vector2(280f, 50f);
            exitRt.anchoredPosition = new Vector2(0f, -20f);
            CanvasGroup exitGroup = exitObj.GetComponent<CanvasGroup>();
            exitGroup.alpha = 0f;
            Image exitBg = exitObj.GetComponent<Image>();
            exitBg.color = new Color(0.04f, 0.06f, 0.10f, 0.85f);

            GameObject exitTxt = new GameObject("ExitTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            exitTxt.transform.SetParent(exitObj.transform, false);
            RectTransform etRt = exitTxt.GetComponent<RectTransform>();
            etRt.anchorMin = Vector2.zero;
            etRt.anchorMax = Vector2.one;
            etRt.sizeDelta = Vector2.zero;
            var etTmp = exitTxt.GetComponent<TextMeshProUGUI>();
            etTmp.text = "PRESS  <color=#00ffff>[ESC]</color>  TO EXIT PC";
            etTmp.fontSize = 17;
            etTmp.fontStyle = FontStyles.Bold;
            etTmp.alignment = TextAlignmentOptions.Center;
            etTmp.color = Color.white;

            // Link serialized properties in InteractionPromptUI
            SerializedObject pSo = new SerializedObject(promptUI);
            pSo.FindProperty("_enterPromptGroup").objectReferenceValue = enterGroup;
            pSo.FindProperty("_exitPromptGroup").objectReferenceValue = exitGroup;
            pSo.FindProperty("_enterTitleText").objectReferenceValue = titleTmp;
            pSo.FindProperty("_enterKeyBadge").objectReferenceValue = keyImg;
            pSo.FindProperty("_enterBorderGlow").objectReferenceValue = borderImg;
            pSo.FindProperty("_exitTitleText").objectReferenceValue = etTmp;
            pSo.ApplyModifiedProperties();

            return promptUI;
        }
    }

    public class CylinderColliderPlaceholder : MonoBehaviour { }
}
