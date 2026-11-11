using UnityEngine;
using UnityEditor;
using System.IO;

namespace VRRemoteDesktop.Editor
{
    public static class MultiMonitorAssetSetup
    {
        public static void EnsureAssets()
        {
            if (!Directory.Exists("Assets/Textures")) Directory.CreateDirectory("Assets/Textures");
            if (!Directory.Exists("Assets/Materials")) Directory.CreateDirectory("Assets/Materials");

            // 1. Create Left & Right RenderTextures
            EnsureRenderTexture("Assets/Textures/UI_Left_RenderTexture.renderTexture", 1920, 1080);
            EnsureRenderTexture("Assets/Textures/UI_Right_RenderTexture.renderTexture", 1920, 1080);

            // 2. Create Screen Materials bound to RenderTextures
            EnsureScreenMaterial("Assets/Materials/Mat_ScreenLeft.mat", "Assets/Textures/UI_Left_RenderTexture.renderTexture");
            EnsureScreenMaterial("Assets/Materials/Mat_ScreenRight.mat", "Assets/Textures/UI_Right_RenderTexture.renderTexture");

            // 3. Create Supplemental Prop Materials
            CreateURPLit("Assets/Materials/Mat_MetalChrome.mat", new Color(0.85f, 0.88f, 0.92f), 0.95f, 0.90f);
            CreateURPLit("Assets/Materials/Mat_CoffeeMachine.mat", new Color(0.08f, 0.08f, 0.09f), 0.70f, 0.20f);
            CreateURPLit("Assets/Materials/Mat_PlatformDark.mat", new Color(0.035f, 0.04f, 0.05f), 0.40f, 0.10f);
            CreateURPLit("Assets/Materials/Mat_AcousticWood.mat", new Color(0.12f, 0.13f, 0.16f), 0.25f, 0.05f);
            CreateURPLit("Assets/Materials/Mat_DeskDrawerWhite.mat", new Color(0.92f, 0.93f, 0.95f), 0.60f, 0.05f);

            // Realistic Tempered Glass for CPU case & glass surfaces
            EnsureTemperedGlassMaterial("Assets/Materials/Mat_TemperedGlass.mat");

            // Emissive Accent Colors & Battlestation Ambient Glow
            CreateURPEmissive("Assets/Materials/Mat_NeonOrange.mat", new Color(1.0f, 0.55f, 0.05f) * 3.5f);
            CreateURPEmissive("Assets/Materials/Mat_NeonGreen.mat", new Color(0.05f, 1.0f, 0.45f) * 3.5f);
            CreateURPEmissive("Assets/Materials/Mat_EmissiveRed.mat", new Color(1.0f, 0.1f, 0.15f) * 3.5f);
            CreateURPEmissive("Assets/Materials/Mat_AmbientWallGlow.mat", new Color(0.0f, 0.55f, 1.0f) * 3.8f);
            CreateURPEmissive("Assets/Materials/Mat_WarmOrb.mat", new Color(1.0f, 0.95f, 0.8f) * 2.5f);

            // Projector Materials
            EnsureProjectorScreenMaterial("Assets/Materials/Mat_ProjectorScreen.mat");
            EnsureProjectorBeamMaterial("Assets/Materials/Mat_ProjectorBeam.mat");

            // Holographic Materials (Unlit)
            EnsureHologramMaterial("Assets/Materials/Mat_HologramCyan.mat", new Color(0.0f, 1.0f, 1.0f, 0.65f));
            EnsureHologramMaterial("Assets/Materials/Mat_HologramMagenta.mat", new Color(1.0f, 0.1f, 0.65f, 0.65f));

            // Window Scenery Material
            EnsureCosmicVistaMaterial("Assets/Materials/Mat_CosmicVista.mat");

            // Biophilic Corner Plant Materials
            CreateURPLit("Assets/Materials/Mat_PlantLeafEmerald.mat", new Color(0.08f, 0.44f, 0.16f), 0.0f, 0.35f);
            CreateURPLit("Assets/Materials/Mat_PlantLeafVibrant.mat", new Color(0.18f, 0.68f, 0.22f), 0.0f, 0.40f);
            CreateURPLit("Assets/Materials/Mat_PlantStem.mat", new Color(0.24f, 0.48f, 0.18f), 0.0f, 0.25f);
            CreateURPLit("Assets/Materials/Mat_PlanterCeramic.mat", new Color(0.92f, 0.94f, 0.96f), 0.10f, 0.70f);
            CreateURPLit("Assets/Materials/Mat_PlanterSoil.mat", new Color(0.09f, 0.07f, 0.06f), 0.0f, 0.05f);

            // 3D Nature Landscape Materials (Mountains, Trees, River, Cottages)
            CreateURPLit("Assets/Materials/Mat_MountainRock.mat", new Color(0.28f, 0.30f, 0.35f), 0.25f, 0.10f);
            CreateURPLit("Assets/Materials/Mat_MountainSnow.mat", new Color(0.96f, 0.98f, 1.0f), 0.40f, 0.05f);
            CreateURPLit("Assets/Materials/Mat_TerrainGrass.mat", new Color(0.22f, 0.42f, 0.18f), 0.20f, 0.05f);
            CreateURPLit("Assets/Materials/Mat_PineNeedles.mat", new Color(0.11f, 0.28f, 0.14f), 0.35f, 0.05f);
            CreateURPLit("Assets/Materials/Mat_PineNeedlesVibrant.mat", new Color(0.16f, 0.42f, 0.20f), 0.35f, 0.05f);
            CreateURPLit("Assets/Materials/Mat_TreeBark.mat", new Color(0.32f, 0.22f, 0.14f), 0.20f, 0.05f);
            CreateURPLit("Assets/Materials/Mat_RiverStone.mat", new Color(0.38f, 0.40f, 0.42f), 0.35f, 0.10f);
            CreateURPLit("Assets/Materials/Mat_CabinWood.mat", new Color(0.45f, 0.30f, 0.18f), 0.25f, 0.05f);
            CreateURPLit("Assets/Materials/Mat_CabinRoof.mat", new Color(0.20f, 0.18f, 0.20f), 0.30f, 0.05f);
            CreateURPEmissive("Assets/Materials/Mat_WindowGlowWarm.mat", new Color(1.0f, 0.82f, 0.40f) * 2.5f);
            EnsureRiverWaterMaterial("Assets/Materials/Mat_RiverWater.mat");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureRenderTexture(string path, int width, int height)
        {
            if (AssetDatabase.LoadAssetAtPath<RenderTexture>(path) == null)
            {
                RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                AssetDatabase.CreateAsset(rt, path);
                Debug.Log($"[MultiMonitorAssetSetup] Created RenderTexture: {path}");
            }
        }

        private static void EnsureScreenMaterial(string matPath, string rtPath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            RenderTexture rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);

            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetColor("_BaseColor", Color.white);
                if (rt != null)
                {
                    mat.SetTexture("_BaseMap", rt);
                }
                AssetDatabase.CreateAsset(mat, matPath);
                Debug.Log($"[MultiMonitorAssetSetup] Created Screen Material: {matPath}");
            }
            else if (rt != null && mat.GetTexture("_BaseMap") == null)
            {
                mat.SetTexture("_BaseMap", rt);
                EditorUtility.SetDirty(mat);
            }
        }

