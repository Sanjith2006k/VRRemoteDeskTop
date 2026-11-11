using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

public class EnvironmentBuilder : EditorWindow
{
    [MenuItem("VR Remote Desktop/Build Phase 2 Environment")]
    public static void BuildEnvironment()
    {
        CreateMaterials();
        BuildSceneGeometry();
        SetupLightingAndPostProcessing();
        
        Debug.Log("Phase 2 Environment Built Successfully!");
    }

    private static void CreateMaterials()
    {
        if (!Directory.Exists("Assets/Materials"))
        {
            Directory.CreateDirectory("Assets/Materials");
        }

        CreateURPMaterial("Assets/Materials/Mat_DarkCharcoal.mat", new Color(0.1f, 0.12f, 0.15f), false, Color.black);
        CreateURPMaterial("Assets/Materials/Mat_Floor.mat", new Color(0.05f, 0.05f, 0.07f), false, Color.black, 0.2f);
        CreateURPMaterial("Assets/Materials/Mat_DeskSurface.mat", new Color(0.08f, 0.09f, 0.11f), false, Color.black, 0.8f);
        
        // Emissive Materials
        CreateURPMaterial("Assets/Materials/Mat_EmissiveCyan.mat", Color.black, true, new Color(0f, 1f, 1f) * 4f);
        CreateURPMaterial("Assets/Materials/Mat_EmissiveViolet.mat", Color.black, true, new Color(0.5f, 0f, 1f) * 4f);
        CreateURPMaterial("Assets/Materials/Mat_EmissiveMagenta.mat", Color.black, true, new Color(1f, 0f, 0.5f) * 4f);
        
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

    private static void BuildSceneGeometry()
    {
        Material matCharcoal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DarkCharcoal.mat");
        Material matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Floor.mat");
        Material matDesk = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DeskSurface.mat");
        Material matCyan = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveCyan.mat");
        Material matViolet = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_EmissiveViolet.mat");

        // Create Environment Parent
        GameObject envParent = GameObject.Find("Environment");
        if (envParent == null) envParent = new GameObject("Environment");

        // Create Floor
        if (GameObject.Find("Floor") == null)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(envParent.transform);
            floor.transform.position = new Vector3(0, -0.05f, 0);
            floor.transform.localScale = new Vector3(10f, 0.1f, 10f);
            floor.GetComponent<Renderer>().sharedMaterial = matFloor;
        }

        // Create Walls
        if (GameObject.Find("Walls") == null)
        {
            GameObject wallsParent = new GameObject("Walls");
            wallsParent.transform.SetParent(envParent.transform);

            // Front Wall
            GameObject frontWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontWall.name = "Wall_Front";
            frontWall.transform.SetParent(wallsParent.transform);
            frontWall.transform.position = new Vector3(0, 2f, 5f);
            frontWall.transform.localScale = new Vector3(10f, 4f, 0.2f);
            frontWall.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Back Wall
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "Wall_Back";
            backWall.transform.SetParent(wallsParent.transform);
            backWall.transform.position = new Vector3(0, 2f, -5f);
            backWall.transform.localScale = new Vector3(10f, 4f, 0.2f);
            backWall.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Left Wall
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "Wall_Left";
            leftWall.transform.SetParent(wallsParent.transform);
            leftWall.transform.position = new Vector3(-5f, 2f, 0f);
            leftWall.transform.localScale = new Vector3(0.2f, 4f, 10f);
            leftWall.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Right Wall
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "Wall_Right";
            rightWall.transform.SetParent(wallsParent.transform);
            rightWall.transform.position = new Vector3(5f, 2f, 0f);
            rightWall.transform.localScale = new Vector3(0.2f, 4f, 10f);
            rightWall.GetComponent<Renderer>().sharedMaterial = matCharcoal;
        }

        // Create Workstation Parent
        GameObject workParent = GameObject.Find("Workstation");
        if (workParent == null) workParent = new GameObject("Workstation");
        
        // Desk
        if (GameObject.Find("ComputerDesk") == null)
        {
            GameObject deskParent = new GameObject("ComputerDesk");
            deskParent.transform.SetParent(workParent.transform);
            deskParent.transform.position = new Vector3(0, 0, 0.6f);

            // Top
            GameObject deskTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskTop.name = "Desk_Top";
            deskTop.transform.SetParent(deskParent.transform);
            deskTop.transform.localPosition = new Vector3(0, 0.75f, 0);
            deskTop.transform.localScale = new Vector3(1.6f, 0.04f, 0.8f);
            deskTop.GetComponent<Renderer>().sharedMaterial = matDesk;

            // Left Leg
            GameObject legLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legLeft.name = "Desk_Leg_L";
            legLeft.transform.SetParent(deskParent.transform);
            legLeft.transform.localPosition = new Vector3(-0.75f, 0.375f, 0);
            legLeft.transform.localScale = new Vector3(0.05f, 0.75f, 0.7f);
            legLeft.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Right Leg
            GameObject legRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legRight.name = "Desk_Leg_R";
            legRight.transform.SetParent(deskParent.transform);
            legRight.transform.localPosition = new Vector3(0.75f, 0.375f, 0);
            legRight.transform.localScale = new Vector3(0.05f, 0.75f, 0.7f);
            legRight.GetComponent<Renderer>().sharedMaterial = matCharcoal;

            // Emissive Strip Cyan
            GameObject stripCyan = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripCyan.name = "Desk_Accent_Cyan";
            stripCyan.transform.SetParent(deskParent.transform);
            stripCyan.transform.localPosition = new Vector3(0, 0.73f, 0.38f);
            stripCyan.transform.localScale = new Vector3(1.5f, 0.01f, 0.05f);
            stripCyan.GetComponent<Renderer>().sharedMaterial = matCyan;
            
            // Emissive Strip Violet
            GameObject stripViolet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripViolet.name = "Desk_Accent_Violet";
            stripViolet.transform.SetParent(deskParent.transform);
            stripViolet.transform.localPosition = new Vector3(0, 0.73f, -0.38f);
            stripViolet.transform.localScale = new Vector3(1.5f, 0.01f, 0.05f);
            stripViolet.GetComponent<Renderer>().sharedMaterial = matViolet;
        }
    }