        private static void CreateURPLit(string path, Color color, float smoothness, float metallic)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", smoothness);
                mat.SetFloat("_Metallic", metallic);
                AssetDatabase.CreateAsset(mat, path);
            }
        }

        private static void CreateURPEmissive(string path, Color emissiveColor)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", Color.black);
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", emissiveColor);
                AssetDatabase.CreateAsset(mat, path);
            }
        }

        private static void EnsureHologramMaterial(string path, Color color)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetColor("_BaseColor", color);
                // Configure alpha blending
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);   // Alpha
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive glow
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                AssetDatabase.CreateAsset(mat, path);
            }
        }

        private static void EnsureTemperedGlassMaterial(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);   // Alpha blend
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                // Subtle cool tint with high smoothness for realistic reflections
                mat.SetColor("_BaseColor", new Color(0.85f, 0.92f, 1.0f, 0.14f));
                mat.SetFloat("_Smoothness", 0.98f);
                mat.SetFloat("_Metallic", 0.15f);
                mat.SetFloat("_Cull", 0); // Double-sided
                AssetDatabase.CreateAsset(mat, path);
            }
        }

        private static void EnsureRiverWaterMaterial(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0);   // Alpha blend
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.SetColor("_BaseColor", new Color(0.18f, 0.58f, 0.78f, 0.85f));
                mat.SetFloat("_Smoothness", 0.96f);
                mat.SetFloat("_Metallic", 0.12f);
                mat.SetFloat("_Cull", 0); // Double-sided
                AssetDatabase.CreateAsset(mat, path);
            }
        }

        private static void EnsureProjectorScreenMaterial(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", new Color(0.96f, 0.96f, 0.98f));
                mat.SetFloat("_Smoothness", 0.10f); // Matte screen surface
                mat.SetFloat("_Metallic", 0.0f);
                AssetDatabase.CreateAsset(mat, path);
            }
        }

        private static void EnsureProjectorBeamMaterial(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 1);   // Additive
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.SetColor("_BaseColor", new Color(0.2f, 0.6f, 1.0f, 0.12f));
                AssetDatabase.CreateAsset(mat, path);
            }
        }

        private static void EnsureCosmicVistaMaterial(string path)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Scenery_CosmicVista.jpg");
            if (tex == null) tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Scenery_MountainNature.jpg");
            if (tex != null)
            {
                mat.mainTexture = tex;
                mat.SetTexture("_BaseMap", tex);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Cull", 0); // Double-sided rendering so backdrop is never culled
            EditorUtility.SetDirty(mat);
        }
    }
}