    private static void SetupLightingAndPostProcessing()
    {
        // Tone down directional light
        GameObject dirLight = GameObject.Find("Directional Light");
        if (dirLight != null)
        {
            Light l = dirLight.GetComponent<Light>();
            if (l != null)
            {
                l.intensity = 0.1f;
                l.color = new Color(0.2f, 0.25f, 0.35f);
            }
        }

        // Add soft ambient point lights if none exist
        if (GameObject.Find("Workstation Light") == null)
        {
            GameObject deskLightObj = new GameObject("Workstation Light");
            deskLightObj.transform.position = new Vector3(0, 1.8f, 0.5f);
            Light deskLight = deskLightObj.AddComponent<Light>();
            deskLight.type = LightType.Point;
            deskLight.color = new Color(0.8f, 0.9f, 1f);
            deskLight.intensity = 2f;
            deskLight.range = 5f;
            
            GameObject workParent = GameObject.Find("Workstation");
            if (workParent != null) deskLightObj.transform.SetParent(workParent.transform);
        }

        // Configure Global Volume for Bloom
        Volume volume = Object.FindAnyObjectByType<Volume>();
        if (volume != null && volume.profile != null)
        {
            if (!volume.profile.TryGet(out Bloom bloom))
            {
                bloom = volume.profile.Add<Bloom>();
            }
            bloom.active = true;
            bloom.intensity.Override(1.5f);
            bloom.threshold.Override(1f);
            
            if (!volume.profile.TryGet(out Tonemapping tonemap))
            {
                tonemap = volume.profile.Add<Tonemapping>();
            }
            tonemap.active = true;
            tonemap.mode.Override(TonemappingMode.ACES);
        }
    }
}
